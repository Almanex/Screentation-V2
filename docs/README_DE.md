# Screentation

*Eine professionelle Desktop-Anwendung zum Aufnehmen, Kommentieren und Nummerieren von Schritten auf Screenshots unter Windows.*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-lightgrey.svg)]()
[![Framework: .NET 10.0](https://img.shields.io/badge/Framework-.NET%2010.0-blueviolet.svg)]()
[![UI: WinUI 3](https://img.shields.io/badge/UI-WinUI%203-blue.svg)]()
[![Share](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FScreentation-V2)](https://twitter.com/intent/tweet?text=Check%20out%20Screentation%20-%20a%20native%20Windows%20screenshot%20annotation%20tool%20built%20with%20WinUI3%20and%20Win2D!&url=https%3A%2F%2Fgithub.com%2FAlmanex%2FScreentation-V2)

Screentation ist eine native Windows-Anwendung zum schnellen Erstellen, Bearbeiten und Kommentieren von Screenshots. Geschrieben in C# unter Verwendung von WinUI 3 (Windows App SDK) und der Grafikbibliothek Win2D für Hardware-beschleunigtes Rendering.

Mit der Anwendung können Sie Screenshots sofort aus der Zwischenablage importieren (auch im Hintergrund), Pfeile, Rahmen und Text hinzufügen, sensible Daten unkenntlich machen, Bilder zuschneiden und Anweisungsschritte automatisch nummerieren (sowohl mit Zahlen als auch mit lateinischen Buchstaben).

Detaillierte Informationen zur Verwendung der Anwendung finden Sie im [Benutzerhandbuch](GUIDE_DE.md).

---

## Hauptmerkmale

* **Automatische Aufnahme**: Die Hintergrundüberwachung der Zwischenablage importiert neue Screenshots automatisch beim Drücken von PrintScreen (auch bei minimiertem Fenster).
* **Anmerkungstools**: Rahmen (mit/ohne Füllung), Richtungspfeile, Gaußsche Unschärfe für sensible Daten, Kopierstempel (Eraser) und Textblöcke.
* **Automatische Schrittnummerierung**: Marker mit automatischer Erhöhung unterstützen numerische und alphabetische Formate.
* **Intelligentes Zuschneiden**: Zuschneiden des Bildes mit automatischer Anpassung und Verschiebung der Koordinaten aller zuvor gezeichneten Elemente.
* **Lokalisierung**: Vollständige Übersetzung der Benutzeroberfläche in drei Sprachen (Russisch, Englisch, Deutsch) mit automatischer Erkennung der System-Sprache.

---

## Technologie-Stack

| Komponente / Abhängigkeit | Technologie | Zweck / Beschreibung |
| --- | --- | --- |
| Plattform | .NET 10.0 | Ziel-Framework net10.0-windows |
| Benutzeroberfläche (UI) | WinUI 3 | Windows App SDK (v2.2.0) |
| Grafik-Engine | Win2D | Hardware-beschleunigtes Zeichnen von Formen (v1.4.0) |
| System-Tray | Win32 API | Native Integration von Shell_NotifyIcon und WNDPROC Subclassing |

---

## Erste Schritte

### Voraussetzungen

* Betriebssystem: Windows 10 (Version 1809 / Build 17763) oder neuer.
* Entwicklungsumgebung: Visual Studio 2022 mit installierter Windows-Entwicklungsauslastung (UWP/WinUI) oder .NET 10.0 SDK.

### Erstellen und Ausführen

Projekt kompilieren:
```bash
dotnet build
```

Anwendung starten:
```bash
dotnet run
```

---

## Tests

Um die Korrektheit des Codes zu überprüfen, führen Sie das Projekt im Debug-Modus aus:
```bash
dotnet build -c Debug
```
Dadurch wird das Projekt auf Kompilierungsfehler und Warnungen überprüft.

---

## Bereitstellung

Veröffentlichung als eigenständiges Release (Single-File / Self-Contained):
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```
Das Ergebnis wird im Ordner `publish/` gespeichert und umfasst:
* `Screentation.exe` (eigenständige ausführbare Datei ~300 MB, enthält die .NET-Laufzeitumgebung und WinUI 3-Bibliotheken).
* Den Ordner `Assets/` (Grafikressourcen der Anwendung).

### Windows Defender SmartScreen-Warnung
Da die ausführbare Datei der Anwendung nicht mit einem digitalen Entwicklerzertifikat signiert ist, blockiert Windows Defender Smartscreen diese möglicherweise beim ersten Start.
So starten Sie die Anwendung:
1. Klicken Sie auf den Link **Weitere Informationen**.
2. Klicken Sie auf die Schaltfläche **Trotzdem ausführen**.

---

## Mitwirken

Wenn Sie zum Projekt beitragen möchten, erstellen Sie bitte einen Pull Request oder eröffnen Sie ein Issue mit der Beschreibung der vorgeschlagenen Verbesserungen.

---

## Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert. Details finden Sie in der Datei [LICENSE](../LICENSE).
