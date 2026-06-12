using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Security.RightsManagement;
using LocalDeviceMonitor.App.Modelos;

namespace LocalDeviceMonitor.App
{
    public class AssetRepository
    {
        private readonly DatabaseSettings _settings;

        public AssetRepository()
        {
            _settings = AppConfig.LoadDatabaseSettings();

            // NOVO: Garante que as tabelas novas são criadas ao arrancar
            new AssetDatabase().EnsureDatabaseCreated();
        }

        private DbConnection CreateConnection()
        {
            return new SqliteConnection(_settings.ConnectionString);
        }

        public int UpsertAtivo(string ativo, string? origem, string device)
        {
            using var conn = CreateConnection();
            conn.Open();

            var sql = """
            INSERT INTO Ativos (ativo, origem, device)
            VALUES (@ativo, @origem, @device)
            ON CONFLICT(device) DO UPDATE SET
                ativo = excluded.ativo,
                origem = excluded.origem;

            SELECT id FROM Ativos WHERE device = @device;
            """;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            AddParam(cmd, "@ativo", ativo);
            AddParam(cmd, "@origem", origem);
            AddParam(cmd, "@device", device);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateTituloAtivo(int ativoId, string novoTitulo)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
            UPDATE Ativos
            SET ativo = @ativo
            WHERE id = @id;
            """;

            AddParam(cmd, "@ativo", novoTitulo);
            AddParam(cmd, "@id", ativoId);

            cmd.ExecuteNonQuery();
        }

        public List<Atributo> GetAtributos()
        {
            var result = new List<Atributo>();

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, atributo FROM Atributos ORDER BY atributo;";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new Atributo
                {
                    Id = reader.GetInt32(0),
                    Nome = reader.GetString(1)
                });
            }

            return result;
        }

        public void SetAtributoAtivo(int ativoId, int atributoId, bool selected)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();

            if (selected)
            {
                cmd.CommandText = """
                INSERT OR IGNORE INTO AtivosAtributos (id_ativo, id_atributo)
                VALUES (@id_ativo, @id_atributo);
                """;
            }
            else
            {
                cmd.CommandText = """
                DELETE FROM AtivosAtributos
                WHERE id_ativo = @id_ativo
                  AND id_atributo = @id_atributo;
                """;
            }

            AddParam(cmd, "@id_ativo", ativoId);
            AddParam(cmd, "@id_atributo", atributoId);

            cmd.ExecuteNonQuery();
        }

        public List<int> GetAtributosDoAtivo(int ativoId)
        {
            var result = new List<int>();

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
            SELECT id_atributo
            FROM AtivosAtributos
            WHERE id_ativo = @id_ativo;
            """;

            AddParam(cmd, "@id_ativo", ativoId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(reader.GetInt32(0));
            }

            return result;
        }

        public void SaveOrUpdateDevice(DeviceInfo device)
        {
            using var conn = CreateConnection();
            conn.Open();

            var sql = @"
            INSERT OR REPLACE INTO Devices (
                Id, Origin, Icon, DeviceType, Name, Manufacturer, MacAddress, IpAddress, 
                Status, Protocol, Rssi, EstimatedDistanceMeters, OpenPorts, DetectedServices, 
                IsSuspicious, SuspiciousReason, PreviousIpAddress, BacnetDeviceId, 
                BacnetVendorName, BacnetModelName, BacnetFirmware, BacnetObjectSummary, 
                ModbusUnitId, ModbusRegisterSummary, OnvifXAddr, OnvifScopes, OnvifEndpointAddress, 
                LastSeen, CustomNotes, AtributoAtual
            ) VALUES (
                @Id, @Origin, @Icon, @DeviceType, @Name, @Manufacturer, @MacAddress, @IpAddress, 
                @Status, @Protocol, @Rssi, @EstimatedDistanceMeters, @OpenPorts, @DetectedServices, 
                @IsSuspicious, @SuspiciousReason, @PreviousIpAddress, @BacnetDeviceId, 
                @BacnetVendorName, @BacnetModelName, @BacnetFirmware, @BacnetObjectSummary, 
                @ModbusUnitId, @ModbusRegisterSummary, @OnvifXAddr, @OnvifScopes, @OnvifEndpointAddress, 
                @LastSeen, @CustomNotes, @AtributoAtual
            );";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            AddParam(cmd, "@Id", device.Id);
            AddParam(cmd, "@Origin", device.Origin);
            AddParam(cmd, "@Icon", device.Icon);
            AddParam(cmd, "@DeviceType", device.DeviceType);
            AddParam(cmd, "@Name", device.Name);
            AddParam(cmd, "@Manufacturer", device.Manufacturer);
            AddParam(cmd, "@MacAddress", device.MacAddress);
            AddParam(cmd, "@IpAddress", device.IpAddress);
            AddParam(cmd, "@Status", device.Status);
            AddParam(cmd, "@Protocol", device.Protocol);
            AddParam(cmd, "@Rssi", device.Rssi);
            AddParam(cmd, "@EstimatedDistanceMeters", device.EstimatedDistanceMeters);
            AddParam(cmd, "@OpenPorts", device.OpenPorts);
            AddParam(cmd, "@DetectedServices", device.DetectedServices);
            AddParam(cmd, "@BacnetDeviceId", device.BacnetDeviceId);
            AddParam(cmd, "@BacnetVendorName", device.BacnetVendorName);
            AddParam(cmd, "@BacnetModelName", device.BacnetModelName);
            AddParam(cmd, "@BacnetFirmware", device.BacnetFirmware);
            AddParam(cmd, "@BacnetObjectSummary", device.BacnetObjectSummary);
            AddParam(cmd, "@ModbusUnitId", device.ModbusUnitId);
            AddParam(cmd, "@ModbusRegisterSummary", device.ModbusRegisterSummary);
            AddParam(cmd, "@OnvifXAddr", device.OnvifXAddr);
            AddParam(cmd, "@OnvifScopes", device.OnvifScopes);
            AddParam(cmd, "@OnvifEndpointAddress", device.OnvifEndpointAddress);
            AddParam(cmd, "@LastSeen", device.LastSeen.ToString("o"));

            // As propriedades customizadas:
            AddParam(cmd, "@CustomNotes", device.CustomNotes);
            AddParam(cmd, "@AtributoAtual", device.AtributoAtual);

         
        }

        public List<DeviceInfo> GetAllDevices()
        {
            var list = new List<DeviceInfo>();

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Devices";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var device = new DeviceInfo
                {
                    Id = reader.GetString(reader.GetOrdinal("Id")),
                    Origin = reader.IsDBNull(reader.GetOrdinal("Origin")) ? "" : reader.GetString(reader.GetOrdinal("Origin")),
                    Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? "" : reader.GetString(reader.GetOrdinal("Icon")),
                    DeviceType = reader.IsDBNull(reader.GetOrdinal("DeviceType")) ? "" : reader.GetString(reader.GetOrdinal("DeviceType")),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? "" : reader.GetString(reader.GetOrdinal("Name")),
                    Manufacturer = reader.IsDBNull(reader.GetOrdinal("Manufacturer")) ? "" : reader.GetString(reader.GetOrdinal("Manufacturer")),
                    MacAddress = reader.IsDBNull(reader.GetOrdinal("MacAddress")) ? "" : reader.GetString(reader.GetOrdinal("MacAddress")),
                    IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? "" : reader.GetString(reader.GetOrdinal("IpAddress")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "" : reader.GetString(reader.GetOrdinal("Status")),
                    Protocol = reader.IsDBNull(reader.GetOrdinal("Protocol")) ? "" : reader.GetString(reader.GetOrdinal("Protocol")),
                    OpenPorts = reader.IsDBNull(reader.GetOrdinal("OpenPorts")) ? "" : reader.GetString(reader.GetOrdinal("OpenPorts")),
                    DetectedServices = reader.IsDBNull(reader.GetOrdinal("DetectedServices")) ? "" : reader.GetString(reader.GetOrdinal("DetectedServices")),
                    BacnetVendorName = reader.IsDBNull(reader.GetOrdinal("BacnetVendorName")) ? "" : reader.GetString(reader.GetOrdinal("BacnetVendorName")),
                    BacnetModelName = reader.IsDBNull(reader.GetOrdinal("BacnetModelName")) ? "" : reader.GetString(reader.GetOrdinal("BacnetModelName")),
                    BacnetFirmware = reader.IsDBNull(reader.GetOrdinal("BacnetFirmware")) ? "" : reader.GetString(reader.GetOrdinal("BacnetFirmware")),
                    BacnetObjectSummary = reader.IsDBNull(reader.GetOrdinal("BacnetObjectSummary")) ? "" : reader.GetString(reader.GetOrdinal("BacnetObjectSummary")),
                    ModbusRegisterSummary = reader.IsDBNull(reader.GetOrdinal("ModbusRegisterSummary")) ? "" : reader.GetString(reader.GetOrdinal("ModbusRegisterSummary")),
                    OnvifXAddr = reader.IsDBNull(reader.GetOrdinal("OnvifXAddr")) ? "" : reader.GetString(reader.GetOrdinal("OnvifXAddr")),
                    OnvifScopes = reader.IsDBNull(reader.GetOrdinal("OnvifScopes")) ? "" : reader.GetString(reader.GetOrdinal("OnvifScopes")),
                    OnvifEndpointAddress = reader.IsDBNull(reader.GetOrdinal("OnvifEndpointAddress")) ? "" : reader.GetString(reader.GetOrdinal("OnvifEndpointAddress")),

                    CustomNotes = reader.IsDBNull(reader.GetOrdinal("CustomNotes")) ? "" : reader.GetString(reader.GetOrdinal("CustomNotes")),
                    AtributoAtual = reader.IsDBNull(reader.GetOrdinal("AtributoAtual")) ? "Nenhum" : reader.GetString(reader.GetOrdinal("AtributoAtual"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("Rssi"))) device.Rssi = reader.GetInt32(reader.GetOrdinal("Rssi"));
                if (!reader.IsDBNull(reader.GetOrdinal("EstimatedDistanceMeters"))) device.EstimatedDistanceMeters = reader.GetDouble(reader.GetOrdinal("EstimatedDistanceMeters"));
                if (!reader.IsDBNull(reader.GetOrdinal("BacnetDeviceId"))) device.BacnetDeviceId = (uint)reader.GetInt64(reader.GetOrdinal("BacnetDeviceId"));
                if (!reader.IsDBNull(reader.GetOrdinal("ModbusUnitId"))) device.ModbusUnitId = reader.GetByte(reader.GetOrdinal("ModbusUnitId"));
                if (!reader.IsDBNull(reader.GetOrdinal("LastSeen"))) device.LastSeen = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastSeen")));

                list.Add(device);
            }

            return list;
        }

        // NOVO: Overload para gravar o atributo na nova tabela de ligação (DevicesAtributos) usando o ID do Device
        public void SetAtributoAtivo(string deviceId, int atributoId, bool selected)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();

            if (selected)
            {
                cmd.CommandText = """
                INSERT OR IGNORE INTO DevicesAtributos (id_device, id_atributo)
                VALUES (@id_device, @id_atributo);
                """;
            }
            else
            {
                cmd.CommandText = """
                DELETE FROM DevicesAtributos
                WHERE id_device = @id_device
                  AND id_atributo = @id_atributo;
                """;
            }

            AddParam(cmd, "@id_device", deviceId);
            AddParam(cmd, "@id_atributo", atributoId);

            cmd.ExecuteNonQuery();
        }
        public List<WorkspaceInfo> GetWorkspaces()
        {
            var result = new List<WorkspaceInfo>();

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
            SELECT w.id, w.nome, w.created_at, w.updated_at, COUNT(d.device_id) AS device_count
            FROM Workspaces w
            LEFT JOIN WorkspaceDevices d ON d.workspace_id = w.id
            GROUP BY w.id, w.nome, w.created_at, w.updated_at
            ORDER BY w.updated_at DESC, w.nome ASC;
            """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new WorkspaceInfo
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CreatedAt = DateTime.Parse(reader.GetString(2)),
                    UpdatedAt = DateTime.Parse(reader.GetString(3)),
                    DeviceCount = reader.GetInt32(4)
                });
            }

            return result;
        }

        public WorkspaceInfo SaveWorkspaceSnapshot(string name, IEnumerable<DeviceInfo> devices)
        {
            var now = DateTime.Now;
            var deviceList = devices.ToList();

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
            INSERT INTO Workspaces (nome, created_at, updated_at)
            VALUES (@nome, @created_at, @updated_at)
            ON CONFLICT(nome) DO UPDATE SET updated_at = excluded.updated_at;
            SELECT id FROM Workspaces WHERE nome = @nome;
            """;
            AddParam(cmd, "@nome", name);
            AddParam(cmd, "@created_at", now.ToString("o"));
            AddParam(cmd, "@updated_at", now.ToString("o"));

            var workspaceId = Convert.ToInt32(cmd.ExecuteScalar());

            using (var deleteCmd = conn.CreateCommand())
            {
                deleteCmd.CommandText = "DELETE FROM WorkspaceDevices WHERE workspace_id = @workspace_id;";
                AddParam(deleteCmd, "@workspace_id", workspaceId);
                deleteCmd.ExecuteNonQuery();
            }

            foreach (var device in deviceList)
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = """
                INSERT INTO WorkspaceDevices (
                    workspace_id, device_id, origin, icon, device_type, name, manufacturer, mac_address,
                    ip_address, status, protocol, rssi, estimated_distance, open_ports, detected_services,
                    is_suspicious, suspicious_reason, previous_ip_address, bacnet_device_id,
                    bacnet_vendor_name, bacnet_model_name, bacnet_firmware, bacnet_object_summary,
                    modbus_unit_id, modbus_register_summary, onvif_xaddr, onvif_scopes,
                    onvif_endpoint_address, last_seen, custom_notes, atributo_atual
                ) VALUES (
                    @workspace_id, @device_id, @origin, @icon, @device_type, @name, @manufacturer, @mac_address,
                    @ip_address, @status, @protocol, @rssi, @estimated_distance, @open_ports, @detected_services,
                    @is_suspicious, @suspicious_reason, @previous_ip_address, @bacnet_device_id,
                    @bacnet_vendor_name, @bacnet_model_name, @bacnet_firmware, @bacnet_object_summary,
                    @modbus_unit_id, @modbus_register_summary, @onvif_xaddr, @onvif_scopes,
                    @onvif_endpoint_address, @last_seen, @custom_notes, @atributo_atual
                );
                """;

                AddDeviceParams(insertCmd, workspaceId, device);
                insertCmd.ExecuteNonQuery();
            }

            return new WorkspaceInfo
            {
                Id = workspaceId,
                Name = name,
                CreatedAt = now,
                UpdatedAt = now,
                DeviceCount = deviceList.Count
            };
        }

        public List<DeviceInfo> GetWorkspaceDevices(int workspaceId)
        {
            var list = new List<DeviceInfo>();

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM WorkspaceDevices WHERE workspace_id = @workspace_id ORDER BY origin, name, ip_address;";
            AddParam(cmd, "@workspace_id", workspaceId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DeviceInfo
                {
                    Id = ReadString(reader, "device_id"),
                    Origin = ReadString(reader, "origin"),
                    Icon = ReadString(reader, "icon"),
                    DeviceType = ReadString(reader, "device_type"),
                    Name = ReadString(reader, "name"),
                    Manufacturer = ReadString(reader, "manufacturer"),
                    MacAddress = ReadString(reader, "mac_address"),
                    IpAddress = ReadString(reader, "ip_address"),
                    Status = ReadString(reader, "status"),
                    Protocol = ReadString(reader, "protocol"),
                    Rssi = ReadNullableInt(reader, "rssi"),
                    EstimatedDistanceMeters = ReadNullableDouble(reader, "estimated_distance"),
                    OpenPorts = ReadString(reader, "open_ports"),
                    DetectedServices = ReadString(reader, "detected_services"),
                    IsSuspicious = ReadNullableInt(reader, "is_suspicious") == 1,
                    SuspiciousReason = ReadString(reader, "suspicious_reason"),
                    PreviousIpAddress = ReadString(reader, "previous_ip_address"),
                    BacnetDeviceId = ReadNullableUInt(reader, "bacnet_device_id"),
                    BacnetVendorName = ReadString(reader, "bacnet_vendor_name"),
                    BacnetModelName = ReadString(reader, "bacnet_model_name"),
                    BacnetFirmware = ReadString(reader, "bacnet_firmware"),
                    BacnetObjectSummary = ReadString(reader, "bacnet_object_summary"),
                    ModbusUnitId = ReadNullableByte(reader, "modbus_unit_id"),
                    ModbusRegisterSummary = ReadString(reader, "modbus_register_summary"),
                    OnvifXAddr = ReadString(reader, "onvif_xaddr"),
                    OnvifScopes = ReadString(reader, "onvif_scopes"),
                    OnvifEndpointAddress = ReadString(reader, "onvif_endpoint_address"),
                    LastSeen = ReadDateTime(reader, "last_seen"),
                    CustomNotes = ReadString(reader, "custom_notes"),
                    AtributoAtual = ReadString(reader, "atributo_atual")
                });
            }

            return list;
        }

        public void DeleteWorkspace(int workspaceId)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Workspaces WHERE id = @workspace_id;";
            AddParam(cmd, "@workspace_id", workspaceId);
            cmd.ExecuteNonQuery();
        }

        private static void AddDeviceParams(DbCommand cmd, int workspaceId, DeviceInfo device)
        {
            AddParam(cmd, "@workspace_id", workspaceId);
            AddParam(cmd, "@device_id", device.Id);
            AddParam(cmd, "@origin", device.Origin);
            AddParam(cmd, "@icon", device.Icon);
            AddParam(cmd, "@device_type", device.DeviceType);
            AddParam(cmd, "@name", device.Name);
            AddParam(cmd, "@manufacturer", device.Manufacturer);
            AddParam(cmd, "@mac_address", device.MacAddress);
            AddParam(cmd, "@ip_address", device.IpAddress);
            AddParam(cmd, "@status", device.Status);
            AddParam(cmd, "@protocol", device.Protocol);
            AddParam(cmd, "@rssi", device.Rssi);
            AddParam(cmd, "@estimated_distance", device.EstimatedDistanceMeters);
            AddParam(cmd, "@open_ports", device.OpenPorts);
            AddParam(cmd, "@detected_services", device.DetectedServices);
            AddParam(cmd, "@is_suspicious", device.IsSuspicious ? 1 : 0);
            AddParam(cmd, "@suspicious_reason", device.SuspiciousReason);
            AddParam(cmd, "@previous_ip_address", device.PreviousIpAddress);
            AddParam(cmd, "@bacnet_device_id", device.BacnetDeviceId);
            AddParam(cmd, "@bacnet_vendor_name", device.BacnetVendorName);
            AddParam(cmd, "@bacnet_model_name", device.BacnetModelName);
            AddParam(cmd, "@bacnet_firmware", device.BacnetFirmware);
            AddParam(cmd, "@bacnet_object_summary", device.BacnetObjectSummary);
            AddParam(cmd, "@modbus_unit_id", device.ModbusUnitId);
            AddParam(cmd, "@modbus_register_summary", device.ModbusRegisterSummary);
            AddParam(cmd, "@onvif_xaddr", device.OnvifXAddr);
            AddParam(cmd, "@onvif_scopes", device.OnvifScopes);
            AddParam(cmd, "@onvif_endpoint_address", device.OnvifEndpointAddress);
            AddParam(cmd, "@last_seen", device.LastSeen == DateTime.MinValue ? null : device.LastSeen.ToString("o"));
            AddParam(cmd, "@custom_notes", device.CustomNotes);
            AddParam(cmd, "@atributo_atual", device.AtributoAtual);
        }

        private static string ReadString(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
        }

        private static int? ReadNullableInt(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        private static double? ReadNullableDouble(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
        }

        private static uint? ReadNullableUInt(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : (uint)reader.GetInt64(ordinal);
        }

        private static byte? ReadNullableByte(DbDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : Convert.ToByte(reader.GetInt32(ordinal));
        }

        private static DateTime ReadDateTime(DbDataReader reader, string columnName)
        {
            var value = ReadString(reader, columnName);
            return DateTime.TryParse(value, out var parsed) ? parsed : DateTime.MinValue;
        }

        // ========================================================
        // HELPER
        // ========================================================
        private static void AddParam(DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}