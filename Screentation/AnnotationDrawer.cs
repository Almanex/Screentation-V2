using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;

namespace Screentation;

public static class AnnotationDrawer
{
    public static void DrawElement(
        ICanvasResourceCreator resourceCreator, 
        CanvasDrawingSession ds, 
        AnnotationElement element, 
        CanvasBitmap? backgroundBitmap)
    {
        switch (element)
        {
            case RectElement rectEl:
                if (rectEl.HasFill)
                {
                    var fillCol = Color.FromArgb((byte)(rectEl.Color.A * 0.1f), rectEl.Color.R, rectEl.Color.G, rectEl.Color.B);
                    ds.FillRectangle(rectEl.Bounds, fillCol);
                }
                ds.DrawRectangle(rectEl.Bounds, rectEl.Color, rectEl.StrokeThickness);
                break;

            case StepElement stepEl:
                ds.FillCircle(stepEl.Center, stepEl.Radius, stepEl.Color);
                ds.DrawCircle(stepEl.Center, stepEl.Radius, Microsoft.UI.Colors.White, 1.5f);

                using (var tf = new CanvasTextFormat
                       {
                           FontFamily = "Segoe UI Variable Text",
                           FontSize = stepEl.Radius * 1.0f,
                           FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                           HorizontalAlignment = CanvasHorizontalAlignment.Center,
                           VerticalAlignment = CanvasVerticalAlignment.Center
                       })
                {
                    string textToDraw = string.IsNullOrEmpty(stepEl.Label) ? stepEl.Number.ToString() : stepEl.Label;
                    ds.DrawText(textToDraw, stepEl.Center, Microsoft.UI.Colors.White, tf);
                }
                break;

            case TextElement textEl:
                using (var tf = new CanvasTextFormat
                       {
                           FontFamily = "Segoe UI Variable Text",
                           FontSize = textEl.FontSize,
                           FontWeight = Microsoft.UI.Text.FontWeights.Normal,
                           HorizontalAlignment = CanvasHorizontalAlignment.Left,
                           VerticalAlignment = CanvasVerticalAlignment.Top
                       })
                {
                    ds.DrawText(textEl.Text, textEl.Position, textEl.Color, tf);
                }
                break;

            case ArrowElement arrowEl:
                Vector2 diff = arrowEl.End - arrowEl.Start;
                float len = diff.Length();
                if (len > 0)
                {
                    Vector2 dir = diff / len;
                    Vector2 normal = new Vector2(-dir.Y, dir.X);

                    float headLen = arrowEl.StrokeThickness * 4f + 8f;
                    float headWidth = arrowEl.StrokeThickness * 3f + 6f;

                    if (len > headLen)
                    {
                        Vector2 arrowBase = arrowEl.End - dir * headLen;
                        Vector2 leftPt = arrowBase + normal * (headWidth / 2f);
                        Vector2 rightPt = arrowBase - normal * (headWidth / 2f);

                        ds.DrawLine(arrowEl.Start, arrowBase, arrowEl.Color, arrowEl.StrokeThickness);

                        var points = new[] { arrowEl.End, leftPt, rightPt };
                        var geometry = CanvasGeometry.CreatePolygon(resourceCreator, points);
                        ds.FillGeometry(geometry, arrowEl.Color);
                    }
                    else
                    {
                        ds.DrawLine(arrowEl.Start, arrowEl.End, arrowEl.Color, arrowEl.StrokeThickness);
                    }
                }
                break;

            case BlurElement blurEl:
                if (backgroundBitmap != null && blurEl.Bounds.Width > 0 && blurEl.Bounds.Height > 0)
                {
                    var blurEffect = new GaussianBlurEffect
                    {
                        Source = backgroundBitmap,
                        BlurAmount = blurEl.BlurRadius
                    };
                    ds.DrawImage(blurEffect, blurEl.Bounds, blurEl.Bounds);
                }
                break;

            case EraserElement eraserEl:
                if (backgroundBitmap != null && eraserEl.Bounds.Width > 0 && eraserEl.Bounds.Height > 0)
                {
                    ds.DrawImage(backgroundBitmap, eraserEl.Bounds, eraserEl.SourceBounds);
                }
                break;

            case HighlighterElement highEl:
                if (highEl.Bounds.Width > 0 && highEl.Bounds.Height > 0)
                {
                    var fillCol = Color.FromArgb((byte)(255 * highEl.Opacity), highEl.Color.R, highEl.Color.G, highEl.Color.B);
                    ds.FillRectangle(highEl.Bounds, fillCol);
                    if (highEl.IsSelected)
                    {
                        ds.DrawRectangle(highEl.Bounds, Microsoft.UI.Colors.LightSkyBlue, 1.0f);
                    }
                }
                break;
        }
    }
}
