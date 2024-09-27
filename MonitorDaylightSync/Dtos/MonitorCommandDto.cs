using System.Runtime.Serialization;

namespace MonitorDaylightSync.Dtos;

public class MonitorCommandDto
{
    [DataMember(Name="brightness")]
    public int Brightness { get; set; }
    
    [DataMember(Name="color")]
    public int Color { get; set; }
}