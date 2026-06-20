using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Windows.Foundation;

namespace Screentation;

public enum AnnotationType
{
    Select,
    Rect,
    Step,
    Arrow,
    Blur,
    Eraser, // Clone Stamp
    Text,
    Crop
}

public abstract class AnnotationElement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AnnotationType Type { get; protected set; }
    public Color Color { get; set; } = Microsoft.UI.Colors.Red;
    public float StrokeThickness { get; set; } = 3.0f;
    public bool IsSelected { get; set; } = false;

    // Helper for cloning elements for the Undo/Redo stack
    public abstract AnnotationElement Clone();

    // Hit-testing helper in original image coordinates
    public abstract bool HitTest(Vector2 point, float tolerance);
    
    // Bounds helper
    public abstract Rect GetBounds();

    // Move element by offset
    public abstract void Move(Vector2 offset);

    // Resize element
    public virtual void Resize(Vector2 delta, int handleIndex) { }
}

public class RectElement : AnnotationElement
{
    public Rect Bounds { get; set; }
    public bool HasFill { get; set; } = false;

    public RectElement()
    {
        Type = AnnotationType.Rect;
    }

    public override AnnotationElement Clone()
    {
        return new RectElement
        {
            Id = this.Id,
            Color = this.Color,
            StrokeThickness = this.StrokeThickness,
            Bounds = this.Bounds,
            HasFill = this.HasFill,
            IsSelected = this.IsSelected
        };
    }

    public override bool HitTest(Vector2 point, float tolerance)
    {
        // Check outline
        bool nearLeft = Math.Abs(point.X - Bounds.Left) <= tolerance && point.Y >= Bounds.Top - tolerance && point.Y <= Bounds.Bottom + tolerance;
        bool nearRight = Math.Abs(point.X - Bounds.Right) <= tolerance && point.Y >= Bounds.Top - tolerance && point.Y <= Bounds.Bottom + tolerance;
        bool nearTop = Math.Abs(point.Y - Bounds.Top) <= tolerance && point.X >= Bounds.Left - tolerance && point.X <= Bounds.Right + tolerance;
        bool nearBottom = Math.Abs(point.Y - Bounds.Bottom) <= tolerance && point.X >= Bounds.Left - tolerance && point.X <= Bounds.Right + tolerance;

        if (nearLeft || nearRight || nearTop || nearBottom) return true;

        // If filled, check inside
        if (HasFill)
        {
            return point.X >= Bounds.Left && point.X <= Bounds.Right && point.Y >= Bounds.Top && point.Y <= Bounds.Bottom;
        }

        return false;
    }

    public override Rect GetBounds() => Bounds;

    public override void Move(Vector2 offset)
    {
        Bounds = new Rect(Bounds.X + offset.X, Bounds.Y + offset.Y, Bounds.Width, Bounds.Height);
    }

    public override void Resize(Vector2 delta, int handleIndex)
    {
        double left = Bounds.Left;
        double right = Bounds.Right;
        double top = Bounds.Top;
        double bottom = Bounds.Bottom;

        // Handles: 
        // 0: TopLeft, 1: TopRight, 2: BottomRight, 3: BottomLeft
        // 4: Top, 5: Right, 6: Bottom, 7: Left
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

        // Keep width/height positive
        double x = Math.Min(left, right);
        double y = Math.Min(top, bottom);
        double w = Math.Abs(right - left);
        double h = Math.Abs(bottom - top);

        Bounds = new Rect(x, y, w, h);
    }
}

public class StepElement : AnnotationElement
{
    public Vector2 Center { get; set; }
    public float Radius { get; set; } = 75.0f;
    public int Number { get; set; }
    public string Label { get; set; } = string.Empty;

    public StepElement()
    {
        Type = AnnotationType.Step;
    }

    public override AnnotationElement Clone()
    {
        return new StepElement
        {
            Id = this.Id,
            Color = this.Color,
            StrokeThickness = this.StrokeThickness,
            Center = this.Center,
            Radius = this.Radius,
            Number = this.Number,
            Label = this.Label,
            IsSelected = this.IsSelected
        };
    }

    public override bool HitTest(Vector2 point, float tolerance)
    {
        return Vector2.Distance(point, Center) <= Radius + tolerance;
    }

    public override Rect GetBounds()
    {
        return new Rect(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);
    }

    public override void Move(Vector2 offset)
    {
        Center = new Vector2(Center.X + offset.X, Center.Y + offset.Y);
    }
}

public class TextElement : AnnotationElement
{
    public string Text { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
    public float FontSize { get; set; } = 36.0f;

    public TextElement()
    {
        Type = AnnotationType.Text;
    }

    public override AnnotationElement Clone()
    {
        return new TextElement
        {
            Id = this.Id,
            Color = this.Color,
            StrokeThickness = this.StrokeThickness,
            Text = this.Text,
            Position = this.Position,
            FontSize = this.FontSize,
            IsSelected = this.IsSelected
        };
    }

    public override bool HitTest(Vector2 point, float tolerance)
    {
        Rect bounds = GetBounds();
        return point.X >= bounds.Left - tolerance && point.X <= bounds.Right + tolerance &&
               point.Y >= bounds.Top - tolerance && point.Y <= bounds.Bottom + tolerance;
    }

    public override Rect GetBounds()
    {
        float charWidth = FontSize * 0.6f;
        float height = FontSize;
        float width = Math.Max(FontSize, Text.Length * charWidth);
        return new Rect(Position.X, Position.Y, width, height);
    }

    public override void Move(Vector2 offset)
    {
        Position = new Vector2(Position.X + offset.X, Position.Y + offset.Y);
    }

    public override void Resize(Vector2 delta, int handleIndex)
    {
        float sizeChange = delta.Y;
        FontSize = Math.Max(12.0f, FontSize + sizeChange);
    }
}

public class ArrowElement : AnnotationElement
{
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }

    public ArrowElement()
    {
        Type = AnnotationType.Arrow;
    }

    public override AnnotationElement Clone()
    {
        return new ArrowElement
        {
            Id = this.Id,
            Color = this.Color,
            StrokeThickness = this.StrokeThickness,
            Start = this.Start,
            End = this.End,
            IsSelected = this.IsSelected
        };
    }

    public override bool HitTest(Vector2 point, float tolerance)
    {
        // Distance from point to line segment
        float l2 = Vector2.DistanceSquared(Start, End);
        if (l2 == 0.0f) return Vector2.Distance(point, Start) <= tolerance;

        float t = Math.Max(0.0f, Math.Min(1.0f, Vector2.Dot(point - Start, End - Start) / l2));
        Vector2 projection = Start + t * (End - Start);
        return Vector2.Distance(point, projection) <= tolerance;
    }

    public override Rect GetBounds()
    {
        float minX = Math.Min(Start.X, End.X);
        float maxX = Math.Max(Start.X, End.X);
        float minY = Math.Min(Start.Y, End.Y);
        float maxY = Math.Max(Start.Y, End.Y);
        return new Rect(minX, minY, Math.Max(1.0f, maxX - minX), Math.Max(1.0f, maxY - minY));
    }

    public override void Move(Vector2 offset)
    {
        Start = new Vector2(Start.X + offset.X, Start.Y + offset.Y);
        End = new Vector2(End.X + offset.X, End.Y + offset.Y);
    }

    public override void Resize(Vector2 delta, int handleIndex)
    {
        // 0: Start point, 1: End point
        if (handleIndex == 0)
        {
            Start += delta;
        }
        else if (handleIndex == 1)
        {
            End += delta;
        }
    }
}

public class BlurElement : AnnotationElement
{
    public Rect Bounds { get; set; }
    public float BlurRadius { get; set; } = 15.0f;

    public BlurElement()
    {
        Type = AnnotationType.Blur;
        Color = Microsoft.UI.Colors.Transparent; // No stroke color needed usually, or transparent
    }

    public override AnnotationElement Clone()
    {
        return new BlurElement
        {
            Id = this.Id,
            Color = this.Color,
            StrokeThickness = this.StrokeThickness,
            Bounds = this.Bounds,
            BlurRadius = this.BlurRadius,
            IsSelected = this.IsSelected
        };
    }

    public override bool HitTest(Vector2 point, float tolerance)
    {
        // Blur regions can be selected by clicking anywhere inside them
        return point.X >= Bounds.Left && point.X <= Bounds.Right && point.Y >= Bounds.Top && point.Y <= Bounds.Bottom;
    }

    public override Rect GetBounds() => Bounds;

    public override void Move(Vector2 offset)
    {
        Bounds = new Rect(Bounds.X + offset.X, Bounds.Y + offset.Y, Bounds.Width, Bounds.Height);
    }

    public override void Resize(Vector2 delta, int handleIndex)
    {
        double left = Bounds.Left;
        double right = Bounds.Right;
        double top = Bounds.Top;
        double bottom = Bounds.Bottom;

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

        double x = Math.Min(left, right);
        double y = Math.Min(top, bottom);
        double w = Math.Abs(right - left);
        double h = Math.Abs(bottom - top);

        Bounds = new Rect(x, y, w, h);
    }
}

public class EraserElement : AnnotationElement
{
    public Rect Bounds { get; set; } // Target bounds (where the patch is placed)
    public Vector2 SourceOffset { get; set; } = new Vector2(50.0f, 50.0f); // Relative offset to source area

    public Rect SourceBounds
    {
        get => new Rect(Bounds.X + SourceOffset.X, Bounds.Y + SourceOffset.Y, Bounds.Width, Bounds.Height);
        set => SourceOffset = new Vector2((float)(value.X - Bounds.X), (float)(value.Y - Bounds.Y));
    }

    public EraserElement()
    {
        Type = AnnotationType.Eraser;
        Color = Microsoft.UI.Colors.Magenta; // Color of guidelines / dashed line
    }

    public override AnnotationElement Clone()
    {
        return new EraserElement
        {
            Id = this.Id,
            Color = this.Color,
            StrokeThickness = this.StrokeThickness,
            Bounds = this.Bounds,
            SourceOffset = this.SourceOffset,
            IsSelected = this.IsSelected
        };
    }

    public override bool HitTest(Vector2 point, float tolerance)
    {
        // Hit test target area with tolerance
        bool inTarget = point.X >= Bounds.Left - tolerance && point.X <= Bounds.Right + tolerance && 
                        point.Y >= Bounds.Top - tolerance && point.Y <= Bounds.Bottom + tolerance;
        
        // If selected, we can also hit test source area with tolerance
        bool inSource = false;
        if (IsSelected)
        {
            Rect src = SourceBounds;
            inSource = point.X >= src.Left - tolerance && point.X <= src.Right + tolerance && 
                       point.Y >= src.Top - tolerance && point.Y <= src.Bottom + tolerance;
        }

        return inTarget || inSource;
    }

    public override Rect GetBounds() => Bounds;

    public override void Move(Vector2 offset)
    {
        // Moving the target moves the source along with it
        Bounds = new Rect(Bounds.X + offset.X, Bounds.Y + offset.Y, Bounds.Width, Bounds.Height);
    }

    public void MoveSourceOnly(Vector2 offset)
    {
        SourceOffset += offset;
    }

    public override void Resize(Vector2 delta, int handleIndex)
    {
        double left = Bounds.Left;
        double right = Bounds.Right;
        double top = Bounds.Top;
        double bottom = Bounds.Bottom;

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

        double x = Math.Min(left, right);
        double y = Math.Min(top, bottom);
        double w = Math.Abs(right - left);
        double h = Math.Abs(bottom - top);

        Bounds = new Rect(x, y, w, h);
    }
}

public class ScreenshotSession : IDisposable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Screenshot";
    public SoftwareBitmap OriginalBitmap { get; set; }
    public Microsoft.UI.Xaml.Media.ImageSource Thumbnail { get; set; }
    
    public ObservableCollection<AnnotationElement> Annotations { get; set; } = new();
    
    // Step counter specific to this session
    public int StepCounter { get; set; } = 1;
    
    // History specific to this session
    public HistoryManager History { get; }

    public ScreenshotSession(SoftwareBitmap bitmap, Microsoft.UI.Xaml.Media.ImageSource thumbnail, string name)
    {
        OriginalBitmap = bitmap;
        Thumbnail = thumbnail;
        Name = name;
        History = new HistoryManager(this);
    }

    public void Dispose()
    {
        OriginalBitmap?.Dispose();
    }
}
