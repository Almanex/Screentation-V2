using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Windows.UI.Core;

namespace Screentation;

public sealed partial class MainPage : Page
{
    private readonly ObservableCollection<ScreenshotSession> _sessions = new();
    private readonly ObservableCollection<ColorItem> _colors = new();
    private ClipboardMonitor? _clipboardMonitor;
    private int _screenshotCounter = 0;
    private bool _isInitialized = false;
    private readonly Microsoft.Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader = new();

    public MainPage()
    {
        this.InitializeComponent();
        
        // Populate color palette
        InitializeColors();
        
        // Bind lists
        ListViewThumbnails.ItemsSource = _sessions;
        GridViewColors.ItemsSource = _colors;

        this.Loaded += MainPage_Loaded;
        this.Unloaded += MainPage_Unloaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;

        // Initialize Clipboard Monitor
        if (MainWindow.Instance != null)
        {
            _clipboardMonitor = new ClipboardMonitor(MainWindow.Instance);
            _clipboardMonitor.ImageCaptured += OnImageCaptured;
        }

        // Load settings and bind to UI
        LoadSettingsToUI();

        // Connect canvas settings
        UpdateCanvasSettings();

        // Bind zoom and step sequencing formats
        DrawingCanvas.ZoomChanged += (s, ev) => 
        {
            float pct = DrawingCanvas.ZoomPercent;
            TxtZoomLevel.Text = string.Format(_resourceLoader.GetString("ZoomLevelTemplate"), pct);
            SliderZoom.Value = pct;
        };
        SliderZoom.Value = DrawingCanvas.ZoomPercent;
        TxtZoomLevel.Text = string.Format(_resourceLoader.GetString("ZoomLevelTemplate"), DrawingCanvas.ZoomPercent);

        DrawingCanvas.NextStepValueChanged += (s, val) => NumNextStep.Value = val;
        DrawingCanvas.ActiveFontSize = (float)SliderFontSize.Value;
        DrawingCanvas.StepSequencingFormat = (string)((ComboBoxItem)ComboStepFormat.SelectedItem).Tag;

        // Hook hotkeys
        this.KeyDown += MainPage_KeyDown;

        UpdateUIState();
        _isInitialized = true;
    }

    private void MainPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_clipboardMonitor != null)
        {
            _clipboardMonitor.ImageCaptured -= OnImageCaptured;
            _clipboardMonitor.Dispose();
            _clipboardMonitor = null;
        }
    }

    private void InitializeColors()
    {
        var palette = new[]
        {
            Microsoft.UI.Colors.Red,
            Microsoft.UI.Colors.Green,
            Microsoft.UI.Colors.Blue,
            Microsoft.UI.Colors.Yellow,
            Microsoft.UI.Colors.Orange,
            Microsoft.UI.Colors.Purple,
            Microsoft.UI.Colors.Black,
            Microsoft.UI.Colors.White
        };

        foreach (var color in palette)
        {
            _colors.Add(new ColorItem { Color = color, IsSelected = false });
        }
    }

    private void LoadSettingsToUI()
    {
        TxtExportPath.Text = SettingsManager.Current.ExportPath;
        TxtNamingTemplate.Text = SettingsManager.Current.NamingTemplate;
        SliderThickness.Value = SettingsManager.Current.StrokeThickness;
        CheckboxHasFill.IsChecked = SettingsManager.Current.HasFill;
        SliderQuality.Value = SettingsManager.Current.ExportQuality;

        // Set active format
        var formatItem = ComboFormat.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (string)i.Tag == SettingsManager.Current.ExportFormat);
        ComboFormat.SelectedItem = formatItem ?? ComboFormat.Items[0];

        // Set active color
        Color strokeColor = SettingsManager.Current.StrokeColor;
        CustomColorPicker.Color = strokeColor;

        var matchingColor = _colors.FirstOrDefault(c => c.Color == strokeColor);
        if (matchingColor != null)
        {
            matchingColor.IsSelected = true;
            GridViewColors.SelectedItem = matchingColor;
        }
        else
        {
            GridViewColors.SelectedItem = null;
        }

        // Set active tool default
        SetTool(AnnotationType.Select);
    }

    private void UpdateCanvasSettings()
    {
        DrawingCanvas.ActiveThickness = (float)SliderThickness.Value;
        DrawingCanvas.HasFill = CheckboxHasFill.IsChecked ?? false;

        if (GridViewColors.SelectedItem is ColorItem activeColorItem)
        {
            DrawingCanvas.ActiveColor = activeColorItem.Color;
        }
        else
        {
            DrawingCanvas.ActiveColor = CustomColorPicker.Color;
        }
    }

    private void UpdateUIState()
    {
        bool hasActiveSession = ListViewThumbnails.SelectedItem != null;
        
        DrawingCanvas.Visibility = hasActiveSession ? Visibility.Visible : Visibility.Collapsed;
        TextCanvasPlaceholder.Visibility = hasActiveSession ? Visibility.Collapsed : Visibility.Visible;
        TextNoScreenshots.Visibility = _sessions.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

        // Enable/disable panels
        BtnUndo.IsEnabled = hasActiveSession && (DrawingCanvas.Session?.History.CanUndo ?? false);
        BtnRedo.IsEnabled = hasActiveSession && (DrawingCanvas.Session?.History.CanRedo ?? false);
        BtnSaveActive.IsEnabled = hasActiveSession;
        BtnSaveAll.IsEnabled = _sessions.Count > 0;
    }

    private void OnImageCaptured(object? sender, SoftwareBitmap bitmap)
    {
        // Enqueue to UI thread
        DispatcherQueue.TryEnqueue(async () =>
        {
            var thumbnailBitmap = await ImageHelper.ScaleSoftwareBitmapAsync(bitmap, 160);
            var thumbnailSource = await ImageHelper.CreateSourceFromSoftwareBitmapAsync(thumbnailBitmap);

            string name = string.Format(_resourceLoader.GetString("ScreenshotNameTemplate"), ++_screenshotCounter);
            var session = new ScreenshotSession(bitmap, thumbnailSource, name);
            
            // Hook history changes to update undo/redo buttons
            session.History.HistoryChanged += (s, e) => UpdateUIState();

            _sessions.Add(session);
            ListViewThumbnails.SelectedItem = session;
            
            UpdateUIState();
        });
    }

    private async void BtnPaste_Click(object sender, RoutedEventArgs e)
    {
        await PasteFromClipboardAsync();
    }

    private async Task PasteFromClipboardAsync()
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                var imageStreamRef = await dataPackageView.GetBitmapAsync();
                using var stream = await imageStreamRef.OpenReadAsync();
                
                var decoder = await BitmapDecoder.CreateAsync(stream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    var converted = SoftwareBitmap.Convert(
                        softwareBitmap, 
                        BitmapPixelFormat.Bgra8, 
                        BitmapAlphaMode.Premultiplied
                    );
                    softwareBitmap.Dispose();
                    softwareBitmap = converted;
                }

                var thumbnailBitmap = await ImageHelper.ScaleSoftwareBitmapAsync(softwareBitmap, 160);
                var thumbnailSource = await ImageHelper.CreateSourceFromSoftwareBitmapAsync(thumbnailBitmap);

                string name = string.Format(_resourceLoader.GetString("ScreenshotNameTemplate"), ++_screenshotCounter);
                var session = new ScreenshotSession(softwareBitmap, thumbnailSource, name);
                session.History.HistoryChanged += (s, e) => UpdateUIState();

                _sessions.Add(session);
                ListViewThumbnails.SelectedItem = session;
                
                UpdateUIState();
            }
        }
        catch
        {
            // Ignore clipboard errors
        }
    }

    private void ListViewThumbnails_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedSession = (ScreenshotSession?)ListViewThumbnails.SelectedItem;
        DrawingCanvas.Session = selectedSession;
        UpdateUIState();
    }

    private void DrawingCanvas_SelectionChanged(object sender, EventArgs e)
    {
        UpdateUIState();
    }

    private void BtnDeleteSession_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ScreenshotSession session)
        {
            int index = _sessions.IndexOf(session);
            _sessions.Remove(session);
            session.Dispose();

            // Select next screenshot
            if (_sessions.Count > 0)
            {
                int nextIndex = Math.Min(index, _sessions.Count - 1);
                ListViewThumbnails.SelectedIndex = nextIndex;
            }
            else
            {
                ListViewThumbnails.SelectedItem = null;
            }

            UpdateUIState();
        }
    }

    private void ToolBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton btn && btn.Tag is string tag)
        {
            var tool = Enum.Parse<AnnotationType>(tag);
            SetTool(tool);
        }
    }

    private void SetTool(AnnotationType tool)
    {
        DrawingCanvas.ActiveTool = tool;

        ToolBtnSelect.IsChecked = (tool == AnnotationType.Select);
        ToolBtnRect.IsChecked = (tool == AnnotationType.Rect);
        ToolBtnArrow.IsChecked = (tool == AnnotationType.Arrow);
        ToolBtnStep.IsChecked = (tool == AnnotationType.Step);
        ToolBtnBlur.IsChecked = (tool == AnnotationType.Blur);
        ToolBtnEraser.IsChecked = (tool == AnnotationType.Eraser);
        ToolBtnText.IsChecked = (tool == AnnotationType.Text);
        ToolBtnCrop.IsChecked = (tool == AnnotationType.Crop);

        PanelFontSize.Visibility = (tool == AnnotationType.Text) ? Visibility.Visible : Visibility.Collapsed;
        PanelStepSettings.Visibility = (tool == AnnotationType.Step) ? Visibility.Visible : Visibility.Collapsed;
        BorderCropActions.Visibility = (tool == AnnotationType.Crop) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnResetSteps_Click(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas.Session != null)
        {
            DrawingCanvas.Session.StepCounter = 1;
            DrawingCanvas.NextStepValue = 1;
        }
    }

    private void SliderThickness_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_isInitialized) return;
        SettingsManager.Current.StrokeThickness = (float)e.NewValue;
        SettingsManager.Save();
        UpdateCanvasSettings();
    }

    private void CheckboxHasFill_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        SettingsManager.Current.HasFill = CheckboxHasFill.IsChecked ?? false;
        SettingsManager.Save();
        UpdateCanvasSettings();
    }

    private void GridViewColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        
        // Reset selections
        foreach (var item in _colors)
        {
            item.IsSelected = false;
        }

        if (GridViewColors.SelectedItem is ColorItem selectedColorItem)
        {
            selectedColorItem.IsSelected = true;
            SettingsManager.Current.StrokeColor = selectedColorItem.Color;
            SettingsManager.Save();
            UpdateCanvasSettings();
        }
    }

    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        UndoActive();
    }

    private void BtnRedo_Click(object sender, RoutedEventArgs e)
    {
        RedoActive();
    }

    private void UndoActive()
    {
        DrawingCanvas.Session?.History.Undo();
        UpdateUIState();
    }

    private void RedoActive()
    {
        DrawingCanvas.Session?.History.Redo();
        UpdateUIState();
    }

    private void TxtExportPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        SettingsManager.Current.ExportPath = TxtExportPath.Text;
        SettingsManager.Save();
    }

    private async void BtnBrowsePath_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        
        // Initialize folder picker on MainWindow handle
        if (MainWindow.Instance != null)
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        }

        folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        folderPicker.FileTypeFilter.Add("*");

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            TxtExportPath.Text = folder.Path;
            SettingsManager.Current.ExportPath = folder.Path;
            SettingsManager.Save();
        }
    }

    private void ComboFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboFormat.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            PanelQuality.Visibility = (tag == "JPEG" || tag == "WebP") ? Visibility.Visible : Visibility.Collapsed;

            if (_isInitialized)
            {
                SettingsManager.Current.ExportFormat = tag;
                SettingsManager.Save();
            }
        }
    }

    private void SliderQuality_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_isInitialized) return;
        SettingsManager.Current.ExportQuality = (int)e.NewValue;
        SettingsManager.Save();
    }

    private void TxtNamingTemplate_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        SettingsManager.Current.NamingTemplate = TxtNamingTemplate.Text;
        SettingsManager.Save();
    }

    private void SliderFontSize_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_isInitialized) return;
        DrawingCanvas.ActiveFontSize = (float)e.NewValue;
    }

    private void ComboStepFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized) return;
        if (ComboStepFormat.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            DrawingCanvas.StepSequencingFormat = tag;
        }
    }

    private void NumNextStep_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_isInitialized) return;
        if (!double.IsNaN(args.NewValue))
        {
            DrawingCanvas.NextStepValue = (int)args.NewValue;
        }
    }

    private void BtnResetZoom_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.ResetZoom();
    }

    private void BtnApplyCrop_Click(object sender, RoutedEventArgs e)
    {
        DrawingCanvas.ApplyCrop();
    }

    private void BtnCancelCrop_Click(object sender, RoutedEventArgs e)
    {
        SetTool(AnnotationType.Select);
    }

    private void SliderZoom_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_isInitialized) return;
        if (Math.Abs(DrawingCanvas.ZoomPercent - e.NewValue) > 1.0f)
        {
            DrawingCanvas.ZoomPercent = (float)e.NewValue;
        }
    }

    private void CustomColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (!_isInitialized) return;
        GridViewColors.SelectedItem = null;
        Color newColor = args.NewColor;
        SettingsManager.Current.StrokeColor = newColor;
        SettingsManager.Save();
        DrawingCanvas.ActiveColor = newColor;
    }

    private void BtnApplyCustomColor_Click(object sender, RoutedEventArgs e)
    {
        FlyoutColorPicker.Hide();
    }

    private async void BtnSaveActive_Click(object sender, RoutedEventArgs e)
    {
        await SaveActiveScreenshotAsync();
    }

    private async Task SaveActiveScreenshotAsync()
    {
        var activeSession = DrawingCanvas.Session;
        if (activeSession == null) return;

        try
        {
            // Clear selection before render
            DrawingCanvas.ClearSelection();
            
            await ExportManager.ExportSessionAsync(
                activeSession,
                1,
                SettingsManager.Current.ExportPath,
                SettingsManager.Current.ExportFormat,
                SettingsManager.Current.ExportQuality,
                SettingsManager.Current.NamingTemplate
            );

            ShowStatusDialog(_resourceLoader.GetString("DialogSuccess"), _resourceLoader.GetString("SaveActiveSuccess"));
        }
        catch (Exception ex)
        {
            ShowStatusDialog(_resourceLoader.GetString("DialogError"), string.Format(_resourceLoader.GetString("SaveActiveError"), ex.Message));
        }
    }

    private async void BtnSaveAll_Click(object sender, RoutedEventArgs e)
    {
        await SaveAllScreenshotsAsync();
    }

    private async Task SaveAllScreenshotsAsync()
    {
        if (_sessions.Count == 0) return;

        try
        {
            DrawingCanvas.ClearSelection();

            int index = 1;
            foreach (var session in _sessions)
            {
                await ExportManager.ExportSessionAsync(
                    session,
                    index++,
                    SettingsManager.Current.ExportPath,
                    SettingsManager.Current.ExportFormat,
                    SettingsManager.Current.ExportQuality,
                    SettingsManager.Current.NamingTemplate
                );
            }

            ShowStatusDialog(_resourceLoader.GetString("DialogSuccess"), string.Format(_resourceLoader.GetString("SaveAllSuccess"), _sessions.Count));
        }
        catch (Exception ex)
        {
            ShowStatusDialog(_resourceLoader.GetString("DialogError"), string.Format(_resourceLoader.GetString("SaveAllError"), ex.Message));
        }
    }

    private async void ShowStatusDialog(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var focused = FocusManager.GetFocusedElement(this.XamlRoot);
        if (focused is TextBox)
        {
            // Ignore text inputs for single-key shortcuts
            return;
        }

        var window = MainWindow.Instance;
        if (window == null) return;

        // Check modifiers
        bool ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        bool shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

        HandleKeyDown(e.Key, ctrl, shift);
    }

    private void HandleKeyDown(VirtualKey key, bool isCtrlDown, bool isShiftDown)
    {
        if (isCtrlDown)
        {
            switch (key)
            {
                case VirtualKey.Z:
                    if (isShiftDown) RedoActive();
                    else UndoActive();
                    break;
                case VirtualKey.Y:
                    RedoActive();
                    break;
                case VirtualKey.S:
                    if (isShiftDown) _ = SaveAllScreenshotsAsync();
                    else _ = SaveActiveScreenshotAsync();
                    break;
            }
        }
        else
        {
            switch (key)
            {
                case VirtualKey.Number1: SetTool(AnnotationType.Rect); break;
                case VirtualKey.Number2: SetTool(AnnotationType.Step); break;
                case VirtualKey.Number3: SetTool(AnnotationType.Arrow); break;
                case VirtualKey.Number4: SetTool(AnnotationType.Blur); break;
                case VirtualKey.Number5: SetTool(AnnotationType.Eraser); break;
                case VirtualKey.Enter:
                    if (DrawingCanvas.ActiveTool == AnnotationType.Crop)
                    {
                        DrawingCanvas.ApplyCrop();
                    }
                    break;
                case VirtualKey.Escape:
                    if (DrawingCanvas.SelectedElement != null)
                    {
                        DrawingCanvas.ClearSelection();
                    }
                    else
                    {
                        SetTool(AnnotationType.Select);
                    }
                    break;
                case VirtualKey.Delete:
                case VirtualKey.Back:
                    DrawingCanvas.DeleteSelectedElement();
                    break;
            }
        }
    }
}

public class ColorItem : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isSelected;
    public Color Color { get; set; }
    public SolidColorBrush Brush => new SolidColorBrush(Color);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(IsSelectedVisibility));
            }
        }
    }

    public Visibility IsSelectedVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
