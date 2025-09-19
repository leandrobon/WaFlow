using System.Text.Json.Serialization;

namespace WebApplication1.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
    Text = 0
    // v1: Image = 1,
    // v1: Template = 2
}