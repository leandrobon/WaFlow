using System.Text.Json.Serialization;

namespace EchoBot.Example.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
    Text = 0
}