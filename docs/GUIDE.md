# Screentation V2 User Guide — How to Annotate Screenshots on Windows

> [!NOTE]
> Screentation V2 is a professional desktop utility that automates screenshot annotation, highlighting, and step-sequencing. It runs in the background, intercepting screenshots copied to the clipboard, and lets you add vector markups, blur details, crop, or slice cut images. Export files in high-quality WebP, PNG, or JPEG without overwriting existing files.

Screentation V2 provides a clean, native desktop environment for technical writers, software developers, and support specialists who create step-by-step instructions. By running silently in the system tray, it streamlines the workflow of capturing, markup editing, and exporting images.

## Core Features

### Clipboard Monitoring & System Tray
Screentation V2 does not capture screenshots on its own. Instead, it monitors your system clipboard. When you copy a screenshot using standard Windows tools (like `Win + Shift + S` or `PrintScreen`), Screentation instantly imports it. Closing the window minimizes the app to the system tray so that it continues capturing screenshots in the background.

### Premium Annotation Vector Markups
*   **Arrow**: Draw precise directional arrows to point out specific interface elements.
*   **Frame (Rect)**: Place rectangles around key components, optionally with semi-transparent background fills.
*   **Text**: Insert text annotations on top of the image with a custom font size and color.
*   **Highlighter**: Use the semi-transparent marker brush to highlight code snippets, UI text, or layout sections.
*   **Dynamic Thickness & Color Sliders**: Adjust the drawing pen thickness or select custom color schemes in real time; currently selected items will update instantly.

### Privacy Masking & Redaction
*   **Gaussian Blur**: Apply standard Gaussian blur overlay to mask passwords, emails, or personal data.
*   **Clone Stamp (Eraser)**: Hide interface elements by cloning textures from one area of the screenshot to another.

### Smart Cropping & Slice Cutting
*   **Smart Crop**: Crop your screenshot to any size. Existing annotation elements automatically shift coordinates so they remain perfectly aligned with the cropped image.
*   **Slice Cut**: Delete a horizontal or vertical strip of the screenshot. The remaining pieces snap together seamlessly, and all annotation elements below or to the right shift automatically.

### Auto-Sequenced Step Markers
Place step circles that increment automatically to build numbered walkthroughs.
*   Supports three formats: Numbers (`1, 2, 3...`), uppercase Latin letters (`A, B, C...`), and lowercase Latin letters (`a, b, c...`).
*   The circle size scales dynamically based on the active drawing thickness.
*   You can change the value of the next step at any time using the settings panel.

### File Export Formats (WebP, PNG, JPEG)
Save your annotated screenshots in **PNG**, **JPEG**, or compressed **WebP** formats.
*   The output directory is fully configurable.
*   Batch export ("Save All") saves all screenshots in one click.
*   File naming prevents overwrites by using the screenshot's list index (e.g. `Screentation_02.png`), ensuring your work is never lost.

## Interface Languages & Localization

Screentation V2 is designed for a global audience and is fully localized in **three languages**:
*   **English** (US)
*   **Russian** (RU)
*   **German** (DE)

### Automatic Detection
By default, the application automatically detects your Windows system display language on startup and loads the matching translation resources.

### Manual Language Override
If you want to run the application in a language different from your OS settings, you can launch it from the command line or terminal with the `--lang` flag:
```bash
Screentation.exe --lang en  # Force English interface
Screentation.exe --lang ru  # Force Russian interface
Screentation.exe --lang de  # Force German interface
```

## Quick-Start Instructions

1.  **Open the App**: Launch Screentation V2. You will see a clean workspace. The app also initializes in the system tray.
2.  **Take a Screenshot**: Use your default Windows shortcut (e.g., `Win + Shift + S`) to capture a portion of your screen.
3.  **Annotate**: Screentation will automatically pull the image. Click any tool in the left toolbar (e.g., Arrow, Highlighter, Step) and draw directly on the canvas.
4.  **Export**: Click **Save Active** (or press `Ctrl + S`) to save the current image, or **Save All** to batch-export all captures to your designated directory.

## Tips & Keyboard Shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl + V` | Manually paste screenshot from clipboard |
| `Ctrl + Z` | Undo last edit action |
| `Ctrl + Y` / `Ctrl + Shift + Z` | Redo last undone action |
| `Ctrl + S` | Save active screenshot |
| `Ctrl + Shift + S` | Save all screenshots (batch export) |
| `Delete` / `Backspace` | Delete currently selected annotation |
| `Escape` | Cancel active drawing tool or deselect elements |
| `Enter` | Apply active crop / slice cut |
| `Escape` (in crop/slice mode) | Cancel active crop / slice cut |
| `1` | Select Frame tool |
| `2` | Select Step tool |
| `3` | Select Arrow tool |
| `4` | Select Blur tool |
| `5` | Select Stamp (Eraser) tool |
| `Ctrl + Mouse Wheel` | Zoom canvas in or out centered on cursor |
| `Middle Mouse Button (Drag)` | Pan zoomed canvas |

## FAQ & Troubleshooting

### Windows Defender SmartScreen warning on launch?
Because the standalone installer is unsigned, Windows may show a warning on first run. Click **"More info"**, then select **"Run anyway"** to proceed.

### How to run multiple instances?
Screentation V2 is a single-instance app. Launching it again will automatically bring the active background instance to the foreground instead of creating duplicate tray icons.

### Why do UI buttons sometimes look white-on-white?
Screentation V2 includes a runtime theme synchronization listener. If your system changes theme (e.g. Dark to Light), the application will instantly update all controls and fonts to ensure they remain readable.

## Join the Community & Support

Screentation V2 is an open-source tool built to simplify screenshot workflow.
*   **Star the Repo**: If you find this tool helpful, please give us a star on [GitHub](https://github.com/Almanex/Screentation-V2)!
*   **Report Issues**: Encountered a bug or have a feature idea? Open an issue on our GitHub page.
*   **Contribute**: Pull requests are always welcome! Help us make Screentation even better for everyone.
