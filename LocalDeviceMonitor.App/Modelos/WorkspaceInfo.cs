namespace LocalDeviceMonitor.App.Modelos;

public class WorkspaceInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DeviceCount { get; set; }

    public string Summary => $"{DeviceCount} dispositivos";
}
