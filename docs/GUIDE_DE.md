# Screentation Benutzerhandbuch

Diese Anleitung beschreibt die Verwendung von Screentation zum Aufnehmen, Kommentieren und Verwalten von Screenshots.

## Hauptmerkmale

1. **Automatische Aufnahme**
   * Wenn die Anwendung im Hintergrund ausgeführt wird (auch wenn sie in den System-Tray minimiert ist), importiert das Drücken der Taste `PrintScreen` oder das Erstellen eines Screenshots mit dem Windows Snipping Tool den Screenshot automatisch in die aktive Screentation-Sitzung.

2. **Anmerkungstools**
   * **Auswählen**: Verschieben und Ändern der Größe von Anmerkungen.
   * **Rahmen (Rect)**: Hinzufügen von rechteckigen Rahmen, optional mit halbtransparenter Füllung.
   * **Pfeil**: Zeichnen von Richtungspfeilen zur Hervorhebung von Schnittstellen-Elementen.
   * **Unschärfe**: Vertrauliche Details mit Gaußscher Unschärfe maskieren.
   * **Radiergummi (Klon-Stempel)**: Ausblenden von UI-Elementen durch Kopieren von Texturen aus einem anderen Bereich.
   * **Text**: Textanmerkungen mit anpassbaren Schriftgrößen und Farben hinzufügen.
   * **Schritt**: Automatisch inkrementierte Schrittnummern zeichnen.

3. **Intelligentes Zuschneiden**
   * Schneiden Sie den Screenshot auf einen beliebigen rechteckigen Bereich zu. Bestätigte Zuschnitte passen vorhandene Anmerkungselemente automatisch an, sodass sie relativ zu den neuen Bildgrenzen an ihrer Position bleiben.

4. **Zoom & Schwenk**
   * Halten Sie die `Strg`-Taste gedrückt und scrollen Sie das Mausrad, um auf die Cursorposition zu zoomen, oder verwenden Sie den Zoom-Schieberegler im Einstellungsbereich.
   * Verschieben Sie die vergrößerte Leinwand, indem Sie die mittlere Maustaste (Rad) gedrückt halten und ziehen.
   * Klicken Sie auf "Zurücksetzen", um das Bild an das Fenster anzupassen.

5. **Schrittnummerierungsformate**
   * Unterstützt Zahlen (`1, 2, 3...`), Großbuchstaben (`A, B, C...`) und Kleinbuchstaben (`a, b, c...`).
   * Der Startwert der Sequenz kann über das Steuerelement "Nächster Schritt" angepasst werden.

6. **Farbauswahl**
   * Schnellauswahl-Farbvoreinstellungen sind verfügbar.
   * Eigene Farben können über den spektralen ColorPicker ausgewählt werden. Benutzerdefinierte Farben bleiben über Anwendungsneustarts hinweg erhalten.

## Tastaturkürzel

| Tastenkürzel | Aktion |
| --- | --- |
| `Strg + V` | Screenshot manuell aus der Zwischenablage einfügen |
| `Strg + Z` | Letzte Aktion rückgängig machen |
| `Strg + Y` oder `Strg + Umschalt + Z` | Letzte rückgängig gemachte Aktion wiederholen |
| `Strg + S` | Aktiven Screenshot auf Festplatte speichern |
| `Strg + Umschalt + S` | Alle Screenshots speichern (Stapelspeicherung) |
| `Löschen` oder `Rücktaste` | Ausgewähltes Anmerkungselement löschen |
| `Escape` | Auswahl zurücksetzen oder aktives Zeichenwerkzeug verlassen |
| `Enter` (beim Zuschneiden) | Zuschneiden bestätigen und anwenden |
| `Escape` (beim Zuschneiden) | Zuschneiden abbrechen |
| `1` | Rahmen-Werkzeug auswählen |
| `2` | Schritt-Werkzeug auswählen |
| `3` | Pfeil-Werkzeug auswählen |
| `4` | Unschärfe-Werkzeug auswählen |
| `5` | Stempel-Werkzeug (Radiergummi) auswählen |

## System-Tray-Integration

* Das Schließen des Hauptfensters (über die Schaltfläche `X`) beendet die Anwendung nicht. Stattdessen wird sie im System-Tray ausgeblendet, um die Hintergrundüberwachung der Zwischenablage fortzusetzen.
* Doppelklicken Sie auf das System-Tray-Symbol, oder klicken Sie mit der rechten Maustaste auf das Symbol und wählen Sie **Screentation öffnen**, um das Fenster wiederherzustellen.
* Klicken Sie mit der rechten Maustaste auf das System-Tray-Symbol und wählen Sie **Beenden**, um die Anwendung vollständig zu beenden.
