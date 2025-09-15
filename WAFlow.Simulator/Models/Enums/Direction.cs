using System.Text.Json.Serialization;

namespace WebApplication1.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Direction
{
    Inbound = 0, 
    Outbound = 1   
}