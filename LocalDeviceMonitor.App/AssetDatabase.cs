using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.IO;

using LocalDeviceMonitor.App;

namespace LocalDeviceMonitor.App
{

    public class AssetDatabase
    {
        private readonly DatabaseSettings _settings;

        public AssetDatabase()
        {
            _settings = AppConfig.LoadDatabaseSettings();
        }
        public void EnsureDatabaseCreated()
        {
            var dbPath = GetSqliteDatabasePath(_settings.ConnectionString);

            RunMigrations();
            RunDefaults();
        }
        private string GetSqliteDatabasePath(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);

            var dataSource = builder.DataSource;

            if (string.IsNullOrWhiteSpace(dataSource))
                throw new Exception("Data Source não definido na connection string.");

            if (!Path.IsPathRooted(dataSource))
            {
                dataSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataSource);
            }

            return dataSource;
        }

        private DbConnection CreateConnection()
        {
            if (_settings.Provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                return new SqliteConnection(_settings.ConnectionString);
            }

            throw new NotSupportedException($"Provider não suportado: {_settings.Provider}");
        }

        public void RunMigrations()
        {
            using var conn = CreateConnection();
            conn.Open();

            var sql = """
        CREATE TABLE IF NOT EXISTS Ativos (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            ativo TEXT NOT NULL,
            origem TEXT NULL,
            device TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS Atributos (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            atributo TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS AtivosAtributos (
            id_ativo INTEGER NOT NULL,
            id_atributo INTEGER NOT NULL,
            PRIMARY KEY (id_ativo, id_atributo),
            FOREIGN KEY (id_ativo) REFERENCES Ativos(id) ON DELETE CASCADE,
            FOREIGN KEY (id_atributo) REFERENCES Atributos(id) ON DELETE CASCADE
        );
        CREATE TABLE IF NOT EXISTS Workspaces (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            nome TEXT NOT NULL UNIQUE,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS WorkspaceDevices (
            workspace_id INTEGER NOT NULL,
            device_id TEXT NOT NULL,
            origin TEXT NULL,
            icon TEXT NULL,
            device_type TEXT NULL,
            name TEXT NULL,
            manufacturer TEXT NULL,
            mac_address TEXT NULL,
            ip_address TEXT NULL,
            status TEXT NULL,
            protocol TEXT NULL,
            rssi INTEGER NULL,
            estimated_distance REAL NULL,
            open_ports TEXT NULL,
            detected_services TEXT NULL,
            is_suspicious INTEGER NOT NULL DEFAULT 0,
            suspicious_reason TEXT NULL,
            previous_ip_address TEXT NULL,
            bacnet_device_id INTEGER NULL,
            bacnet_vendor_name TEXT NULL,
            bacnet_model_name TEXT NULL,
            bacnet_firmware TEXT NULL,
            bacnet_object_summary TEXT NULL,
            modbus_unit_id INTEGER NULL,
            modbus_register_summary TEXT NULL,
            onvif_xaddr TEXT NULL,
            onvif_scopes TEXT NULL,
            onvif_endpoint_address TEXT NULL,
            last_seen TEXT NULL,
            custom_notes TEXT NULL,
            atributo_atual TEXT NULL,
            PRIMARY KEY (workspace_id, device_id),
            FOREIGN KEY (workspace_id) REFERENCES Workspaces(id) ON DELETE CASCADE
        );
        """;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public void RunDefaults()
        {
            using var conn = CreateConnection();
            conn.Open();

            var sql = """
        INSERT OR IGNORE INTO Atributos (atributo) VALUES ('Interno');
        INSERT OR IGNORE INTO Atributos (atributo) VALUES ('Externo');
        """;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }


        public int UpsertAtributo(string atributo)
        {
            using var conn = CreateConnection();
            conn.Open();

            var sql = """
            INSERT INTO Atributos (atributo)
            VALUES (@atributo);
            SELECT id FROM Atributos WHERE atributo = @atributo;
            """;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            AddParam(cmd, "@atributo", atributo);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        private static void AddParam(DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

    }
}