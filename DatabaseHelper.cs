using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace AutoTimeTracker
{
    // Kapselt alle direkten Datenbankzugriffe der Anwendung.
    // Die UI ruft nur diese Methoden auf und muss keine SQL-Details kennen.
    public static class DatabaseHelper
    {
        // Die Datenbankdatei liegt neben der gestarteten EXE im Ausgabeverzeichnis.
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "timelog.db");

        // Zentrale SQLite-Verbindungszeichenfolge für alle Datenbankoperationen.
        private static readonly string ConnectionString =
            $"Data Source={DbPath};Version=3;";

        // Erstellt die Datenbankdatei und die Tabelle WorkLog, falls sie noch nicht existieren.
        public static void InitializeDatabase()
        {
            if (!File.Exists(DbPath))
                SQLiteConnection.CreateFile(DbPath);

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = @"
            CREATE TABLE IF NOT EXISTS WorkLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Datum TEXT NOT NULL,
                Task TEXT NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT
            );";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // Startet eine neue Aufgabe.
        // Falls bereits eine Aufgabe läuft, wird diese vorher mit der aktuellen Uhrzeit beendet.
        public static void StartTask(string task)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string time = DateTime.Now.ToString("HH:mm:ss");

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            // Beendet den zuletzt gestarteten offenen Eintrag.
            string closeSql = @"
                UPDATE WorkLog
                SET EndTime = @EndTime
                WHERE Id = (
                    SELECT Id FROM WorkLog
                    WHERE EndTime IS NULL
                    ORDER BY Id DESC
                    LIMIT 1
                );";

            using (var cmd = new SQLiteCommand(closeSql, conn))
            {
                cmd.Parameters.AddWithValue("@EndTime", time);
                cmd.ExecuteNonQuery();
            }

            // Legt den neuen aktiven Eintrag ohne Endzeit an.
            string insertSql = @"
                INSERT INTO WorkLog (Datum, Task, StartTime, EndTime)
                VALUES (@Datum, @Task, @StartTime, NULL);";

            using (var cmd = new SQLiteCommand(insertSql, conn))
            {
                cmd.Parameters.AddWithValue("@Datum", date);
                cmd.Parameters.AddWithValue("@Task", task);
                cmd.Parameters.AddWithValue("@StartTime", time);
                cmd.ExecuteNonQuery();
            }
        }

        // Beendet den aktuell laufenden Task, ohne direkt einen neuen Task zu starten.
        // Gibt true zurück, wenn ein offener Eintrag gefunden und geschlossen wurde.
        public static bool EndCurrentTask()
        {
            string time = DateTime.Now.ToString("HH:mm:ss");

            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = @"
                UPDATE WorkLog
                SET EndTime = @EndTime
                WHERE Id = (
                    SELECT Id FROM WorkLog
                    WHERE EndTime IS NULL
                    ORDER BY Id DESC
                    LIMIT 1
                );";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EndTime", time);

            return cmd.ExecuteNonQuery() > 0;
        }

        // Liefert den aktuell laufenden Task, also den neuesten Eintrag ohne EndTime.
        // Gibt null zurück, wenn gerade keine Aufgabe aktiv ist.
        public static DataRow GetCurrentTask()
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = @"
                SELECT * FROM WorkLog
                WHERE EndTime IS NULL
                ORDER BY Id DESC
                LIMIT 1;";

            using var adapter = new SQLiteDataAdapter(sql, conn);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // Lädt alle Zeiterfassungs-Einträge, neueste Einträge zuerst.
        public static DataTable GetAllLogs()
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = @"
                SELECT *
                FROM WorkLog
                ORDER BY Id DESC;";

            using var adapter = new SQLiteDataAdapter(sql, conn);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            return dt;
        }

        // Lädt Einträge nach optionalem Task-Text und optionalem Datum.
        // Diese ältere Einzel-Filter-Methode bleibt für einfache Filterlogik verfügbar.
        public static DataTable GetFilteredLogs(string task, string date)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = @"
                SELECT *
                FROM WorkLog
                WHERE (@Task = '' OR Task LIKE '%' || @Task || '%')
                AND (@Date = '' OR Datum = @Date)
                ORDER BY Id DESC;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Task", task ?? "");
            cmd.Parameters.AddWithValue("@Date", date ?? "");

            using var adapter = new SQLiteDataAdapter(cmd);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            return dt;
        }

        // Aktualisiert einen bestehenden Zeiteintrag nach dem Bearbeiten-Dialog.
        // Eine leere Endzeit wird als NULL gespeichert und bedeutet: Task läuft noch.
        public static void UpdateLog(int id, string datum, string task, string startTime, string? endTime)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = @"
                UPDATE WorkLog
                SET Datum = @Datum,
                    Task = @Task,
                    StartTime = @StartTime,
                    EndTime = @EndTime
                WHERE Id = @Id;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Datum", datum);
            cmd.Parameters.AddWithValue("@Task", task);
            cmd.Parameters.AddWithValue("@StartTime", startTime);
            cmd.Parameters.AddWithValue("@EndTime", string.IsNullOrWhiteSpace(endTime) ? DBNull.Value : endTime);
            cmd.ExecuteNonQuery();
        }

        // Löscht einen einzelnen Zeiteintrag anhand seiner Datenbank-ID.
        public static void DeleteLog(int id)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = "DELETE FROM WorkLog WHERE Id = @Id;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        // Löscht alle Einträge, die zum aktuellen Mehrfach-Task-Filter und Datumsbereich passen.
        // Gibt die Anzahl der gelöschten Datensätze zurück.
        public static int DeleteFilteredLogsMulti(string[] tasks, string from, string to)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string taskCondition = "";

            // Verwendet dieselbe dynamische Task-Bedingung wie Anzeige und Gesamtzeit.
            if (tasks.Length > 0)
            {
                taskCondition = "AND (" +
                    string.Join(" OR ", tasks.Select((t, i) => $"Task LIKE @t{i}")) +
                ")";
            }

            string sql = $@"
                DELETE FROM WorkLog
                WHERE
                    Datum BETWEEN @From AND @To
                    {taskCondition};";

            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@From", from);
            cmd.Parameters.AddWithValue("@To", to);

            for (int i = 0; i < tasks.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@t{i}", "%" + tasks[i].Trim() + "%");
            }

            return cmd.ExecuteNonQuery();
        }

        // Lädt Einträge für mehrere Task-Suchbegriffe und einen Datumsbereich.
        // Jeder Suchbegriff wird per LIKE verglichen; mehrere Begriffe werden mit OR verbunden.
        public static DataTable GetFilteredLogsMulti(string[] tasks, string from, string to)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string taskCondition = "";

            // Baut den optionalen Task-Filter dynamisch auf, verwendet aber weiterhin SQL-Parameter.
            if (tasks.Length > 0)
            {
                taskCondition = "AND (" +
                    string.Join(" OR ", tasks.Select((t, i) => $"Task LIKE @t{i}")) +
                ")";
            }

            string sql = $@"
            SELECT *
            FROM WorkLog
            WHERE
                Datum BETWEEN @From AND @To
                {taskCondition}
            ORDER BY Id DESC;
            ";

            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@From", from);
            cmd.Parameters.AddWithValue("@To", to);

            // Fügt alle Task-Suchbegriffe als Parameter hinzu, damit keine SQL-Injection möglich ist.
            for (int i = 0; i < tasks.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@t{i}", "%" + tasks[i].Trim() + "%");
            }

            using var adapter = new SQLiteDataAdapter(cmd);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            return dt;
        }

        // Berechnet die Gesamtzeit für einen optionalen Task-Text und ein optionales Datum.
        // Nur abgeschlossene Einträge mit EndTime werden in die Summe aufgenommen.
        public static TimeSpan GetTotalDuration(string task, string date)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string sql = @"
                SELECT SUM(
                    strftime('%s', EndTime) - strftime('%s', StartTime)
                )
                FROM WorkLog
                WHERE (@Task = '' OR Task LIKE '%' || @Task || '%')
                AND (@Date = '' OR Datum = @Date)
                AND EndTime IS NOT NULL;";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Task", task ?? "");
            cmd.Parameters.AddWithValue("@Date", date ?? "");

            object result = cmd.ExecuteScalar();

            if (result == DBNull.Value || result == null)
                return TimeSpan.Zero;

            return TimeSpan.FromSeconds(Convert.ToDouble(result));
        }

        // Berechnet die Gesamtzeit für mehrere Task-Suchbegriffe und einen Datumsbereich.
        // Diese Methode verwendet dieselbe Filterlogik wie GetFilteredLogsMulti.
        public static TimeSpan GetTotalDurationMulti(string[] tasks, string from, string to)
        {
            using var conn = new SQLiteConnection(ConnectionString);
            conn.Open();

            string taskCondition = "";

            // Baut bei Bedarf eine OR-Bedingung für mehrere Task-Suchbegriffe.
            if (tasks.Length > 0)
            {
                taskCondition = "AND (" +
                    string.Join(" OR ", tasks.Select((t, i) => $"Task LIKE @t{i}")) +
                ")";
            }

            string sql = $@"
                SELECT SUM(strftime('%s', EndTime) - strftime('%s', StartTime))
                FROM WorkLog
                WHERE
                    Datum BETWEEN @From AND @To
                    AND EndTime IS NOT NULL
                    {taskCondition};
            ";

            using var cmd = new SQLiteCommand(sql, conn);

            cmd.Parameters.AddWithValue("@From", from);
            cmd.Parameters.AddWithValue("@To", to);

            // Bindet die dynamisch erzeugten Platzhalter an konkrete Parameterwerte.
            for (int i = 0; i < tasks.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@t{i}", "%" + tasks[i].Trim() + "%");
            }

            object result = cmd.ExecuteScalar();

            if (result == DBNull.Value || result == null)
                return TimeSpan.Zero;

            return TimeSpan.FromSeconds(Convert.ToDouble(result));
        }
    }
}
