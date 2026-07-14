# Screentation User Guide

This guide provides details on how to use Screentation for capturing, annotating, and managing your screenshots.

## Key Features

1. **Automatic Capture**
   * When the application is running (even when minimized to the system tray), pressing the `PrintScreen` key or taking a screenshot via Windows Snipping Tool automatically imports the screenshot into Screentation's active session.

2. **Annotation Tools**
   * **Select**: Move and resize annotations.
   * **Frame (Rect)**: Add rectangular frames, optionally with semi-transparent fills.
   * **Arrow**: Draw directional arrows to point out interface elements.
   * **Blur**: Mask sensitive details with Gaussian blur.
   * **Eraser (Clone Stamp)**: Hide UI elements by copying textures from one area to another.
   * **Text**: Add text annotations with customizable font sizes and colors.
   * **Step**: Draw auto-incremented step numbers.

3. **Smart Crop**
   * Crop the screenshot to any rectangular area. Confirmed crops automatically adjust and shift existing annotation elements so they remain in place relative to the new image boundaries.

4. **Zoom & Pan**
   * Hold the `Ctrl` key and scroll the mouse wheel to zoom in on the cursor position, or use the zoom slider in the settings panel.
   * Pan the zoomed canvas by holding the middle mouse button (wheel) and dragging.
   * Click "Reset" to fit the image to the window.

5. **Step Numbering Formats**
   * Supports numbers (`1, 2, 3...`), capital letters (`A, B, C...`), and lowercase letters (`a, b, c...`).
   * The starting sequence value can be adjusted using the "Next Step" control.

6. **Color Palette**
   * Quick-selection color presets are available.
   * Custom colors can be chosen using the spectral ColorPicker. Custom colors are saved across application restarts.

## Keyboard Shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl + V` | Manually paste screenshot from clipboard |
| `Ctrl + Z` | Undo last action |
| `Ctrl + Y` or `Ctrl + Shift + Z` | Redo last undone action |
| `Ctrl + S` | Save the active screenshot to disk |
| `Ctrl + Shift + S` | Save all screenshots (batch export) |
| `Delete` or `Backspace` | Remove selected annotation element |
| `Escape` | Reset active selection or exit current drawing tool |
| `Enter` (during crop) | Confirm and apply crop |
| `Escape` (during crop) | Cancel crop |
| `1` | Select Frame tool |
| `2` | Select Step tool |
| `3` | Select Arrow tool |
| `4` | Select Blur tool |
| `5` | Select Stamp (Eraser) tool |

## System Tray Integration

* Closing the main window (using the `X` close button) hides the application to the system tray to ensure background clipboard monitoring remains active.
* Double-click the system tray icon, or right-click the icon and choose **Open Screentation** to restore the window.
* Right-click the system tray icon and choose **Exit** to completely close the application.
