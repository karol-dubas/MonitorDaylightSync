using System.Runtime.Serialization;

namespace MonitorDaylightSync.Dtos;

public class MonitorCommandDto
{
    private int _brightness;
    private int _color;

    [DataMember(Name="brightness")]
    public int Brightness
    {
        get => _brightness;
        set => _brightness = Math.Clamp(value, 0, 100);
    }

    [DataMember(Name="color")]
    public int Color
    {
        get => _color;
        set => _color = Math.Clamp(value, 0, 100);
    }
}