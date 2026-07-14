# Screentation V2 - Project Context Documentation

This document is intended for architects and developers. It describes the internal architecture, key technical solutions, fixed critical bugs with their workarounds, and the project roadmap.

---

## 1. Project Core & Stack

**Screentation** is a desktop Windows application for instantly capturing screenshots from the clipboard, annotating them using vector tools (steps, text, arrows, frames, blur, clone stamp), and exporting them in batch mode. The application is designed for quickly creating step-by-step guides and interface documentation.

### Technology Stack

* **Runtime**: .NET 10.0 (TargetFramework: `net10.0-windows10.0.26100.0`, TargetPlatformMinVersion: `10.0.17763.0`)
* **Programming Language**: C# 13
* **User Interface**: WinUI 3 (Windows App SDK v2.2.0)
* **Build Type**: Unpackaged (`<WindowsPackageType>None</WindowsPackageType>`), Self-Contained / Single-File (one autonomous `.exe` containing the .NET Runtime and dependency DLLs, plus the copied `Assets` folder and embedded localized resources for `en-US`, `ru-RU`, `de-DE`)
* **Graphics & Rendering**: Microsoft.Graphics.Win2D v1.4.0 (hardware-accelerated annotation rendering on the canvas)
* **System Tray**: Native integration using the Win32 API (`Shell_NotifyIcon`, WNDPROC subclassing, and native `TrackPopupMenu` in `TrayManager.cs`) to ensure reliable tray menu behavior and process termination.
* **Build Tools**: Microsoft.Windows.SDK.BuildTools `10.0.28000.1839`, Microsoft.Windows.SDK.BuildTools.WinApp `0.3.2`
* **Database**: None. User settings are stored locally in JSON format (`settings.json`) under `AppData/Local/Screentation` using `System.Text.Json`.

---

## 2. Architecture & Design Patterns

### Directory and File Structure

```
Screentation/
├── Assets/                 # App icons, splash screens, and graphical assets
├── Properties/             # Launch and publish profiles
├── AnnotationCanvas.cs     # Interactive canvas logic (mouse events, scaling, drawing tools)
├── AnnotationDrawer.cs     # Win2D vector primitives rendering
├── ClipboardMonitor.cs     # Low-level Win32 clipboard monitoring (WM_CLIPBOARDUPDATE)
├── ExportManager.cs        # High-resolution batch rendering and file export
├── HistoryManager.cs       # Undo/Redo stack for screenshot sessions
├── ImageHelper.cs          # SoftwareBitmap scaling and format conversion
├── SettingsManager.cs      # User settings load/save (JSON)
├── Models.cs               # Domain models (Rect, Arrow, Step, Text, Blur, Eraser)
├── MainWindow.xaml/.cs     # Root window, custom title bar, and tray setup
├── MainPage.xaml/.cs       # Main UI screen (sessions sidebar, settings, canvas)
├── TrayManager.cs          # Native Win32 tray icon management and WNDPROC hook (P/Invoke)
├── app.manifest            # Application manifest (privileges, DPI-awareness)
└── Screentation.csproj     # MSBuild configuration file
```

### Architectural Patterns

1. **Separation of Concerns**:
   * **Domain Model Layer**: All annotation elements inherit from the abstract `AnnotationElement` class (`Models.cs`). They store geometry and colors in the original screenshot coordinates (in pixels).
   * **Rendering Layer**: `AnnotationDrawer` contains pure static methods to draw primitives on a `CanvasDrawingSession` Win2D context. It does not track selection states or zoom factor.
   * **UI Input Controller Layer**: `AnnotationCanvas` manages canvas scale and translation, converts mouse coordinates to/from original image coordinates, and dynamically places interactive UI controls (like a `TextBox` for text input).
2. **State Management**:
   * The current active screenshot and its annotations are encapsulated in `ScreenshotSession`.
   * The annotation collection is tracked via an `ObservableCollection<AnnotationElement>`, to which the canvas subscribes to trigger redraws (`Invalidate()`).
   * Annotations are stored in **original screenshot coordinates**. Zoom and Pan transformations are applied globally to the Win2D drawing context matrix before rendering:
     ```csharp
     ds.Transform = Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_offsetX, _offsetY);
     ```

---

## 3. Main Application Logic

* **Background Clipboard Monitoring**: The app registers its window as a clipboard listener (`AddClipboardFormatListener`). When minimized to the tray, clipboard interception continues to capture screenshots in the background.
* **Interactive Annotation Tools**:
  * *Rect*: Semi-transparent filled or hollow rectangular frames.
  * *Arrow*: Dynamic arrow head rendering based on start and end points.
  * *Blur*: Gaussian blur effect applied directly to the cropped region.
  * *Eraser (Stamp)*: Clones pixels from a source region to a target region to mask sensitive information.
  * *Step*: Auto-incremented sequence numbers supporting numeric, lowercase, and uppercase alphabetical formats.
  * *Text*: Vector text blocks with customizable font sizes.
* **Canvas Zoom & Pan**: Mouse wheel zoom with `Ctrl` (centered around the cursor) and middle mouse button drag-to-pan, synchronized with the right-hand panel slider.
* **Smart Crop**: Cropping shifts all existing annotations relative to the new crop frame so they remain aligned.
* **Settings**: Persistent configuration (saved across launches) for export directory, quick colors, default formats, and compression quality.

---

## 4. Key Decisions & Workarounds

> [!IMPORTANT]
> The workarounds detailed below were implemented to bypass OS-level and framework limitations. Modifying these components without understanding the underlying design will break core features.

### 4.1. Bypassing WinRT Clipboard Restrictions in Background Mode
* **Problem**: Using the standard WinRT `Clipboard.GetContent()` in background mode (when the app is minimized to the tray and lacks window focus) throws an access denied exception.
* **Workaround**: Directly access the low-level Win32 clipboard API to read the raw `CF_DIB` structure.
```csharp
if (!OpenClipboard(IntPtr.Zero)) return (null, false);
try {
    IntPtr hDIB = GetClipboardData(CF_DIB);
    if (hDIB == IntPtr.Zero) return (null, false);
    IntPtr pDIB = GlobalLock(hDIB);
    // ...Parse BITMAPINFOHEADER and copy pixel data...
} finally {
    CloseClipboard();
}
```

### 4.2. Handling Clipboard Lock Race Conditions
* **Problem**: System tools (like Windows Snipping Tool) lock the clipboard while writing data. Reading immediately upon receiving `WM_CLIPBOARDUPDATE` results in a locked clipboard error.
* **Workaround**: Implement an initial 100 ms delay followed by up to 10 retries at 50 ms intervals.
```csharp
await Task.Delay(100);
for (int i = 0; i < 10; i++) {
    var result = GetClipboardImageWin32();
    if (result.isDuplicate) break;
    bitmap = result.bitmap;
    if (bitmap != null) break;
    await Task.Delay(50);
}
```

### 4.3. Time-Windowed Duplicate Detection
* **Problem**: Certain apps copy screenshot data in multiple formats simultaneously, triggering multiple `WM_CLIPBOARDUPDATE` events. However, users might want to take identical screenshots intentionally within a short timeframe.
* **Workaround**: Store a fingerprint of the captured screenshot (dimensions + sample pixels). Filter out duplicates only if they occur within 2.0 seconds of the previous screenshot.

### 4.4. TextBox Projection on Win2D Canvas
* **Problem**: Win2D canvases do not natively support child UI elements like interactive text inputs.
* **Workaround**: Dynamically overlay a standard `TextBox` on `AnnotationCanvas` (which inherits from `Grid`), matching the current canvas zoom scale and translation offset. Pressing `Enter` or losing focus converts the text into a vector `TextElement`.

### 4.5. Annotation Shifting on Cropping
* **Problem**: Cropping reduces the size of the canvas, which shifts vector coordinates relative to the cropped background image.
* **Workaround**: Calculate the crop offset vector and shift all annotation elements in the opposite direction.
```csharp
Vector2 shift = new Vector2(-(float)cropRect.X, -(float)cropRect.Y);
foreach (var element in Session.Annotations) {
    element.Move(shift);
}
```

### 4.6. Replacing H.NotifyIcon with Native Win32 Tray Management
* **Problem**: The third-party library `H.NotifyIcon` uses a `MenuFlyout` which resides outside of the main WinUI 3 visual tree. This causes event bindings (`Click` and `Command`) to fail silently due to garbage collection or focus loss, leaving the process orphaned in the background when the user attempts to exit.
* **Workaround**: Replaced `H.NotifyIcon` with a native P/Invoke class `TrayManager.cs` using `Shell_NotifyIcon`. We subclass the window procedure using `SetWindowLongPtr` (keeping a strong reference to the delegate to prevent GC collection) and handle native click events by calling `Environment.Exit(0)` to cleanly terminate all background threads.

---

## 5. Environment & Requirements

* **Environment Variables**: None required.
* **External Services**: None required.
* **Build & Run**:
  * Run project:
    ```bash
    dotnet run --project Screentation/Screentation.csproj
    ```
  * Publish autonomous single-file release:
    ```bash
    dotnet publish Screentation/Screentation.csproj -c Release -r win-x64 --self-contained true
    ```
    * The output executables will be saved in: `Screentation/bin/Release/net10.0-windows10.0.26100.0/win-x64/publish/`
    * The directory will contain `Screentation.exe` (~300 MB), `Screentation.pdb` (debug symbols), and the `Assets/` directory. All localized resource files are embedded within the single executable.

---

## 6. Technical Debt & Known Issues

1. **WIC WebP Dependency**:
   * *Problem*: WebP export in `ExportManager.cs` utilizes the Windows Imaging Component (WIC) encoder. If the WebP Image Extension is missing in Windows, export fails.
   * *Workaround*: Fall back to PNG format instead of crashing.
2. **Text Hit-Testing Approximations**:
   * *Problem*: Selection hit-testing for text uses character-width approximations.
   * *Refactoring*: Replace with `CanvasTextLayout` in the future for pixel-perfect bounding box calculations.
3. **SoftwareBitmap Resource Management**:
   * *Problem*: `SoftwareBitmap` uses unmanaged memory, which can accumulate during long-running sessions.

---

## 7. Roadmap

- [x] **UI Localization**: Move string resources to `.resw` for RU, EN, and DE language support.
- [ ] **Export to PDF**: Add multi-page PDF generation from active sessions.
- [ ] **Text Hit-Testing Layout**: Refactor `TextElement` hit-testing using `CanvasTextLayout`.
- [ ] **Export Notification Banner**: Add a WinUI `InfoBar` on the main page to alert the user of export errors or status.
