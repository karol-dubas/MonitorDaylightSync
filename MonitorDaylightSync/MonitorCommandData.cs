using System.Runtime.Serialization;

namespace MonitorDaylightSync;

public class MonitorCommandData
{
    [DataMember(Name="brightness")]
    public int Brightness { get; set; }
    
    [DataMember(Name="color")]
    public int Color { get; set; }
}