namespace MonitorDaylightSync.Configuration;

public class MonitorConfiguration
{
    public List<Monitor> Monitors { get; set; } = [];
}

public class Monitor
{
    public string? Name { get; set; }
    public Brightness Brightness { get; set; } = new();
    public Contrast Contrast { get; set; } = new();
}

public class Brightness
{
    public short CmmCode { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}

public class Contrast
{
    public short CmmCode { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
}