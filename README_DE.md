[ English ](README.md) • [ Русский ](README_RU.md) • [ Deutsch ](README_DE.md)

# Screentation

**Screentation** ist eine native Windows-Anwendung zum schnellen Erstellen, Bearbeiten und Kommentieren von Screenshots. Geschrieben in C# unter Verwendung der modernen Schnittstelle **WinUI 3 (Windows App SDK)** und der Grafikbibliothek **Win2D** für Hardware-Rendering.

Mit der Anwendung können Sie sofort Screenshots aus der Zwischenablage aufnehmen (auch im minimierten Zustand), Pfeile, Rahmen und Text darauf anwenden, vertrauliche Daten verwischen, Bilder zuschneiden und Anweisungsschritte automatisch nummerieren (sowohl in Zahlen als auch in lateinischen Buchstaben).

---

## Benutzerhandbuch

### Hauptmerkmale
1. **Automatische Aufnahme**: Wenn die Anwendung geöffnet oder einfach in der Taskleiste minimiert ist und Sie auf „PrintScreen“ klicken (oder ein Foto mit dem Windows Snipping Tool aufnehmen), wird der Screenshot automatisch zur Liste auf der linken Seite hinzugefügt.
2. **Anmerkungstools**:
 - **Auswählen**: Angewandte Elemente auswählen, verschieben und in der Größe ändern.
 - **Rahmen (Rect)**: Erstellt rechteckige Bereiche (optional mit durchscheinender Füllung).
 - **Pfeil**: Richtungspfeile erstellen.
 - **Unschärfe**: Gaußscher Unschärfeeffekt zum Ausblenden vertraulicher Informationen.
 - **Radiergummi**: Textur kopieren und übertragen (Klonbereich), um Oberflächenelemente auszublenden.
 - **Text**: Geben Sie benutzerdefinierten Text auf der Leinwand ein und haben Sie die Möglichkeit, Schriftgröße und -farbe zu ändern.
 - **Schritt**: Automatische Abfolge von Schritten.
3. **Intelligentes Zuschneiden**:
 - Ermöglicht das Zuschneiden des Screenshots auf einen beliebigen rechteckigen Bereich.
 - Wenn das Zuschneiden bestätigt wird, werden alle zuvor gezeichneten Elemente auf den entsprechenden Vektor verschoben und bleiben genau an ihren Plätzen relativ zum zugeschnittenen Rahmen.
4. **Zoom & Schwenk**:
 - Die Skalierung erfolgt durch Halten der **`Strg`-Taste + Mausrad** (fokussiert auf den Cursor) oder durch Ziehen des **Skalierungsschiebereglers** im rechten Bereich.
 - Das Bewegen auf der vergrößerten Leinwand erfolgt durch **Halten der mittleren Maustaste (Rad)** und Ziehen.
 - Mit der Schaltfläche **Zurücksetzen** kehrt die Skala in den Modus „An Fenster anpassen“ zurück.
5. **Nummerierung der Schritte**:
 - Unterstützt 3 Formate: Zahlen (`1, 2, 3...`), lateinische Großbuchstaben (`A, B... Z, AA...`), lateinische Kleinbuchstaben (`a, b... z, aa...`).
 - Im Feld „Nächster Schritt“ können Sie die Startnummer festlegen oder den aktuellen Index ändern.
6. **Farbauswahl**:
 - Fertige Schnellfarben verfügbar.
 - Die Schaltfläche **Farbe auswählen...** öffnet die Spektralpalette (ColorPicker) zur Auswahl eines beliebigen benutzerdefinierten Farbtons. Die ausgewählte Farbe bleibt zwischen Anwendungsneustarts erhalten.

### Tastaturkürzel
| Schlüssel | Aktion |
| :--- | :--- |
| `Strg + V` | Screenshot manuell aus der Zwischenablage einfügen |
| `Strg + Z` | Letzte Aktion rückgängig machen (Rückgängig) |
| „Strg + Y“ / „Strg + Umschalt + Z“ | Eine rückgängig gemachte Aktion wiederherstellen (Redo) |
| `Strg + S` | Aktiven Screenshot auf Festplatte speichern |
| `Strg + Umschalt + S` | Alle Screenshots speichern (Stapelspeicherung) |
| „Löschen“ / „Rücktaste“ | Ausgewähltes Anmerkungselement löschen |
| „Flucht“ | Aktuellen Modus verlassen / Auswahl zurücksetzen |
| `Enter` *(im Frame-Modus)* | Zuschneiden anwenden |
| `Escape` *(im Frame-Modus)* | Zuschneiden rückgängig machen |
| „1“, „2“, „3“, „4“, „5“ | Schnelle Auswahl an Werkzeugen: Rahmen, Schritt, Pfeil, Unschärfe, Stempel |

---

## Entwicklerhandbuch

### Technologie-Stack
* **Plattform**: .NET 10.0, Windows 10/11
* **UI-Framework**: WinUI 3 (Windows App SDK 2.2.0)
* **Grafik**: Win2D (Microsoft.Graphics.Win2D 1.4.0) Hardware-2D-Rendering basierend auf Direct2D
* **Systemintegration**: Unterklassen von Windows (Comctl32), um Win32-Zwischenablageereignisse abzufangen

### Projektstruktur
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

### Schlüsselkomponentenarchitektur

#### 1. Abfangen der Zwischenablage im Hintergrund („ClipboardMonitor.cs“)
Um Screenshots im minimierten Zustand zuverlässig abzufangen, wird die Win32-Funktion „AddClipboardFormatListener“ verwendet, die das Anwendungsfenster in der Abhörkette der Zwischenablage registriert. Wenn sich Daten ändern, empfängt das Fenster die Systemmeldung „WM_CLIPBOARDUPDATE“.
* **Sperrschutz**: Da die Snapshot-Quelle (z. B. Windows-Zwischenablage) den Puffer während des Schreibens sperrt, pausiert sie vor dem Lesen der Daten 100 ms und startet einen Zyklus von 10 Zugriffsversuchen („OpenClipboard“).
* **Deduplizierung**: Windows sendet mehrere Updates hintereinander für verschiedene Formate derselben Daten. Um Duplikate zu vermeiden, wird der DIB-Bildkopf gelesen, Referenzpixel berechnet und mit dem vorherigen Bild verglichen. Wenn der Inhalt identisch ist und innerhalb von 2 Sekunden nach dem vorherigen Schnappschuss empfangen wird, wird er als Systemduplikat abgeschnitten.

#### 2. Interaktive Leinwand (`AnnotationCanvas.cs`)
Erbt von „Grid“ und enthält „CanvasControl“ (Win2D). 
* **Raster**: Alle Anmerkungskoordinaten werden in der ursprünglichen Screenshot-Auflösung gespeichert. Beim Rendern wird eine Transformationsmatrix auf den Win2D-Kontext angewendet („Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_offsetX, _offsetY)“), wodurch alle Formen reibungslos und ohne Verlust der Klarheit skaliert und verschoben werden.
* **Texteditor**: Beim Platzieren von Text auf der Leinwand wird das Standardsteuerelement „TextBox“ dynamisch mit aktivierter Fokuseigenschaft „Geladen“ projiziert und am oberen linken Rand der Leinwand ausgerichtet. Wenn Sie den Fokus verlieren oder die Eingabetaste drücken, wird der Text in einen Vektor „TextElement“ umgewandelt.

#### 3. Formen rendern (`AnnotationDrawer.cs`)
Statische Klasse, die das Zeichnen von Vektorprimitiven auf niedriger Ebene in der „CanvasDrawingSession“ durchführt. Die Unschärfe wird mithilfe des Effekts „GaussianBlurEffect“ implementiert, der auf dem ursprünglichen Textur-Cache „CanvasBitmap“ basiert.

---

##️ Bauen und ausführen

### Anforderungen
* Betriebssystem: Windows 10 (Version 1809 / Build 17763) oder neuer.
* Visual Studio 2022 mit installierter **Anwendungsentwicklung für die Windows-Plattform (UWP/WinUI)** oder .NET 10 SDK.

###Befehle in der CLI erstellen
Erstellen Sie das Projekt:
```bash
dotnet build
```

Starten der Anwendung:
```bash
dotnet run
```

Veröffentlichung (vollständig autonome Single-File-Version):
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```
Das Ergebnis wird im Verzeichnis `Screentation/bin/Release/net10.0-windows10.0.26100.0/win-x64/publish/` gespeichert und besteht aus:
- **`Screentation.exe`** (eine einzelne ~300 MB große ausführbare Datei, die die .NET 10.0-Laufzeitumgebung, das Windows App SDK und alle abhängigen DLLs enthält)
- **`Assets/`** (Ordner mit Symbolen und Logos)
- **`Screentation.pdb`** (Debugschnittstellensymbole, optional für den Vertrieb)