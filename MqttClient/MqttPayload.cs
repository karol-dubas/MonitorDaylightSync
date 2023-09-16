using System.Runtime.Serialization;

namespace MqttClient;

public class MqttPayload
{
    [DataMember(Name="brightness")]
    public int Brightness { get; set; }
    
    [DataMember(Name="color")]
    public int Color { get; set; }
}