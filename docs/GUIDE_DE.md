# Screentation V2 Benutzerhandbuch — Screenshots unter Windows kommentieren und markieren

> [!NOTE]
> Screentation V2 ist ein professionelles Desktop-Dienstprogramm, das das Kommentieren, Markieren und Strukturieren von Screenshots automatisiert. Die Anwendung läuft im Hintergrund, fängt in die Zwischenablage kopierte Screenshots ab und ermöglicht das Hinzufügen von Vektormarkierungen, das Weichzeichnen vertraulicher Details, das Zuschneiden oder das Ausschneiden von Abschnitten (Slice Cut). Exportieren Sie Dateien in hoher Qualität als WebP, PNG oder JPEG ohne Überschreiben vorhandener Aufnahmen.

Screentation V2 bietet eine übersichtliche, native Desktop-Umgebung für technische Redakteure, Softwareentwickler und Support-Spezialisten, die Schritt-für-Schritt-Anleitungen erstellen. Da die Anwendung lautlos im System-Tray ausgeführt wird, optimiert sie den gesamten Arbeitsablauf vom Erfassen über das Editieren bis hin zum Exportieren von Bildern.

## Hauptfunktionen

### Überwachung der Zwischenablage & System-Tray
Screentation V2 nimmt selbst keine Screenshots auf. Stattdessen überwacht es Ihre Systemzwischenablage. Wenn Sie einen Screenshot mit Standard-Windows-Tools (wie `Win + Umschalt + S` oder `Druck`) kopieren, importiert Screentation diesen sofort. Das Schließen des Fensters minimiert die App in den System-Tray, sodass die Screenshot-Erfassung im Hintergrund aktiv bleibt.

### Erstklassige Vektor-Anmerkungswerkzeuge
*   **Pfeil**: Zeichnen Sie präzise Richtungspfeile, um auf bestimmte Schnittstellen-Elemente hinzuweisen.
*   **Rahmen (Rechteck)**: Platzieren Sie Rechtecke um wichtige Komponenten, optional mit halbtransparenter Hintergrundfüllung.
*   **Text**: Fügen Sie Textanmerkungen mit anpassbarer Schriftgröße und Farbe hinzu.
*   **Textmarker**: Verwenden Sie den halbtransparenten Marker-Pinsel, um Code-Snippets, UI-Texte oder Layout-Abschnitte hervorzuheben.
*   **Dynamische Schieberegler für Stärke & Farbe**: Passen Sie die Linienstärke des Stifts an oder wählen Sie benutzerdefinierte Farbschemata in Echtzeit; ausgewählte Elemente werden sofort aktualisiert.

### Datenschutz-Maskierung & Zensur
*   **Gaußscher Weichzeichner**: Wenden Sie eine Gaußsche Unschärfe an, um Passwörter, E-Mail-Adressen oder persönliche Daten unkenntlich zu machen.
*   **Klon-Stempel (Radierer)**: Blenden Sie Schnittstellen-Elemente aus, indem Sie Texturen von einem Bereich des Screenshots auf einen anderen kopieren.

### Intelligentes Zuschneiden & Segmentausschnitte (Slice Cut)
*   **Intelligentes Zuschneiden**: Schneiden Sie Ihren Screenshot auf eine beliebige Größe zu. Vorhandene Anmerkungselemente verschieben ihre Koordinaten automatisch, sodass sie perfekt auf das zugeschnittene Bild ausgerichtet bleiben.
*   **Ausschneiden von Abschnitten (Slice Cut)**: Löschen Sie einen horizontalen oder vertikalen Streifen aus dem Screenshot. Die verbleibenden Bildteile fügen sich nahtlos zusammen, und alle Anmerkungselemente darunter oder rechts davon verschieben sich automatisch.

### Automatisch inkrementierte Schritt-Markierungen
Platzieren Sie Schritt-Kreise, die sich automatisch erhöhen, um nummerierte Anleitungen zu erstellen.
*   Unterstützt drei Formate: Zahlen (`1, 2, 3...`), lateinische Großbuchstaben (`A, B, C...`) und lateinische Kleinbuchstaben (`a, b, c...`).
*   Die Kreisgröße skaliert dynamisch basierend auf der aktiven Zeichenstift-Stärke.
*   Sie können den Wert des nächsten Schritts jederzeit über das Einstellungsfenster ändern.

### Dateiexportformate (WebP, PNG, JPEG)
Speichern Sie Ihre kommentierten Screenshots in den Formaten **PNG**, **JPEG** oder komprimiertem **WebP**.
*   Das Ausgabeverzeichnis ist vollständig konfigurierbar.
*   Der Batch-Export („Alle speichern“) speichert alle Screenshots mit einem Klick.
*   Die Dateibenennung verhindert ein Überschreiben, indem sie den Listenindex des Screenshots verwendet (z. B. `Screentation_02.png`), wodurch Ihre Arbeit niemals verloren geht.

## Schnellstartanleitung

1.  **App öffnen**: Starten Sie Screentation V2. Sie sehen einen sauberen Arbeitsbereich. Die App wird auch im System-Tray initialisiert.
2.  **Screenshot aufnehmen**: Verwenden Sie Ihre Standard-Windows-Tastenkombination (z. B. `Win + Umschalt + S`), um einen Teil Ihres Bildschirms zu erfassen.
3.  **Kommentieren**: Screentation lädt das Bild automatisch aus der Zwischenablage. Klicken Sie auf ein beliebiges Werkzeug in der linken Symbolleiste (z. B. Pfeil, Textmarker, Schritt) und zeichnen Sie direkt auf die Leinwand.
4.  **Exportieren**: Klicken Sie auf **Aktives speichern** (oder drücken Sie `Strg + S`), um das aktuelle Bild zu speichern, oder auf **Alle speichern**, um alle Aufnahmen stapelweise in das zugewiesene Verzeichnis zu exportieren.

## Tipps & Tastaturkurzel

| Tastenkürzel | Aktion |
| --- | --- |
| `Strg + V` | Screenshot manuell aus der Zwischenablage einfügen |
| `Strg + Z` | Letzten Bearbeitungsschritt rückgängig machen (Undo) |
| `Strg + Y` / `Strg + Umschalt + Z` | Letzten rückgängig gemachten Schritt wiederholen (Redo) |
| `Strg + S` | Aktiven Screenshot speichern |
| `Strg + Umschalt + S` | Alle Screenshots speichern (Stapelspeicherung) |
| `Entfernen` / `Rücktaste` | Ausgewählte Anmerkung löschen |
| `Escape` | Aktives Zeichenwerkzeug abbrechen oder Elemente abwählen |
| `Enter` | Aktiven Zuschnitt / Segmentausschnitt anwenden |
| `Escape` (im Zuschneidemodus) | Aktiven Zuschnitt / Segmentausschnitt abbrechen |
| `1` | Rahmen-Werkzeug auswählen |
| `2` | Schritt-Werkzeug auswählen |
| `3` | Pfeil-Werkzeug auswählen |
| `4` | Unschärfe-Werkzeug auswählen |
| `5` | Stempel-Werkzeug (Radierer) auswählen |
| `Strg + Mausrad` | Leinwand vergrößern oder verkleinern (zentriert auf den Cursor) |
| `Mittlere Maustaste (Ziehen)` | Vergrößerte Leinwand verschieben |

## Häufig gestellte Fragen (FAQ)

### Windows Defender SmartScreen-Warnung beim Start?
Da das eigenständige Installationsprogramm nicht mit einem kostenpflichtigen Entwicklerzertifikat signiert ist, zeigt Windows beim ersten Start möglicherweise eine Warnung an. Klicken Sie auf **"Weitere Informationen"** und wählen Sie dann **"Trotzdem ausführen"**, um mit der Installation fortzufahren.

### Ausführen mehrerer Instanzen?
Screentation V2 ist eine Single-Instance-Anwendung. Ein erneuter Start fängt die Ausführung ab und bringt die bereits aktive Instanz automatisch in den Vordergrund, anstatt doppelte Symbole im Tray zu erzeugen.

### Warum sehen Schaltflächen manchmal weiß-auf-weiß aus?
Screentation V2 verfügt über einen dynamischen Ereignis-Listener zur Synchronisierung von Systemthemen. Wenn sich das Windows-Design ändert (z. B. Dunkel- zu Hellmodus), aktualisiert die Anwendung sofort alle Steuerelemente und Schriftarten, um eine optimale Lesbarkeit zu gewährleisten.
