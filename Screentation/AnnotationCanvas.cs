using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.System;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Core;

namespace Screentation;

public class AnnotationCanvas : Grid
{
    private readonly CanvasControl _canvas;
    private CanvasBitmap? _cachedBitmap;
    private ScreenshotSession? _currentSession;

    // Viewport scaling and offsets
    private float _scale = 1.0f;
    private float _offsetX = 0.0f;
    private float _offsetY = 0.0f;
    private bool _userZoomedOrPanned = false;
    private bool _isPanning = false;
    private Point _panStartScreen;
    private Vector2 _panStartOffset;

    // Interactive Text tool state
    private TextBox? _activeTextEditor;
    private TextElement? _editingTextElement;

    // Interactive Crop tool state
    private Rect _cropRectOriginal;

    // Interactive Slice Cut tool state
    private Rect _sliceCutRectOriginal;
    public bool IsSliceCutHorizontal { get; set; } = true;

    // Interaction states
    private bool _isDrawing = false;
    private bool _isMoving = false;
    private bool _isResizing = false;
    private bool _draggingSourceOnly = false;
    private Vector2 _lastOriginalPoint;
    private Vector2 _startOriginalPoint;
    private int _activeHandleIndex = -1;

    public event EventHandler? ZoomChanged;

    public float ZoomPercent
    {
        get
        {
            if (_cachedBitmap == null || ActualWidth <= 0 || ActualHeight <= 0) return 100.0f;
            float imgWidth = (float)_cachedBitmap.Size.Width;
            float imgHeight = (float)_cachedBitmap.Size.Height;
            float scaleX = (float)ActualWidth / imgWidth;
            float scaleY = (float)ActualHeight / imgHeight;
            float fitScale = Math.Min(scaleX, scaleY);
            if (fitScale > 1.0f) fitScale = 1.0f;
            if (fitScale <= 0.0f) return 100.0f;
            return (_scale / fitScale) * 100.0f;
        }
        set
        {
            if (_cachedBitmap == null || ActualWidth <= 0 || ActualHeight <= 0) return;
            float imgWidth = (float)_cachedBitmap.Size.Width;
            float imgHeight = (float)_cachedBitmap.Size.Height;
            float scaleX = (float)ActualWidth / imgWidth;
            float scaleY = (float)ActualHeight / imgHeight;
            float fitScale = Math.Min(scaleX, scaleY);
            if (fitScale > 1.0f) fitScale = 1.0f;
            if (fitScale <= 0.0f) return;

            float newScale = (value / 100.0f) * fitScale;
            if (newScale < 0.05f) newScale = 0.05f;
            if (newScale > 10.0f) newScale = 10.0f;

            Point centerPt = new Point(ActualWidth / 2, ActualHeight / 2);
            Vector2 origPt = ScreenToOriginal(centerPt);

            _scale = newScale;
            _offsetX = (float)centerPt.X - origPt.X * _scale;
            _offsetY = (float)centerPt.Y - origPt.Y * _scale;

            _userZoomedOrPanned = true;
            _canvas.Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public static readonly DependencyProperty ActiveFontSizeProperty =
        DependencyProperty.Register(nameof(ActiveFontSize), typeof(float), typeof(AnnotationCanvas),
            new PropertyMetadata(36.0f, OnActiveFontSizeChanged));

    public float ActiveFontSize
    {
        get => (float)GetValue(ActiveFontSizeProperty);
        set => SetValue(ActiveFontSizeProperty, value);
    }

    public static readonly DependencyProperty StepSequencingFormatProperty =
        DependencyProperty.Register(nameof(StepSequencingFormat), typeof(string), typeof(AnnotationCanvas),
            new PropertyMetadata("Numeric"));

    public string StepSequencingFormat
    {
        get => (string)GetValue(StepSequencingFormatProperty);
        set => SetValue(StepSequencingFormatProperty, value);
    }

    public event EventHandler<int>? NextStepValueChanged;

    public static readonly DependencyProperty NextStepValueProperty =
        DependencyProperty.Register(nameof(NextStepValue), typeof(int), typeof(AnnotationCanvas),
            new PropertyMetadata(1, OnNextStepValueChanged));

    public int NextStepValue
    {
        get => (int)GetValue(NextStepValueProperty);
        set => SetValue(NextStepValueProperty, value);
    }

    private static void OnNextStepValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (AnnotationCanvas)d;
        int val = (int)e.NewValue;
        if (canvas.Session != null)
        {
            canvas.Session.StepCounter = val;
        }
        canvas.NextStepValueChanged?.Invoke(canvas, val);
    }

    private static void OnActiveFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (AnnotationCanvas)d;
        float val = (float)e.NewValue;
        if (canvas.SelectedElement != null)
        {
            if (canvas.SelectedElement is TextElement textEl)
            {
                textEl.FontSize = val;
                canvas._canvas.Invalidate();
            }
        }
    }

    private static void OnActiveColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (AnnotationCanvas)d;
        Color val = (Color)e.NewValue;
        if (canvas.SelectedElement != null)
        {
            canvas.SelectedElement.Color = val;
            canvas._canvas.Invalidate();
        }
    }

    private static void OnActiveThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (AnnotationCanvas)d;
        float val = (float)e.NewValue;
        if (canvas.SelectedElement != null)
        {
            canvas.SelectedElement.StrokeThickness = val;
            if (canvas.SelectedElement is StepElement stepEl)
            {
                stepEl.Radius = 15.0f + val * 5.0f;
            }
            canvas._canvas.Invalidate();
        }
    }

    // Dependency Properties
    public static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(nameof(Session), typeof(ScreenshotSession), typeof(AnnotationCanvas),
            new PropertyMetadata(null, OnSessionChanged));

    public ScreenshotSession? Session
    {
        get => (ScreenshotSession?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public static readonly DependencyProperty ActiveToolProperty =
        DependencyProperty.Register(nameof(ActiveTool), typeof(AnnotationType), typeof(AnnotationCanvas),
            new PropertyMetadata(AnnotationType.Select, OnActiveToolChanged));

    public AnnotationType ActiveTool
    {
        get => (AnnotationType)GetValue(ActiveToolProperty);
        set => SetValue(ActiveToolProperty, value);
    }

    public static readonly DependencyProperty ActiveColorProperty =
        DependencyProperty.Register(nameof(ActiveColor), typeof(Color), typeof(AnnotationCanvas),
            new PropertyMetadata(Microsoft.UI.Colors.Red, OnActiveColorChanged));

    public Color ActiveColor
    {
        get => (Color)GetValue(ActiveColorProperty);
        set => SetValue(ActiveColorProperty, value);
    }

    public static readonly DependencyProperty ActiveThicknessProperty =
        DependencyProperty.Register(nameof(ActiveThickness), typeof(float), typeof(AnnotationCanvas),
            new PropertyMetadata(3.0f, OnActiveThicknessChanged));

    public float ActiveThickness
    {
        get => (float)GetValue(ActiveThicknessProperty);
        set => SetValue(ActiveThicknessProperty, value);
    }

    public static readonly DependencyProperty HasFillProperty =
        DependencyProperty.Register(nameof(HasFill), typeof(bool), typeof(AnnotationCanvas),
            new PropertyMetadata(true));

    public bool HasFill
    {
        get => (bool)GetValue(HasFillProperty);
        set => SetValue(HasFillProperty, value);
    }

    public AnnotationElement? SelectedElement { get; private set; }

    public event EventHandler? SelectionChanged;

    public AnnotationCanvas()
    {
        _canvas = new CanvasControl();
        _canvas.CreateResources += OnCanvasCreateResources;
        _canvas.Draw += OnCanvasDraw;

        this.Children.Add(_canvas);

        // Hook pointer events
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;

        this.SizeChanged += (s, e) => {
            _canvas.Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    private static void OnSessionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (AnnotationCanvas)d;
        if (canvas._currentSession != null)
        {
            canvas._currentSession.Annotations.CollectionChanged -= canvas.OnAnnotationsCollectionChanged;
        }

        canvas._currentSession = (ScreenshotSession?)e.NewValue;
        canvas._cachedBitmap?.Dispose();
        canvas._cachedBitmap = null;
        canvas.SelectedElement = null;
        canvas._userZoomedOrPanned = false;
        canvas._isPanning = false;
        canvas.ZoomChanged?.Invoke(canvas, EventArgs.Empty);

        if (canvas._currentSession != null)
        {
            canvas._currentSession.Annotations.CollectionChanged += canvas.OnAnnotationsCollectionChanged;
            canvas.NextStepValue = canvas._currentSession.StepCounter;
        }
        else
        {
            canvas.NextStepValue = 1;
        }

        canvas.SelectionChanged?.Invoke(canvas, EventArgs.Empty);
        canvas._canvas.Invalidate();
    }

    private static void OnActiveToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (AnnotationCanvas)d;
        var newTool = (AnnotationType)e.NewValue;

        // Commit active text editor when switching tools
        canvas.CommitActiveTextEditor();

        // If switching tools, clear selection to avoid weird UI state
        if (canvas.SelectedElement != null)
        {
            canvas.SelectedElement.IsSelected = false;
            canvas.SelectedElement = null;
            canvas.SelectionChanged?.Invoke(canvas, EventArgs.Empty);
        }

        if (newTool == AnnotationType.Crop && canvas._cachedBitmap != null)
        {
            canvas._cropRectOriginal = new Rect(0, 0, canvas._cachedBitmap.Size.Width, canvas._cachedBitmap.Size.Height);
        }
        else if (newTool == AnnotationType.SliceCut)
        {
            canvas._sliceCutRectOriginal = new Rect(0, 0, 0, 0);
        }

        canvas._canvas.Invalidate();
    }

    private void OnAnnotationsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _canvas.Invalidate();
    }

    private void OnCanvasCreateResources(CanvasControl sender, object args)
    {
        // Handled dynamically during Draw, or we can load here
    }

    private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (Session == null || Session.OriginalBitmap == null)
        {
            args.DrawingSession.Clear(Microsoft.UI.Colors.Transparent);
            return;
        }

        // Cache the CanvasBitmap
        if (_cachedBitmap == null)
        {
            _cachedBitmap = CanvasBitmap.CreateFromSoftwareBitmap(sender, Session.OriginalBitmap);
        }

        var ds = args.DrawingSession;
        ds.Clear(Microsoft.UI.Colors.Transparent);

        float imgWidth = (float)_cachedBitmap.Size.Width;
        float imgHeight = (float)_cachedBitmap.Size.Height;

        // Initialize crop rect if needed
        if (ActiveTool == AnnotationType.Crop && (_cropRectOriginal.Width <= 0 || _cropRectOriginal.Height <= 0))
        {
            _cropRectOriginal = new Rect(0, 0, imgWidth, imgHeight);
        }

        // Calculate Zoom-To-Fit scale and offset ONLY if the user hasn't zoomed or panned
        if (!_userZoomedOrPanned)
        {
            float scaleX = (float)ActualWidth / imgWidth;
            float scaleY = (float)ActualHeight / imgHeight;
            _scale = Math.Min(scaleX, scaleY);
            
            // Prevent upscaling past 100%
            if (_scale > 1.0f) _scale = 1.0f;

            float drawWidth = imgWidth * _scale;
            float drawHeight = imgHeight * _scale;
            _offsetX = ((float)ActualWidth - drawWidth) / 2.0f;
            _offsetY = ((float)ActualHeight - drawHeight) / 2.0f;
        }

        // Draw background image
        Rect destRect = new Rect(_offsetX, _offsetY, imgWidth * _scale, imgHeight * _scale);
        ds.DrawImage(_cachedBitmap, destRect);

        // Save state to render annotations in original coordinate system
        var oldTransform = ds.Transform;
        ds.Transform = Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_offsetX, _offsetY);

        if (ActiveTool == AnnotationType.Crop)
        {
            // Draw dark mask
            ds.FillRectangle(new Rect(0, 0, imgWidth, imgHeight), Color.FromArgb(120, 0, 0, 0));
            // Redraw crop area cleanly
            ds.DrawImage(_cachedBitmap, _cropRectOriginal, _cropRectOriginal);
        }

        // Draw elements in original coordinates
        foreach (var element in Session.Annotations)
        {
            AnnotationDrawer.DrawElement(sender, ds, element, _cachedBitmap);
        }

        if (ActiveTool == AnnotationType.SliceCut && (_sliceCutRectOriginal.Width > 0 || _sliceCutRectOriginal.Height > 0))
        {
            Rect band;
            if (IsSliceCutHorizontal)
            {
                band = new Rect(0, _sliceCutRectOriginal.Y, imgWidth, _sliceCutRectOriginal.Height);
                ds.FillRectangle(band, Color.FromArgb(120, 255, 0, 0));
                ds.DrawLine(new Vector2(0, (float)band.Top), new Vector2(imgWidth, (float)band.Top), Microsoft.UI.Colors.Red, 2.0f);
                ds.DrawLine(new Vector2(0, (float)band.Bottom), new Vector2(imgWidth, (float)band.Bottom), Microsoft.UI.Colors.Red, 2.0f);
            }
            else
            {
                band = new Rect(_sliceCutRectOriginal.X, 0, _sliceCutRectOriginal.Width, imgHeight);
                ds.FillRectangle(band, Color.FromArgb(120, 255, 0, 0));
                ds.DrawLine(new Vector2((float)band.Left, 0), new Vector2((float)band.Left, imgHeight), Microsoft.UI.Colors.Red, 2.0f);
                ds.DrawLine(new Vector2((float)band.Right, 0), new Vector2((float)band.Right, imgHeight), Microsoft.UI.Colors.Red, 2.0f);
            }
        }

        // Restore screen coordinates to draw selection outlines and handles
        ds.Transform = oldTransform;

        if (ActiveTool == AnnotationType.Crop)
        {
            Rect screenCrop = TransformRectToScreen(_cropRectOriginal);
            ds.DrawRectangle(screenCrop, Microsoft.UI.Colors.White, 2.0f);
            DrawCropCornerTicks(ds, screenCrop);
        }

        if (SelectedElement != null && (ActiveTool == AnnotationType.Select || SelectedElement.Type == ActiveTool))
        {
            DrawSelectionOverlay(sender, ds, SelectedElement);
        }
    }

    private void DrawSelectionOverlay(CanvasControl sender, CanvasDrawingSession ds, AnnotationElement element)
    {
        var strokeStyle = new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Dash };

        if (element is EraserElement eraserEl)
        {
            // Draw Target bounds on screen
            Rect targetScreen = TransformRectToScreen(eraserEl.Bounds);
            ds.DrawRectangle(targetScreen, Microsoft.UI.Colors.Magenta, 1.5f, strokeStyle);

            // Draw Source bounds on screen
            Rect sourceScreen = TransformRectToScreen(eraserEl.SourceBounds);
            ds.DrawRectangle(sourceScreen, Microsoft.UI.Colors.Cyan, 1.5f, strokeStyle);

            // Draw connecting dashed line
            Vector2 tCenter = new Vector2((float)(targetScreen.X + targetScreen.Width / 2), (float)(targetScreen.Y + targetScreen.Height / 2));
            Vector2 sCenter = new Vector2((float)(sourceScreen.X + sourceScreen.Width / 2), (float)(sourceScreen.Y + sourceScreen.Height / 2));
            ds.DrawLine(tCenter, sCenter, Microsoft.UI.Colors.Magenta, 1.5f, strokeStyle);

            // Draw handles for target (0-7) and source (8-15 if wanted, or just simple resize handles on target)
            DrawRectHandles(ds, targetScreen);
            
            // Draw a single move handle inside source center or outline
            ds.DrawCircle(sCenter, 6, Microsoft.UI.Colors.Cyan, 2.0f);
            ds.FillCircle(sCenter, 3, Microsoft.UI.Colors.Cyan);
        }
        else if (element is RectElement || element is BlurElement || element is TextElement)
        {
            Rect screenRect = TransformRectToScreen(element.GetBounds());
            ds.DrawRectangle(screenRect, Microsoft.UI.Colors.DodgerBlue, 1.5f);
            DrawRectHandles(ds, screenRect);
        }
        else if (element is ArrowElement arrowEl)
        {
            Point startScreen = TransformPointToScreen(arrowEl.Start);
            Point endScreen = TransformPointToScreen(arrowEl.End);

            ds.DrawLine(new Vector2((float)startScreen.X, (float)startScreen.Y),
                        new Vector2((float)endScreen.X, (float)endScreen.Y), Microsoft.UI.Colors.DodgerBlue, 1.0f, strokeStyle);

            DrawHandle(ds, startScreen);
            DrawHandle(ds, endScreen);
        }
        else if (element is StepElement stepEl)
        {
            Point centerScreen = TransformPointToScreen(stepEl.Center);
            float screenRadius = stepEl.Radius * _scale;
            ds.DrawCircle(new Vector2((float)centerScreen.X, (float)centerScreen.Y), screenRadius + 4, Microsoft.UI.Colors.DodgerBlue, 1.5f);
        }
    }

    private void DrawRectHandles(CanvasDrawingSession ds, Rect screenRect)
    {
        // 0: TopLeft, 1: TopRight, 2: BottomRight, 3: BottomLeft
        // 4: Top, 5: Right, 6: Bottom, 7: Left
        DrawHandle(ds, new Point(screenRect.Left, screenRect.Top));
        DrawHandle(ds, new Point(screenRect.Right, screenRect.Top));
        DrawHandle(ds, new Point(screenRect.Right, screenRect.Bottom));
        DrawHandle(ds, new Point(screenRect.Left, screenRect.Bottom));

        DrawHandle(ds, new Point(screenRect.Left + screenRect.Width / 2, screenRect.Top));
        DrawHandle(ds, new Point(screenRect.Right, screenRect.Top + screenRect.Height / 2));
        DrawHandle(ds, new Point(screenRect.Left + screenRect.Width / 2, screenRect.Bottom));
        DrawHandle(ds, new Point(screenRect.Left, screenRect.Top + screenRect.Height / 2));
    }

    private void DrawHandle(CanvasDrawingSession ds, Point pt)
    {
        ds.FillRectangle(new Rect(pt.X - 4, pt.Y - 4, 8, 8), Microsoft.UI.Colors.White);
        ds.DrawRectangle(new Rect(pt.X - 4, pt.Y - 4, 8, 8), Microsoft.UI.Colors.DodgerBlue, 1.0f);
    }

    private Vector2 ScreenToOriginal(Point screenPt)
    {
        return new Vector2(
            ((float)screenPt.X - _offsetX) / _scale,
            ((float)screenPt.Y - _offsetY) / _scale
        );
    }

    private Point TransformPointToScreen(Vector2 p)
    {
        return new Point(p.X * _scale + _offsetX, p.Y * _scale + _offsetY);
    }

    private Rect TransformRectToScreen(Rect r)
    {
        return new Rect(
            r.X * _scale + _offsetX,
            r.Y * _scale + _offsetY,
            r.Width * _scale,
            r.Height * _scale
        );
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Session == null) return;

        var pointerPoint = e.GetCurrentPoint(_canvas);
        Point screenPt = pointerPoint.Position;
        
        // 0. Handle middle mouse button panning
        if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStartScreen = screenPt;
            _panStartOffset = new Vector2(_offsetX, _offsetY);
            _canvas.CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        Vector2 origPt = ScreenToOriginal(screenPt);
        _lastOriginalPoint = origPt;
        _startOriginalPoint = origPt;

        // 1. Handle Crop Tool interaction
        if (ActiveTool == AnnotationType.Crop)
        {
            _activeHandleIndex = HitTestCropHandles(screenPt);
            if (_activeHandleIndex != -1)
            {
                _isResizing = true;
                _canvas.CapturePointer(e.Pointer);
                e.Handled = true;
                return;
            }
            
            if (HitTestRect(_cropRectOriginal, origPt))
            {
                _isMoving = true;
                _canvas.CapturePointer(e.Pointer);
                e.Handled = true;
                return;
            }
            
            e.Handled = true;
            return;
        }

        // 1.5. Handle Slice Cut Tool interaction
        if (ActiveTool == AnnotationType.SliceCut)
        {
            _isDrawing = true;
            _sliceCutRectOriginal = new Rect(origPt.X, origPt.Y, 0, 0);
            _canvas.CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        // 2. Handle Text Tool interaction
        if (ActiveTool == AnnotationType.Text)
        {
            TextElement? hitText = null;
            for (int i = Session.Annotations.Count - 1; i >= 0; i--)
            {
                var element = Session.Annotations[i];
                if (element is TextElement tEl && tEl.HitTest(origPt, 8.0f / _scale))
                {
                    hitText = tEl;
                    break;
                }
            }

            if (hitText != null)
            {
                if (SelectedElement != hitText)
                {
                    if (SelectedElement != null) SelectedElement.IsSelected = false;
                    SelectedElement = hitText;
                    SelectedElement.IsSelected = true;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
                ShowTextEditor(hitText);
                _canvas.Invalidate();
            }
            else
            {
                var newText = new TextElement
                {
                    Position = origPt,
                    Color = ActiveColor,
                    FontSize = ActiveFontSize
                };
                
                if (SelectedElement != null) SelectedElement.IsSelected = false;
                Session.History.SaveState();
                Session.Annotations.Add(newText);
                SelectedElement = newText;
                SelectedElement.IsSelected = true;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                _canvas.Invalidate();
                
                ShowTextEditor(newText);
            }
            e.Handled = true;
            return;
        }

        // 3. Handle standard interaction with SelectedElement
        bool canInteractWithSelected = SelectedElement != null && 
            (ActiveTool == AnnotationType.Select || ActiveTool == SelectedElement.Type);

        if (canInteractWithSelected && SelectedElement != null)
        {
            _activeHandleIndex = HitTestHandles(SelectedElement, screenPt);
            if (_activeHandleIndex != -1)
            {
                Session.History.SaveState();
                _isResizing = true;
                e.Handled = true;
                _canvas.CapturePointer(e.Pointer);
                return;
            }

            float tolerance = 8.0f / _scale;
            bool hitSelf = false;
            
            if (SelectedElement is EraserElement eraser)
            {
                if (HitTestRect(eraser.SourceBounds, origPt))
                {
                    _draggingSourceOnly = true;
                    hitSelf = true;
                }
                else if (HitTestRect(eraser.Bounds, origPt))
                {
                    _draggingSourceOnly = false;
                    hitSelf = true;
                }
            }
            else
            {
                hitSelf = SelectedElement.HitTest(origPt, tolerance);
            }

            if (hitSelf)
            {
                Session.History.SaveState();
                _isMoving = true;
                e.Handled = true;
                _canvas.CapturePointer(e.Pointer);
                return;
            }
        }

        if (ActiveTool == AnnotationType.Select)
        {
            AnnotationElement? hit = null;
            for (int i = Session.Annotations.Count - 1; i >= 0; i--)
            {
                var element = Session.Annotations[i];
                float tolerance = 8.0f / _scale;
                if (element.HitTest(origPt, tolerance))
                {
                    hit = element;
                    break;
                }
            }

            if (hit != null)
            {
                if (SelectedElement != hit)
                {
                    if (SelectedElement != null) SelectedElement.IsSelected = false;
                    SelectedElement = hit;
                    SelectedElement.IsSelected = true;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }

                if (SelectedElement is EraserElement eraser && HitTestRect(eraser.SourceBounds, origPt))
                {
                    _draggingSourceOnly = true;
                }
                else
                {
                    _draggingSourceOnly = false;
                }

                Session.History.SaveState();
                _isMoving = true;
                _canvas.CapturePointer(e.Pointer);
                _canvas.Invalidate();
            }
            else
            {
                if (SelectedElement != null)
                {
                    SelectedElement.IsSelected = false;
                    SelectedElement = null;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    _canvas.Invalidate();
                }
            }
        }
        else
        {
            _isDrawing = true;
            _canvas.CapturePointer(e.Pointer);

            AnnotationElement? newEl = null;
            switch (ActiveTool)
            {
                case AnnotationType.Rect:
                    newEl = new RectElement
                    {
                        Bounds = new Rect(origPt.X, origPt.Y, 0, 0),
                        Color = ActiveColor,
                        StrokeThickness = ActiveThickness,
                        HasFill = HasFill
                    };
                    break;
                case AnnotationType.Arrow:
                    newEl = new ArrowElement
                    {
                        Start = origPt,
                        End = origPt,
                        Color = ActiveColor,
                        StrokeThickness = ActiveThickness
                    };
                    break;
                case AnnotationType.Blur:
                    newEl = new BlurElement
                    {
                        Bounds = new Rect(origPt.X, origPt.Y, 0, 0),
                        BlurRadius = 15.0f
                    };
                    break;
                case AnnotationType.Eraser:
                    newEl = new EraserElement
                    {
                        Bounds = new Rect(origPt.X, origPt.Y, 0, 0),
                        SourceOffset = new Vector2(50.0f, 50.0f)
                    };
                    break;
                case AnnotationType.Step:
                    int val = NextStepValue;
                    string label = GetStepLabel(val);
                    newEl = new StepElement
                    {
                        Center = origPt,
                        Color = ActiveColor,
                        Number = val,
                        Label = label,
                        Radius = 15.0f + ActiveThickness * 5.0f
                    };
                    NextStepValue = val + 1;
                    _isDrawing = false;
                    break;
                case AnnotationType.Highlighter:
                    newEl = new HighlighterElement
                    {
                        Bounds = new Rect(origPt.X, origPt.Y, 0, 0),
                        Color = ActiveColor,
                        Opacity = 0.4f
                    };
                    break;
            }

            if (newEl != null)
            {
                if (SelectedElement != null) SelectedElement.IsSelected = false;
                Session.History.SaveState();
                Session.Annotations.Add(newEl);
                SelectedElement = newEl;
                SelectedElement.IsSelected = true;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                _canvas.Invalidate();
            }
        }

        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (Session == null) return;

        var pointerPoint = e.GetCurrentPoint(_canvas);
        Point screenPt = pointerPoint.Position;
        
        // Panning viewport handling
        if (_isPanning)
        {
            Vector2 screenDelta = new Vector2((float)(screenPt.X - _panStartScreen.X), (float)(screenPt.Y - _panStartScreen.Y));
            _offsetX = _panStartOffset.X + screenDelta.X;
            _offsetY = _panStartOffset.Y + screenDelta.Y;
            _userZoomedOrPanned = true;
            _canvas.Invalidate();
            return;
        }

        Vector2 origPt = ScreenToOriginal(screenPt);
        Vector2 delta = origPt - _lastOriginalPoint;
        _lastOriginalPoint = origPt;

        // Cropping box resizing & moving handling
        if (ActiveTool == AnnotationType.Crop)
        {
            if (_isResizing)
            {
                _cropRectOriginal = ResizeCropRect(_cropRectOriginal, delta, _activeHandleIndex);
                _canvas.Invalidate();
            }
            else if (_isMoving && _cachedBitmap != null)
            {
                double x = _cropRectOriginal.X + delta.X;
                double y = _cropRectOriginal.Y + delta.Y;
                float imgWidth = (float)_cachedBitmap.Size.Width;
                float imgHeight = (float)_cachedBitmap.Size.Height;
                
                x = Math.Max(0, Math.Min(x, imgWidth - _cropRectOriginal.Width));
                y = Math.Max(0, Math.Min(y, imgHeight - _cropRectOriginal.Height));
                
                _cropRectOriginal = new Rect(x, y, _cropRectOriginal.Width, _cropRectOriginal.Height);
                _canvas.Invalidate();
            }
            return;
        }

        if (ActiveTool == AnnotationType.SliceCut)
        {
            if (_isDrawing)
            {
                _sliceCutRectOriginal = CalculateNewBounds(_startOriginalPoint, origPt);
                _canvas.Invalidate();
            }
            return;
        }

        if (_isDrawing && SelectedElement != null)
        {
            switch (SelectedElement)
            {
                case RectElement rectEl:
                    rectEl.Bounds = CalculateNewBounds(_startOriginalPoint, origPt);
                    break;
                case ArrowElement arrowEl:
                    arrowEl.End = origPt;
                    break;
                case BlurElement blurEl:
                    blurEl.Bounds = CalculateNewBounds(_startOriginalPoint, origPt);
                    break;
                case EraserElement eraserEl:
                    eraserEl.Bounds = CalculateNewBounds(_startOriginalPoint, origPt);
                    break;
                case HighlighterElement highEl:
                    highEl.Bounds = CalculateNewBounds(_startOriginalPoint, origPt);
                    break;
            }
            _canvas.Invalidate();
        }
        else if (_isResizing && SelectedElement != null)
        {
            SelectedElement.Resize(delta, _activeHandleIndex);
            _canvas.Invalidate();
        }
        else if (_isMoving && SelectedElement != null)
        {
            if (_draggingSourceOnly && SelectedElement is EraserElement eraser)
            {
                eraser.MoveSourceOnly(delta);
            }
            else
            {
                SelectedElement.Move(delta);
            }
            _canvas.Invalidate();
        }
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            _canvas.ReleasePointerCapture(e.Pointer);
            _canvas.Invalidate();
            return;
        }

        if (ActiveTool == AnnotationType.Crop)
        {
            _isDrawing = false;
            _isMoving = false;
            _isResizing = false;
            _canvas.ReleasePointerCapture(e.Pointer);
            _canvas.Invalidate();
            return;
        }

        if (ActiveTool == AnnotationType.SliceCut)
        {
            _isDrawing = false;
            _canvas.ReleasePointerCapture(e.Pointer);
            _canvas.Invalidate();
            return;
        }

        if (_isDrawing || _isMoving || _isResizing)
        {
            Session?.History.RemoveLastStateIfIdentical();
        }

        _isDrawing = false;
        _isMoving = false;
        _isResizing = false;
        _draggingSourceOnly = false;
        _activeHandleIndex = -1;

        _canvas.ReleasePointerCapture(e.Pointer);
        _canvas.Invalidate();
    }

    private Rect CalculateNewBounds(Vector2 startPt, Vector2 currentPt)
    {
        // Simple drag rectangle logic (anchored at first pressed point)
        // Set bounds start point as the anchor
        double left = Math.Min(startPt.X, currentPt.X);
        double top = Math.Min(startPt.Y, currentPt.Y);
        double w = Math.Abs(currentPt.X - startPt.X);
        double h = Math.Abs(currentPt.Y - startPt.Y);
        return new Rect(left, top, w, h);
    }

    private bool HitTestRect(Rect r, Vector2 pt)
    {
        return pt.X >= r.Left && pt.X <= r.Right && pt.Y >= r.Top && pt.Y <= r.Bottom;
    }

    private int HitTestHandles(AnnotationElement element, Point screenPt)
    {
        if (element is ArrowElement arrow)
        {
            Point s = TransformPointToScreen(arrow.Start);
            Point ed = TransformPointToScreen(arrow.End);

            if (PointDistance(screenPt, s) <= 8.0) return 0;
            if (PointDistance(screenPt, ed) <= 8.0) return 1;
            return -1;
        }

        if (element is RectElement || element is BlurElement || element is EraserElement || element is TextElement)
        {
            Rect screenRect = TransformRectToScreen(element.GetBounds());
            
            // Corners
            if (PointDistance(screenPt, new Point(screenRect.Left, screenRect.Top)) <= 8.0) return 0;
            if (PointDistance(screenPt, new Point(screenRect.Right, screenRect.Top)) <= 8.0) return 1;
            if (PointDistance(screenPt, new Point(screenRect.Right, screenRect.Bottom)) <= 8.0) return 2;
            if (PointDistance(screenPt, new Point(screenRect.Left, screenRect.Bottom)) <= 8.0) return 3;

            // Edges
            if (PointDistance(screenPt, new Point(screenRect.Left + screenRect.Width / 2, screenRect.Top)) <= 8.0) return 4;
            if (PointDistance(screenPt, new Point(screenRect.Right, screenRect.Top + screenRect.Height / 2)) <= 8.0) return 5;
            if (PointDistance(screenPt, new Point(screenRect.Left + screenRect.Width / 2, screenRect.Bottom)) <= 8.0) return 6;
            if (PointDistance(screenPt, new Point(screenRect.Left, screenRect.Top + screenRect.Height / 2)) <= 8.0) return 7;
        }

        return -1;
    }

    private double PointDistance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }

    public void ResetZoom()
    {
        _userZoomedOrPanned = false;
        _isPanning = false;
        _canvas.Invalidate();
        ZoomChanged?.Invoke(this, EventArgs.Empty);
    }

    public void InvalidateCanvas()
    {
        _canvas.Invalidate();
    }

    private void OnCanvasPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (Session == null) return;
        var pointerPoint = e.GetCurrentPoint(_canvas);
        
        var ctrl = e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control);
        
        if (ctrl)
        {
            int delta = pointerPoint.Properties.MouseWheelDelta;
            float zoomFactor = delta > 0 ? 1.1f : 0.9f;
            
            Point screenPt = pointerPoint.Position;
            Vector2 origPt = ScreenToOriginal(screenPt);
            
            float newScale = _scale * zoomFactor;
            if (newScale < 0.05f) newScale = 0.05f;
            if (newScale > 10.0f) newScale = 10.0f;
            
            _scale = newScale;
            _offsetX = (float)screenPt.X - origPt.X * _scale;
            _offsetY = (float)screenPt.Y - origPt.Y * _scale;
            
            _userZoomedOrPanned = true;
            _canvas.Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    // Text Editor Overlay methods
    private void ShowTextEditor(TextElement element)
    {
        CommitActiveTextEditor();

        _editingTextElement = element;
        _activeTextEditor = new TextBox
        {
            Text = element.Text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Background = new SolidColorBrush(this.ActualTheme == ElementTheme.Dark
                ? Color.FromArgb(230, 30, 30, 30)
                : Color.FromArgb(230, 255, 255, 255)),
            Foreground = new SolidColorBrush(element.Color),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
            FontSize = element.FontSize * _scale,
            FontFamily = new FontFamily("Segoe UI Variable Text"),
            Padding = new Thickness(4),
            Margin = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        Point screenPt = TransformPointToScreen(element.Position);
        _activeTextEditor.RenderTransform = new TranslateTransform
        {
            X = screenPt.X,
            Y = screenPt.Y
        };
        
        _activeTextEditor.Width = Math.Max(200, element.GetBounds().Width * _scale);
        _activeTextEditor.Height = Math.Max(40, element.GetBounds().Height * _scale);
        
        _activeTextEditor.LostFocus += (s, e) => CommitActiveTextEditor();
        _activeTextEditor.KeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CancelActiveTextEditor();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Enter && !Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
            {
                CommitActiveTextEditor();
                e.Handled = true;
            }
        };

        _activeTextEditor.Loaded += (s, e) =>
        {
            var tb = (TextBox)s;
            tb.Focus(FocusState.Programmatic);
            tb.Select(tb.Text.Length, 0); // Put cursor at the end
        };

        this.Children.Add(_activeTextEditor);
    }

    public void CommitActiveTextEditor()
    {
        if (_activeTextEditor == null || _editingTextElement == null) return;

        string text = _activeTextEditor.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            if (Session != null)
            {
                Session.Annotations.Remove(_editingTextElement);
                if (SelectedElement == _editingTextElement) SelectedElement = null;
            }
        }
        else
        {
            if (_editingTextElement.Text != text)
            {
                Session?.History.SaveState();
                _editingTextElement.Text = text;
            }
        }

        this.Children.Remove(_activeTextEditor);
        _activeTextEditor = null;
        _editingTextElement = null;
        _canvas.Invalidate();
    }

    public void CancelActiveTextEditor()
    {
        if (_activeTextEditor == null || _editingTextElement == null) return;

        if (string.IsNullOrEmpty(_editingTextElement.Text))
        {
            if (Session != null)
            {
                Session.Annotations.Remove(_editingTextElement);
                if (SelectedElement == _editingTextElement) SelectedElement = null;
            }
        }

        this.Children.Remove(_activeTextEditor);
        _activeTextEditor = null;
        _editingTextElement = null;
        _canvas.Invalidate();
    }

    // Crop helper methods
    private int HitTestCropHandles(Point screenPt)
    {
        Rect screenCropRect = TransformRectToScreen(_cropRectOriginal);
        
        if (PointDistance(screenPt, new Point(screenCropRect.Left, screenCropRect.Top)) <= 12.0) return 0;
        if (PointDistance(screenPt, new Point(screenCropRect.Right, screenCropRect.Top)) <= 12.0) return 1;
        if (PointDistance(screenPt, new Point(screenCropRect.Right, screenCropRect.Bottom)) <= 12.0) return 2;
        if (PointDistance(screenPt, new Point(screenCropRect.Left, screenCropRect.Bottom)) <= 12.0) return 3;

        if (PointDistance(screenPt, new Point(screenCropRect.Left + screenCropRect.Width / 2, screenCropRect.Top)) <= 12.0) return 4;
        if (PointDistance(screenPt, new Point(screenCropRect.Right, screenCropRect.Top + screenCropRect.Height / 2)) <= 12.0) return 5;
        if (PointDistance(screenPt, new Point(screenCropRect.Left + screenCropRect.Width / 2, screenCropRect.Bottom)) <= 12.0) return 6;
        if (PointDistance(screenPt, new Point(screenCropRect.Left, screenCropRect.Top + screenCropRect.Height / 2)) <= 12.0) return 7;

        return -1;
    }

    private Rect ResizeCropRect(Rect current, Vector2 delta, int handleIndex)
    {
        if (_cachedBitmap == null) return current;
        float imgWidth = (float)_cachedBitmap.Size.Width;
        float imgHeight = (float)_cachedBitmap.Size.Height;

        double left = current.Left;
        double right = current.Right;
        double top = current.Top;
        double bottom = current.Bottom;

        switch (handleIndex)
        {
            case 0: left += delta.X; top += delta.Y; break;
            case 1: right += delta.X; top += delta.Y; break;
            case 2: right += delta.X; bottom += delta.Y; break;
            case 3: left += delta.X; bottom += delta.Y; break;
            case 4: top += delta.Y; break;
            case 5: right += delta.X; break;
            case 6: bottom += delta.Y; break;
            case 7: left += delta.X; break;
        }

        double minSize = 50.0;
        if (right - left < minSize)
        {
            if (handleIndex == 0 || handleIndex == 3 || handleIndex == 7) left = right - minSize;
            else right = left + minSize;
        }
        if (bottom - top < minSize)
        {
            if (handleIndex == 0 || handleIndex == 1 || handleIndex == 4) top = bottom - minSize;
            else bottom = top + minSize;
        }

        left = Math.Max(0, Math.Min(left, imgWidth));
        right = Math.Max(0, Math.Min(right, imgWidth));
        top = Math.Max(0, Math.Min(top, imgHeight));
        bottom = Math.Max(0, Math.Min(bottom, imgHeight));

        double x = Math.Min(left, right);
        double y = Math.Min(top, bottom);
        double w = Math.Abs(right - left);
        double h = Math.Abs(bottom - top);

        return new Rect(x, y, w, h);
    }

    private void DrawCropCornerTicks(CanvasDrawingSession ds, Rect r)
    {
        float tickLen = 15f;
        float thickness = 4f;
        Color c = Microsoft.UI.Colors.White;
        
        ds.DrawLine(new Vector2((float)r.Left - 1, (float)r.Top), new Vector2((float)r.Left + tickLen, (float)r.Top), c, thickness);
        ds.DrawLine(new Vector2((float)r.Left, (float)r.Top - 1), new Vector2((float)r.Left, (float)r.Top + tickLen), c, thickness);
        
        ds.DrawLine(new Vector2((float)r.Right + 1, (float)r.Top), new Vector2((float)r.Right - tickLen, (float)r.Top), c, thickness);
        ds.DrawLine(new Vector2((float)r.Right, (float)r.Top - 1), new Vector2((float)r.Right, (float)r.Top + tickLen), c, thickness);
        
        ds.DrawLine(new Vector2((float)r.Right + 1, (float)r.Bottom), new Vector2((float)r.Right - tickLen, (float)r.Bottom), c, thickness);
        ds.DrawLine(new Vector2((float)r.Right, (float)r.Bottom + 1), new Vector2((float)r.Right, (float)r.Bottom - tickLen), c, thickness);
        
        ds.DrawLine(new Vector2((float)r.Left - 1, (float)r.Bottom), new Vector2((float)r.Left + tickLen, (float)r.Bottom), c, thickness);
        ds.DrawLine(new Vector2((float)r.Left, (float)r.Bottom + 1), new Vector2((float)r.Left, (float)r.Bottom - tickLen), c, thickness);
    }

    public void ApplyCrop()
    {
        if (Session == null || _cachedBitmap == null) return;
        
        Rect cropRect = _cropRectOriginal;
        int width = (int)cropRect.Width;
        int height = (int)cropRect.Height;
        
        if (width <= 0 || height <= 0) return;
        if (width == _cachedBitmap.Size.Width && height == _cachedBitmap.Size.Height)
        {
            ActiveTool = AnnotationType.Select;
            return;
        }

        try
        {
            Session.History.SaveState();

            var device = CanvasDevice.GetSharedDevice();
            SoftwareBitmap croppedBitmap;
            using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(device, Session.OriginalBitmap))
            using (var renderTarget = new CanvasRenderTarget(device, width, height, canvasBitmap.Dpi))
            {
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Microsoft.UI.Colors.Transparent);
                    Rect dest = new Rect(0, 0, width, height);
                    ds.DrawImage(canvasBitmap, dest, cropRect);
                }

                byte[] bytes = renderTarget.GetPixelBytes();
                croppedBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
                croppedBitmap.CopyFromBuffer(bytes.AsBuffer());
            }

            Session.OriginalBitmap.Dispose();
            Session.OriginalBitmap = croppedBitmap;

            Vector2 shift = new Vector2(-(float)cropRect.X, -(float)cropRect.Y);
            foreach (var element in Session.Annotations)
            {
                element.Move(shift);
            }

            _userZoomedOrPanned = false;
            _cachedBitmap?.Dispose();
            _cachedBitmap = null;

            UpdateSessionThumbnailAsync(Session);

            ActiveTool = AnnotationType.Select;
            Session.History.SaveState();
            _canvas.Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Crop error: {ex.Message}");
        }
    }

    public void ApplySliceCut()
    {
        if (Session == null || _cachedBitmap == null) return;

        Rect cutRect = _sliceCutRectOriginal;
        int imgWidth = (int)_cachedBitmap.Size.Width;
        int imgHeight = (int)_cachedBitmap.Size.Height;

        if (IsSliceCutHorizontal)
        {
            int minY = (int)Math.Max(0, Math.Min(cutRect.Top, imgHeight));
            int maxY = (int)Math.Max(0, Math.Min(cutRect.Bottom, imgHeight));
            int cutHeight = maxY - minY;

            if (cutHeight <= 0 || cutHeight >= imgHeight)
            {
                ActiveTool = AnnotationType.Select;
                return;
            }

            int newHeight = imgHeight - cutHeight;

            try
            {
                Session.History.SaveState();

                var device = CanvasDevice.GetSharedDevice();
                SoftwareBitmap stitchedBitmap;

                using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(device, Session.OriginalBitmap))
                using (var renderTarget = new CanvasRenderTarget(device, imgWidth, newHeight, canvasBitmap.Dpi))
                {
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        ds.Clear(Microsoft.UI.Colors.Transparent);
                        
                        // Top part
                        if (minY > 0)
                        {
                            Rect srcTop = new Rect(0, 0, imgWidth, minY);
                            Rect destTop = new Rect(0, 0, imgWidth, minY);
                            ds.DrawImage(canvasBitmap, destTop, srcTop);
                        }

                        // Bottom part
                        int bottomHeight = imgHeight - maxY;
                        if (bottomHeight > 0)
                        {
                            Rect srcBottom = new Rect(0, maxY, imgWidth, bottomHeight);
                            Rect destBottom = new Rect(0, minY, imgWidth, bottomHeight);
                            ds.DrawImage(canvasBitmap, destBottom, srcBottom);
                        }
                    }

                    byte[] bytes = renderTarget.GetPixelBytes();
                    stitchedBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, imgWidth, newHeight, BitmapAlphaMode.Premultiplied);
                    stitchedBitmap.CopyFromBuffer(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(bytes));
                }

                Session.OriginalBitmap.Dispose();
                Session.OriginalBitmap = stitchedBitmap;

                // Shift and clean annotations
                List<AnnotationElement> toRemove = new();
                foreach (var element in Session.Annotations)
                {
                    switch (element)
                    {
                        case RectElement r:
                            float newTop = ShiftY((float)r.Bounds.Top, minY, maxY, cutHeight);
                            float newBottom = ShiftY((float)r.Bounds.Bottom, minY, maxY, cutHeight);
                            r.Bounds = new Rect(r.Bounds.X, newTop, r.Bounds.Width, Math.Max(0, newBottom - newTop));
                            if (r.Bounds.Height <= 2) toRemove.Add(r);
                            break;

                        case BlurElement b:
                            float bTop = ShiftY((float)b.Bounds.Top, minY, maxY, cutHeight);
                            float bBottom = ShiftY((float)b.Bounds.Bottom, minY, maxY, cutHeight);
                            b.Bounds = new Rect(b.Bounds.X, bTop, b.Bounds.Width, Math.Max(0, bBottom - bTop));
                            if (b.Bounds.Height <= 2) toRemove.Add(b);
                            break;

                        case EraserElement er:
                            float erTop = ShiftY((float)er.Bounds.Top, minY, maxY, cutHeight);
                            float erBottom = ShiftY((float)er.Bounds.Bottom, minY, maxY, cutHeight);
                            er.Bounds = new Rect(er.Bounds.X, erTop, er.Bounds.Width, Math.Max(0, erBottom - erTop));
                            float srcTop = ShiftY((float)er.SourceBounds.Top, minY, maxY, cutHeight);
                            float srcBottom = ShiftY((float)er.SourceBounds.Bottom, minY, maxY, cutHeight);
                            er.SourceBounds = new Rect(er.SourceBounds.X, srcTop, er.SourceBounds.Width, Math.Max(0, srcBottom - srcTop));
                            if (er.Bounds.Height <= 2 || er.SourceBounds.Height <= 2) toRemove.Add(er);
                            break;

                        case HighlighterElement h:
                            float hTop = ShiftY((float)h.Bounds.Top, minY, maxY, cutHeight);
                            float hBottom = ShiftY((float)h.Bounds.Bottom, minY, maxY, cutHeight);
                            h.Bounds = new Rect(h.Bounds.X, hTop, h.Bounds.Width, Math.Max(0, hBottom - hTop));
                            if (h.Bounds.Height <= 2) toRemove.Add(h);
                            break;

                        case StepElement s:
                            s.Center = new Vector2(s.Center.X, ShiftY(s.Center.Y, minY, maxY, cutHeight));
                            if (s.Center.Y >= minY && s.Center.Y <= maxY) toRemove.Add(s);
                            break;

                        case ArrowElement a:
                            a.Start = new Vector2(a.Start.X, ShiftY(a.Start.Y, minY, maxY, cutHeight));
                            a.End = new Vector2(a.End.X, ShiftY(a.End.Y, minY, maxY, cutHeight));
                            if (Vector2.Distance(a.Start, a.End) <= 2) toRemove.Add(a);
                            break;

                        case TextElement t:
                            t.Position = new Vector2(t.Position.X, ShiftY(t.Position.Y, minY, maxY, cutHeight));
                            break;
                    }
                }

                foreach (var el in toRemove)
                {
                    Session.Annotations.Remove(el);
                }

                _userZoomedOrPanned = false;
                _cachedBitmap?.Dispose();
                _cachedBitmap = null;

                UpdateSessionThumbnailAsync(Session);

                ActiveTool = AnnotationType.Select;
                Session.History.SaveState();
                _canvas.Invalidate();
                ZoomChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Slice cut error: {ex.Message}");
            }
        }
        else
        {
            int minX = (int)Math.Max(0, Math.Min(cutRect.Left, imgWidth));
            int maxX = (int)Math.Max(0, Math.Min(cutRect.Right, imgWidth));
            int cutWidth = maxX - minX;

            if (cutWidth <= 0 || cutWidth >= imgWidth)
            {
                ActiveTool = AnnotationType.Select;
                return;
            }

            int newWidth = imgWidth - cutWidth;

            try
            {
                Session.History.SaveState();

                var device = CanvasDevice.GetSharedDevice();
                SoftwareBitmap stitchedBitmap;

                using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(device, Session.OriginalBitmap))
                using (var renderTarget = new CanvasRenderTarget(device, newWidth, imgHeight, canvasBitmap.Dpi))
                {
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        ds.Clear(Microsoft.UI.Colors.Transparent);
                        
                        // Left part
                        if (minX > 0)
                        {
                            Rect srcLeft = new Rect(0, 0, minX, imgHeight);
                            Rect destLeft = new Rect(0, 0, minX, imgHeight);
                            ds.DrawImage(canvasBitmap, destLeft, srcLeft);
                        }

                        // Right part
                        int rightWidth = imgWidth - maxX;
                        if (rightWidth > 0)
                        {
                            Rect srcRight = new Rect(maxX, 0, rightWidth, imgHeight);
                            Rect destRight = new Rect(minX, 0, rightWidth, imgHeight);
                            ds.DrawImage(canvasBitmap, destRight, srcRight);
                        }
                    }

                    byte[] bytes = renderTarget.GetPixelBytes();
                    stitchedBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, newWidth, imgHeight, BitmapAlphaMode.Premultiplied);
                    stitchedBitmap.CopyFromBuffer(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(bytes));
                }

                Session.OriginalBitmap.Dispose();
                Session.OriginalBitmap = stitchedBitmap;

                // Shift and clean annotations
                List<AnnotationElement> toRemove = new();
                foreach (var element in Session.Annotations)
                {
                    switch (element)
                    {
                        case RectElement r:
                            float newLeft = ShiftX((float)r.Bounds.Left, minX, maxX, cutWidth);
                            float newRight = ShiftX((float)r.Bounds.Right, minX, maxX, cutWidth);
                            r.Bounds = new Rect(newLeft, r.Bounds.Y, Math.Max(0, newRight - newLeft), r.Bounds.Height);
                            if (r.Bounds.Width <= 2) toRemove.Add(r);
                            break;

                        case BlurElement b:
                            float bLeft = ShiftX((float)b.Bounds.Left, minX, maxX, cutWidth);
                            float bRight = ShiftX((float)b.Bounds.Right, minX, maxX, cutWidth);
                            b.Bounds = new Rect(bLeft, b.Bounds.Y, Math.Max(0, bRight - bLeft), b.Bounds.Height);
                            if (b.Bounds.Width <= 2) toRemove.Add(b);
                            break;

                        case EraserElement er:
                            float erLeft = ShiftX((float)er.Bounds.Left, minX, maxX, cutWidth);
                            float erRight = ShiftX((float)er.Bounds.Right, minX, maxX, cutWidth);
                            er.Bounds = new Rect(erLeft, er.Bounds.Y, Math.Max(0, erRight - erLeft), er.Bounds.Height);
                            float srcLeft = ShiftX((float)er.SourceBounds.Left, minX, maxX, cutWidth);
                            float srcRight = ShiftX((float)er.SourceBounds.Right, minX, maxX, cutWidth);
                            er.SourceBounds = new Rect(srcLeft, er.SourceBounds.Y, Math.Max(0, srcRight - srcLeft), er.SourceBounds.Height);
                            if (er.Bounds.Width <= 2 || er.SourceBounds.Width <= 2) toRemove.Add(er);
                            break;

                        case HighlighterElement h:
                            float hLeft = ShiftX((float)h.Bounds.Left, minX, maxX, cutWidth);
                            float hRight = ShiftX((float)h.Bounds.Right, minX, maxX, cutWidth);
                            h.Bounds = new Rect(hLeft, h.Bounds.Y, Math.Max(0, hRight - hLeft), h.Bounds.Height);
                            if (h.Bounds.Width <= 2) toRemove.Add(h);
                            break;

                        case StepElement s:
                            s.Center = new Vector2(ShiftX(s.Center.X, minX, maxX, cutWidth), s.Center.Y);
                            if (s.Center.X >= minX && s.Center.X <= maxX) toRemove.Add(s);
                            break;

                        case ArrowElement a:
                            a.Start = new Vector2(ShiftX(a.Start.X, minX, maxX, cutWidth), a.Start.Y);
                            a.End = new Vector2(ShiftX(a.End.X, minX, maxX, cutWidth), a.End.Y);
                            if (Vector2.Distance(a.Start, a.End) <= 2) toRemove.Add(a);
                            break;

                        case TextElement t:
                            t.Position = new Vector2(ShiftX(t.Position.X, minX, maxX, cutWidth), t.Position.Y);
                            break;
                    }
                }

                foreach (var el in toRemove)
                {
                    Session.Annotations.Remove(el);
                }

                _userZoomedOrPanned = false;
                _cachedBitmap?.Dispose();
                _cachedBitmap = null;

                UpdateSessionThumbnailAsync(Session);

                ActiveTool = AnnotationType.Select;
                Session.History.SaveState();
                _canvas.Invalidate();
                ZoomChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Slice cut error: {ex.Message}");
            }
        }
    }

    private float ShiftY(float y, float minY, float maxY, float cutHeight)
    {
        if (y > maxY) return y - cutHeight;
        if (y < minY) return y;
        return minY;
    }

    private float ShiftX(float x, float minX, float maxX, float cutWidth)
    {
        if (x > maxX) return x - cutWidth;
        if (x < minX) return x;
        return minX;
    }

    private async void UpdateSessionThumbnailAsync(ScreenshotSession session)
    {
        try
        {
            var thumbnailBitmap = await ImageHelper.ScaleSoftwareBitmapAsync(session.OriginalBitmap, 160);
            var thumbnailSource = await ImageHelper.CreateSourceFromSoftwareBitmapAsync(thumbnailBitmap);
            session.Thumbnail = thumbnailSource;
        }
        catch { }
    }

    public void DeleteSelectedElement()
    {
        if (Session != null && SelectedElement != null)
        {
            Session.History.SaveState();
            Session.Annotations.Remove(SelectedElement);
            SelectedElement = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            _canvas.Invalidate();
        }
    }

    private string GetStepLabel(int counter)
    {
        if (StepSequencingFormat == "AlphaUpper") return GetAlphabetLabel(counter, true);
        if (StepSequencingFormat == "AlphaLower") return GetAlphabetLabel(counter, false);
        return counter.ToString();
    }

    private string GetAlphabetLabel(int counter, bool upper)
    {
        int val = counter - 1;
        string label = string.Empty;
        do {
            int rem = val % 26;
            char c = (char)((upper ? 'A' : 'a') + rem);
            label = c + label;
            val = (val / 26) - 1;
        } while (val >= 0);
        return label;
    }

    public void ClearSelection()
    {
        if (SelectedElement != null)
        {
            SelectedElement.IsSelected = false;
            SelectedElement = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            _canvas.Invalidate();
        }
    }
}
