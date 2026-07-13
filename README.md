[ English ](README.md) • [ Русский ](README_RU.md) • [ Deutsch ](README_DE.md)

# Screentation

**Screentation** is a native Windows application for quickly creating, editing and annotating screenshots. Written in C# using the modern interface **WinUI 3 (Windows App SDK)** and the graphics library **Win2D** for hardware rendering.

The application allows you to instantly capture screenshots from the clipboard (including in a minimized state), apply arrows, frames, text on top of them, blur sensitive data, crop images and automatically number instruction steps (both in numbers and in Latin letters).

---

## User Guide

### Key Features
1. **Automatic Capture**: When the application is open or simply minimized to tray, when you click `PrintScreen` (or take a photo using Windows Snipping Tool), the screenshot is automatically added to the list on the left.
2. **Annotation Tools**:
 - **Select**: Select, move and resize applied elements.
 - **Frame (Rect)**: Creates rectangular areas (optional with translucent fill).
 - **Arrow**: Create directional arrows.
 - **Blur**: Gaussian blur effect to hide sensitive information.
 - **Eraser**: Copy and transfer texture (clone area) to hide interface elements.
 - **Text**: Enter custom text on the canvas with the ability to change font size and color.
 - **Step**: Automatic sequence of steps.
3. **Smart Crop**:
 - Allows you to crop the screenshot to any rectangular area.
 - When cropping is confirmed, all previously drawn elements are shifted to the corresponding vector, remaining exactly in their places relative to the cropped frame.
4. **Zoom & Pan**:
 - Scaling is performed by holding the **`Ctrl` key + mouse wheel** (focuses on the cursor) or by dragging the **scaling slider** in the right panel.
 - Moving around the enlarged canvas is done by **holding the middle mouse button (wheel)** and dragging.
 - The **“Reset”** button returns the scale to the “fit to window” mode.
5. **Numbering of steps**:
 - Supports 3 formats: Numbers (`1, 2, 3...`), Latin capital letters (`A, B... Z, AA...`), Latin small letters (`a, b... z, aa...`).
 - The “Next step” field allows you to set the starting number or change the current index.
6. **Color selection**:
 - Ready-made quick colors available.
 - The **“Select color...”** button opens the spectral palette (ColorPicker) for selecting any custom shade. The selected color is retained between application restarts.

### Keyboard Shortcuts
| Key | Action |
| :--- | :--- |
| `Ctrl + V` | Paste screenshot from clipboard manually |
| `Ctrl + Z` | Undo last action (Undo) |
| `Ctrl + Y` / `Ctrl + Shift + Z` | Redo a undone action (Redo) |
| `Ctrl + S` | Save active screenshot to disk |
| `Ctrl + Shift + S` | Save all screenshots (batch saving) |
| `Delete` / `Backspace` | Delete selected annotation element |
| `Escape` | Exit current mode / reset selection |
| `Enter` *(in frame mode)* | Apply crop |
| `Escape` *(in frame mode)* | Undo crop |
| `1`, `2`, `3`, `4`, `5` | Quick selection of tools: Frame, Step, Arrow, Blur, Stamp |

---

## Developer Guide

### Technology stack
* **Platform**: .NET 10.0, Windows 10/11
* **UI Framework**: WinUI 3 (Windows App SDK 2.2.0)
* **Graphics**: Win2D (Microsoft.Graphics.Win2D 1.4.0) hardware 2D rendering based on Direct2D
* **System integration**: Subclassing windows (Comctl32) to intercept Win32 clipboard events

### Project structure
```
Screentation/
├── Assets/                    # Иконки, заставки и графические ресурсы
├── Properties/                # Параметры запуска (launchSettings.json)
├── Models.cs                  # Модели данных (ScreenshotSession, AnnotationElement, etc.)
├── AnnotationCanvas.cs        # Интерактивный холст (обработка мыши, отрисовка, логика инструментов)
├── AnnotationDrawer.cs        # Рендеринг фигур и текста на холсте через Win2D
├── ClipboardMonitor.cs        # Фоновый Win32-мониторинг буфера обмена (WM_CLIPBOARDUPDATE)
├── HistoryManager.cs          # Стек отмены/повтора действий (Undo/Redo)
├── SettingsManager.cs         # Чтение и запись конфигурации пользователя (JSON)
├── ExportManager.cs           # Экспорт скриншотов в форматы PNG, JPEG, WebP
├── MainPage.xaml / .cs        # Основной экран интерфейса (панель инструментов, настройки)
├── MainWindow.xaml / .cs      # Корневое окно приложения и интеграция с треем
└── Screentation.csproj        # Файл конфигурации проекта
```

### Key Component Architecture

#### 1. Background clipboard interception (`ClipboardMonitor.cs`)
To reliably intercept screenshots in a minimized state, the Win32 function `AddClipboardFormatListener` is used, which registers the application window in the clipboard listening chain. When data changes, the window receives the `WM_CLIPBOARDUPDATE` system message.
* **Lock protection**: Because the snapshot source (eg Windows Clipboard) locks the buffer while it is being written, it pauses for 100 ms before reading the data and starts a cycle of 10 access attempts (`OpenClipboard`).
* **Deduplication**: Windows sends multiple updates in a row for different formats of the same data. To prevent duplicates, the DIB image header is read, reference pixels are calculated and checked against the previous image. If the content is identical and received within 2 seconds of the previous snapshot, it is cut off as a system duplicate.

#### 2. Interactive canvas (`AnnotationCanvas.cs`)
Inherits from `Grid` and contains `CanvasControl` (Win2D). 
* **Grid**: All annotation coordinates are stored in the original screenshot resolution. When rendering, a transformation matrix is ​​applied to the Win2D context (`Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_offsetX, _offsetY)`), due to which all shapes are scaled and moved smoothly and without loss of clarity.
* **Text editor**: When placing text on the canvas, the standard `TextBox` control is dynamically projected with the `Loaded` focus property enabled and aligned to the top-left edge of the canvas. Losing focus or pressing `Enter` bakes the text into a vector `TextElement`.

#### 3. Rendering shapes (`AnnotationDrawer.cs`)
Static class that performs low-level drawing of vector primitives on the `CanvasDrawingSession`. Blurring is implemented using the `GaussianBlurEffect` effect based on the original `CanvasBitmap` texture cache.

---

## ️ Build & Run

### Requirements
* OS: Windows 10 (version 1809 / build 17763) or newer.
* Visual Studio 2022 with **Application development for the Windows platform (UWP/WinUI)** or .NET 10 SDK installed.

###Build commands in the CLI
Build the project:
```bash
dotnet build
```

Launching the application:
```bash
dotnet run
```

Publishing (fully autonomous single-file release):
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```
The result will be saved in `Screentation/bin/Release/net10.0-windows10.0.26100.0/win-x64/publish/` and consists of:
- **`Screentation.exe`** (a single ~300 MB executable containing the .NET 10.0 Runtime, Windows App SDK, and all dependency DLLs)
- **`Assets/`** (folder containing icons and logos)
- **`Screentation.pdb`** (debug symbols, optional for distribution)