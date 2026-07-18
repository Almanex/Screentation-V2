<p align="center">
  <img src="/screenshots/screentation_01.png" width="98%" alt="Screentation is a native Windows application" />
</p>

[README_RU](docs/README_RU.md) | [README_DE](docs/README_DE.md) | [README_EN](README.md) | [GUIDE_RU](docs/GUIDE_RU.md) | [GUIDE_DE](docs/GUIDE_DE.md) | [GUIDE_EN](docs/GUIDE_EN.md)
# Screentation

*A professional screenshot capture, annotation, and step-sequencing desktop application for Windows.*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-lightgrey.svg)]()
[![Framework: .NET 10.0](https://img.shields.io/badge/Framework-.NET%2010.0-blueviolet.svg)]()
[![UI: WinUI 3](https://img.shields.io/badge/UI-WinUI%203-blue.svg)]()
[![Share](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FScreentation-V2)](https://twitter.com/intent/tweet?text=Check%20out%20Screentation%20-%20a%20native%20Windows%20screenshot%20annotation%20tool%20built%20with%20WinUI3%20and%20Win2D!&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FScreentation-V2)

Screentation is a native Windows application for editing and annotating screenshots. **Please note:** Screentation does not take screenshots itself. Instead, you create screenshots using standard Windows tools (such as `PrintScreen`, `Win+Shift+S`, etc.), and Screentation automatically detects and imports them from the clipboard for instant editing.

The application is written in C# using WinUI 3 (Windows App SDK) and the Win2D graphics library for hardware-accelerated rendering. It allows you to instantly import screenshots from the clipboard (even when minimized to the system tray), apply arrows, frames, markers, or text on top of them, blur sensitive data, crop images, remove slices, and automatically number instruction steps using both numbers and Latin letters.

The user interface is fully localized in **three languages**: English, Russian, and German.

For detailed usage instructions, please refer to the [User Guide](docs/GUIDE.md).

---

## Key Features

*   **Automatic Capture**: Background clipboard monitoring (even when minimized to tray) automatically detects and imports screenshots copied to the clipboard by standard Windows screenshot tools.
*   **Markup Tools**: Semi-transparent highlight marker, frames (with/without fills), directional arrows, Gaussian blur for sensitive details, clone stamp (Eraser), and text blocks.
*   **Auto-sequenced Steps**: Auto-incremented step markers supporting numeric and alphabetical formats, with circle radius scaling dynamically to drawing thickness.
*   **Smart Crop & Slice Cut**: Crop images or cut horizontal/vertical slices with automatic coordinate recalculation and snapping for all previously drawn annotation elements.
*   **Localization**: Fully localized user interface in 3 languages (English, Russian, German) with automatic system display language detection and command-line override support.
*   **WebP Export**: Support for exporting in high-quality WebP, PNG, or JPEG formats. Prevents file overwrite by naming files using list indices.
*   **Runtime Theme Synchronization**: Smooth adaptation of application layout, content dialogs, and button contrasts to system theme changes at runtime.

---

## Tech Stack

| Layer / Component | Technology | Details / Purpose |
| --- | --- | --- |
| Platform | .NET 10.0 | net10.0-windows target framework |
| UI Framework | WinUI 3 | Windows App SDK (v2.2.0) |
| Graphics Rendering | Win2D | Hardware-accelerated 2D drawing (v1.4.0) |
| Image Processing | SixLabors.ImageSharp | Managed WebP image encoding (v2.1.9) |
| System Tray | Win32 API | Native Shell_NotifyIcon & subclassed WNDPROC |

---

## Getting Started

### Prerequisites

* OS: Windows 10 (version 1809 / build 17763) or newer.
* SDK: Visual Studio 2022 with **Windows application development (UWP/WinUI)** workload, or .NET 10.0 SDK.

### Installation & Running

Build the project:
```bash
dotnet build
```

Run the application:
```bash
dotnet run
```

---

## Running the Tests

To verify code correctness, compile the solution in Debug mode to check for any warnings or compilation errors:
```bash
dotnet build -c Debug
```

---

## Deployment

### 1. Standalone Installer (Inno Setup)
Publish a standalone release (Single-File / Self-Contained):
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishPackaged=false
```
Then compile the classic installer using Inno Setup compiler (`setup.iss`):
```bash
& "ISCC.exe" setup.iss
```

### 2. MSIX Package (Windows Store & Sideloading)
Clean previous build caches:
```bash
dotnet clean -c Release -r win-x64 -p:PublishPackaged=true -p:Platform=x64
```
Publish and compile a signed MSIX package:
```bash
dotnet publish -c Release -r win-x64 -p:PublishPackaged=true -p:GenerateAppxPackageOnBuild=true -p:Platform=x64 -p:AppxPackageSigningEnabled=true
```

### Windows Defender SmartScreen Warning
Because the standalone setup executable is unsigned, Windows Defender SmartScreen may display a warning when launched for the first time.
To run the application:
1. Click the **More info** link.
2. Click the **Run anyway** button.

---

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any suggestions or improvements.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
