using System;
using System.Windows.Forms;

namespace AutoTimeTracker
{
    // Einstiegspunkt der Windows-Forms-Anwendung.
    internal static class Program
    {
        // [STAThread] ist für Windows Forms nötig, damit UI-Komponenten wie Dialoge korrekt laufen.
        [STAThread]
        static void Main()
        {
            // Initialisiert globale Windows-Forms-Einstellungen wie DPI, Fonts und Visual Styles.
            ApplicationConfiguration.Initialize();

            // Stellt sicher, dass die SQLite-Datenbank und die benötigte Tabelle existieren,
            // bevor das Hauptfenster geöffnet wird.
            DatabaseHelper.InitializeDatabase();

            // Startet die Anwendung mit dem Hauptfenster.
            Application.Run(new MainForm());
        }
    }
}
