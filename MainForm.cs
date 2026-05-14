using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AutoTimeTracker
{
    // Hauptfenster der Anwendung.
    // Diese Klasse erstellt die Oberfläche per Code und verbindet sie mit der Datenbanklogik.
    public partial class MainForm : Form
    {
        // Tabelle für die Anzeige aller Zeiteinträge.
        private DataGridView dgvLogs;

        // Eingabefeld und Button zum Starten eines neuen Tasks.
        private TextBox txtTask;
        private Button btnStartTask;
        private Button btnEndTask;

        // Statusanzeige für die aktive Aufgabe und deren aktuelle Laufzeit.
        private Label lblCurrentTask;
        private Label lblTimer;

        // Ältere Filter-Felder; werden aktuell nicht mehr in InitializeUI verwendet.
        private TextBox txtFilterTask;
        private DateTimePicker dtFilterDate;
        private Button btnFilter;

        // Anzeige der aufsummierten Zeit für den aktuellen Filter.
        private Label lblTotalTime;

        // Aktualisiert die Laufzeitanzeige der aktiven Aufgabe jede Sekunde.
        private System.Windows.Forms.Timer uiTimer;
        private DateTime taskStartTime;

        // Aktuelle Filter-Steuerelemente: mehrere Tasks per Komma und Zeitraum von/bis.
        private TextBox txtFilterTasks;
        private DateTimePicker dtFrom;
        private DateTimePicker dtTo;

        // Aktionen für Filter anwenden, Eintrag bearbeiten und Eintrag löschen.
        private Button btnApplyFilter;
        private Button btnEditLog;
        private Button btnDeleteLog;
        private Button btnDeleteFilteredLogs;

        // Konstruktor: Oberfläche bauen, Datenbank vorbereiten, Daten laden und Timer starten.
        public MainForm()
        {
            //InitializeComponent();
            InitializeUI();

            DatabaseHelper.InitializeDatabase();
            LoadData();
            StartTimer();
        }

        // Erstellt alle UI-Elemente manuell und platziert sie im Formular.
        private void InitializeUI()
        {
            // FORM STYLE
            Text = "Auto Time Tracker Pro";
            Width = 950;
            Height = 720;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);
            Font = new Font("Segoe UI", 10);

            // =====================
            // TASK INPUT
            // =====================
            // Hier gibt der Benutzer den Namen der Aufgabe ein, die gestartet werden soll.
            txtTask = new TextBox
            {
                Left = 20,
                Top = 20,
                Width = 300,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnStartTask = new Button
            {
                Text = "Start Task",
                Left = 340,
                Top = 18,
                Width = 120,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            // Klick auf den Start-Button beendet eine laufende Aufgabe und startet die neue.
            btnStartTask.FlatAppearance.BorderSize = 0;
            btnStartTask.Click += BtnStartTask_Click;

            btnEndTask = new Button
            {
                Text = "Beenden",
                Left = 475,
                Top = 18,
                Width = 120,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White
            };
            // Schließt den aktuell laufenden Task und berechnet danach die Gesamtzeit neu.
            btnEndTask.FlatAppearance.BorderSize = 0;
            btnEndTask.Click += BtnEndTask_Click;

            // =====================
            // STATUS LABELS
            // =====================
            // Zeigt den Namen der gerade aktiven Aufgabe.
            lblCurrentTask = new Label
            {
                Left = 20,
                Top = 60,
                Width = 600,
                ForeColor = Color.Cyan
            };

            // Zeigt die sekundengenau aktualisierte Laufzeit der aktiven Aufgabe.
            lblTimer = new Label
            {
                Left = 20,
                Top = 85,
                Width = 300,
                ForeColor = Color.LightGreen
            };

            // =====================
            // GRID
            // =====================
            // Das DataGridView ist schreibgeschützt; Änderungen laufen über den Bearbeiten-Dialog.
            dgvLogs = new DataGridView
            {
                Left = 20,
                Top = 130,
                Width = 880,
                Height = 350,

                BackgroundColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.White,

                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,

                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            };

            dgvLogs.BackgroundColor = Color.FromArgb(25, 25, 25);
            dgvLogs.BorderStyle = BorderStyle.None;

            dgvLogs.EnableHeadersVisualStyles = false;

            // Dunkles Tabellen-Design passend zum Formular.
            dgvLogs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgvLogs.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            dgvLogs.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            dgvLogs.DefaultCellStyle.ForeColor = Color.White;

            dgvLogs.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);

            dgvLogs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dgvLogs.DefaultCellStyle.SelectionForeColor = Color.White;

            dgvLogs.GridColor = Color.FromArgb(60, 60, 60);

            dgvLogs.RowHeadersVisible = false;
            dgvLogs.AutoGenerateColumns = true;

            // =====================
            // FILTER UI
            // =====================
            // Kommagetrennte Task-Suchbegriffe, z. B. "Projekt A, Meeting".
            txtFilterTasks = new TextBox
            {
                Left = 20,
                Top = 500,
                Width = 250,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };

            // Anfang des Datumsbereichs.
            dtFrom = new DateTimePicker
            {
                Left = 290,
                Top = 500,
                Width = 140,
                Format = DateTimePickerFormat.Short
            };

            // Ende des Datumsbereichs.
            dtTo = new DateTimePicker
            {
                Left = 440,
                Top = 500,
                Width = 140,
                Format = DateTimePickerFormat.Short
            };

            btnApplyFilter = new Button
            {
                Text = "Apply Filter",
                Left = 600,
                Top = 494,
                Width = 145,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };

            // Verbindet den Button mit der Filterlogik.
            btnApplyFilter.FlatAppearance.BorderSize = 0;
            btnApplyFilter.Click += BtnApplyFilter_Click;

            // Öffnet für den ausgewählten Tabelleneintrag einen Bearbeiten-Dialog.
            btnEditLog = new Button
            {
                Text = "Bearbeiten",
                Left = 20,
                Top = 540,
                Width = 150,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White
            };
            btnEditLog.FlatAppearance.BorderSize = 0;
            btnEditLog.Click += BtnEditLog_Click;

            // Löscht nach Sicherheitsabfrage den ausgewählten Tabelleneintrag.
            btnDeleteLog = new Button
            {
                Text = "Löschen",
                Left = 185,
                Top = 540,
                Width = 150,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White
            };
            btnDeleteLog.FlatAppearance.BorderSize = 0;
            btnDeleteLog.Click += BtnDeleteLog_Click;

            // Löscht nach Sicherheitsabfrage alle Einträge, die zum aktuellen Filter passen.
            btnDeleteFilteredLogs = new Button
            {
                Text = "Gefilterte löschen",
                Left = 350,
                Top = 540,
                Width = 180,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(130, 35, 35),
                ForeColor = Color.White
            };
            btnDeleteFilteredLogs.FlatAppearance.BorderSize = 0;
            btnDeleteFilteredLogs.Click += BtnDeleteFilteredLogs_Click;

            // Zeigt die Summe der gefilterten, abgeschlossenen Zeiten.
            lblTotalTime = new Label
            {
                Text = "Gesamtzeit: 00:00:00",
                Left = 765,
                Top = 502,
                Width = 180,
                ForeColor = Color.LightGreen
            };

            // Fügt alle erzeugten Steuerelemente zum Formular hinzu.
            Controls.Add(txtTask);
            Controls.Add(btnStartTask);
            Controls.Add(btnEndTask);
            Controls.Add(lblCurrentTask);
            Controls.Add(lblTimer);

            Controls.Add(dgvLogs);   // <-- wichtig

            Controls.Add(txtFilterTasks);
            Controls.Add(dtFrom);
            Controls.Add(dtTo);
            Controls.Add(btnApplyFilter);
            Controls.Add(btnEditLog);
            Controls.Add(btnDeleteLog);
            Controls.Add(btnDeleteFilteredLogs);
            Controls.Add(lblTotalTime);
        }

        // =====================
        // START TASK
        // =====================
        // Wird ausgelöst, wenn der Benutzer auf "Start Task" klickt.
        private void BtnStartTask_Click(object sender, EventArgs e)
        {
            // Leere Eingaben werden ignoriert, damit keine namenlosen Tasks entstehen.
            string task = txtTask.Text.Trim();

            if (string.IsNullOrWhiteSpace(task))
                return;

            // Datenbanklogik beendet ggf. den alten Task und legt den neuen aktiven Task an.
            DatabaseHelper.StartTask(task);

            txtTask.Clear();
            LoadData();
            ApplyFilter();
        }

        // Wird ausgelöst, wenn der Benutzer den aktuell laufenden Task beenden möchte.
        private void BtnEndTask_Click(object sender, EventArgs e)
        {
            bool ended = DatabaseHelper.EndCurrentTask();

            if (!ended)
            {
                MessageBox.Show("Es läuft gerade kein Task.");
                return;
            }

            LoadData();
            ApplyFilter();
            lblTimer.Text = "Laufzeit: 00:00:00";
        }

        // =====================
        // FILTER
        // =====================
        // Button-Handler für "Apply Filter".
        private void BtnApplyFilter_Click(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        // Wendet den aktuellen Task- und Datumsfilter an und aktualisiert Tabelle und Gesamtzeit.
        private void ApplyFilter()
        {
            string[] tasks = GetFilterTasks();
            string dateFrom = GetFilterDateFrom();
            string dateTo = GetFilterDateTo();

            dgvLogs.DataSource = DatabaseHelper.GetFilteredLogsMulti(tasks, dateFrom, dateTo);

            // Die Gesamtzeit verwendet dieselben Filter wie die Tabelle.
            TimeSpan total = DatabaseHelper.GetTotalDurationMulti(tasks, dateFrom, dateTo);
            lblTotalTime.Text = $"Gesamtzeit: {total:hh\\:mm\\:ss}";
        }

        // Liest die kommagetrennten Task-Suchbegriffe aus dem Filterfeld.
        private string[] GetFilterTasks()
        {
            return txtFilterTasks.Text
                .Trim()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        // Liefert das Von-Datum im Datenbankformat.
        private string GetFilterDateFrom()
        {
            return dtFrom.Value.ToString("yyyy-MM-dd");
        }

        // Liefert das Bis-Datum im Datenbankformat.
        private string GetFilterDateTo()
        {
            return dtTo.Value.ToString("yyyy-MM-dd");
        }

        // Öffnet einen Dialog, mit dem der ausgewählte Zeiteintrag geändert werden kann.
        private void BtnEditLog_Click(object sender, EventArgs e)
        {
            DataGridViewRow? row = dgvLogs.CurrentRow;

            // Ohne ausgewählte Tabellenzeile gibt es keinen Eintrag zum Bearbeiten.
            if (row == null || row.IsNewRow)
            {
                MessageBox.Show("Bitte zuerst einen Eintrag auswählen.");
                return;
            }

            // Werte aus der ausgewählten Zeile lesen und als Startwerte in den Dialog übernehmen.
            int id = Convert.ToInt32(row.Cells["Id"].Value);
            string datum = row.Cells["Datum"].Value?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd");
            string task = row.Cells["Task"].Value?.ToString() ?? "";
            string startTime = row.Cells["StartTime"].Value?.ToString() ?? "";
            string endTime = row.Cells["EndTime"].Value?.ToString() ?? "";

            // Kleiner modaler Dialog für die Bearbeitung eines einzelnen Eintrags.
            using Form editForm = new Form
            {
                Text = "Eintrag bearbeiten",
                Width = 360,
                Height = 280,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White
            };

            Label lblDate = CreateDialogLabel("Datum", 20, 25);
            DateTimePicker dtDate = new DateTimePicker
            {
                Left = 120,
                Top = 20,
                Width = 180,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.TryParse(datum, out DateTime parsedDate) ? parsedDate : DateTime.Now
            };

            Label lblTask = CreateDialogLabel("Task", 20, 65);
            TextBox txtEditTask = CreateDialogTextBox(task, 120, 60);

            Label lblStart = CreateDialogLabel("Start", 20, 105);
            TextBox txtStart = CreateDialogTextBox(startTime, 120, 100);

            Label lblEnd = CreateDialogLabel("Ende", 20, 145);
            TextBox txtEnd = CreateDialogTextBox(endTime, 120, 140);

            Button btnSave = new Button
            {
                Text = "Speichern",
                Left = 120,
                Top = 185,
                Width = 90,
                DialogResult = DialogResult.OK
            };

            Button btnCancel = new Button
            {
                Text = "Abbrechen",
                Left = 220,
                Top = 185,
                Width = 90,
                DialogResult = DialogResult.Cancel
            };

            editForm.Controls.AddRange(new Control[]
            {
                lblDate, dtDate, lblTask, txtEditTask, lblStart, txtStart, lblEnd, txtEnd, btnSave, btnCancel
            });
            editForm.AcceptButton = btnSave;
            editForm.CancelButton = btnCancel;

            // Abbrechen schließt den Dialog ohne Datenbankänderung.
            if (editForm.ShowDialog(this) != DialogResult.OK)
                return;

            // Werte nach dem Dialog erneut aus den Eingabefeldern lesen.
            string updatedTask = txtEditTask.Text.Trim();
            string updatedStart = txtStart.Text.Trim();
            string updatedEnd = txtEnd.Text.Trim();

            // Ein Task-Name ist Pflicht.
            if (string.IsNullOrWhiteSpace(updatedTask))
            {
                MessageBox.Show("Der Task darf nicht leer sein.");
                return;
            }

            // Startzeit muss gültig sein; Endzeit darf leer bleiben, wenn der Task noch aktiv sein soll.
            if (!IsValidTime(updatedStart) || (!string.IsNullOrWhiteSpace(updatedEnd) && !IsValidTime(updatedEnd)))
            {
                MessageBox.Show("Bitte Zeiten im Format HH:mm:ss eingeben. Ende darf leer sein.");
                return;
            }

            // Speichert die Änderungen in der Datenbank und lädt danach die Ansicht neu.
            DatabaseHelper.UpdateLog(
                id,
                dtDate.Value.ToString("yyyy-MM-dd"),
                updatedTask,
                updatedStart,
                string.IsNullOrWhiteSpace(updatedEnd) ? null : updatedEnd);

            RefreshLogs();
        }

        // Löscht den aktuell ausgewählten Zeiteintrag nach einer Bestätigung.
        private void BtnDeleteLog_Click(object sender, EventArgs e)
        {
            DataGridViewRow? row = dgvLogs.CurrentRow;

            // Ohne ausgewählte Tabellenzeile gibt es keinen Eintrag zum Löschen.
            if (row == null || row.IsNewRow)
            {
                MessageBox.Show("Bitte zuerst einen Eintrag auswählen.");
                return;
            }

            int id = Convert.ToInt32(row.Cells["Id"].Value);
            string task = row.Cells["Task"].Value?.ToString() ?? "";

            // Sicherheitsabfrage, damit ein Klick nicht sofort Daten entfernt.
            DialogResult result = MessageBox.Show(
                $"Eintrag \"{task}\" wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            DatabaseHelper.DeleteLog(id);
            RefreshLogs();
        }

        // Lädt die Tabelle neu und behält dabei die aktuellen Filtereinstellungen bei.
        // Löscht alle Einträge, die den aktuell eingestellten Filtern entsprechen.
        private void BtnDeleteFilteredLogs_Click(object sender, EventArgs e)
        {
            string[] tasks = GetFilterTasks();
            string dateFrom = GetFilterDateFrom();
            string dateTo = GetFilterDateTo();

            DataTable filteredLogs = DatabaseHelper.GetFilteredLogsMulti(tasks, dateFrom, dateTo);
            int count = filteredLogs.Rows.Count;

            if (count == 0)
            {
                MessageBox.Show("Es gibt keine Einträge für den aktuellen Filter.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"{count} gefilterte Einträge wirklich löschen?\n\nDiese Aktion kann nicht rückgängig gemacht werden.",
                "Gefilterte Daten löschen",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            int deletedCount = DatabaseHelper.DeleteFilteredLogsMulti(tasks, dateFrom, dateTo);

            LoadData();
            ApplyFilter();

            MessageBox.Show($"{deletedCount} Einträge wurden gelöscht.");
        }

        private void RefreshLogs()
        {
            ApplyFilter();
        }

        // Prüft, ob eine Zeit exakt im Format HH:mm:ss eingegeben wurde.
        private static bool IsValidTime(string value)
        {
            return TimeSpan.TryParseExact(value, "hh\\:mm\\:ss", null, out _);
        }

        // Hilfsmethode für einheitliche Labels im Bearbeiten-Dialog.
        private static Label CreateDialogLabel(string text, int left, int top)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 90,
                ForeColor = Color.White
            };
        }

        // Hilfsmethode für einheitliche Textboxen im Bearbeiten-Dialog.
        private static TextBox CreateDialogTextBox(string text, int left, int top)
        {
            return new TextBox
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 180,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
        }

        // =====================
        // LOAD DATA
        // =====================
        // Lädt alle Einträge in die Tabelle und aktualisiert die Anzeige der aktiven Aufgabe.
        private void LoadData()
        {
            dgvLogs.DataSource = DatabaseHelper.GetAllLogs();

            var current = DatabaseHelper.GetCurrentTask();

            // Wenn ein offener Eintrag existiert, wird er als laufende Aufgabe angezeigt.
            if (current != null)
            {
                lblCurrentTask.Text = "Aktive Aufgabe: " + current["Task"].ToString();
                taskStartTime = DateTime.Parse(current["StartTime"].ToString());
            }
            else
            {
                lblCurrentTask.Text = "Keine aktive Aufgabe";
                lblTimer.Text = "Laufzeit: 00:00:00";
            }
        }

        // =====================
        // TIMER
        // =====================
        // Startet einen UI-Timer, der jede Sekunde die Laufzeitanzeige aktualisiert.
        private void StartTimer()
        {
            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 1000;

            // Der Timer fragt die aktuell laufende Aufgabe ab und berechnet die Dauer seit StartTime.
            uiTimer.Tick += (s, e) =>
            {
                var current = DatabaseHelper.GetCurrentTask();

                if (current != null)
                {
                    TimeSpan duration = DateTime.Now - taskStartTime;
                    lblTimer.Text = $"Laufzeit: {duration:hh\\:mm\\:ss}";
                }
                else
                {
                    lblTimer.Text = "Laufzeit: 00:00:00";
                }
            };

            uiTimer.Start();
        }
    }
}
