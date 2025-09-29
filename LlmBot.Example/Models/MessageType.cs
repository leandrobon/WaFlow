using System.Text.Json.Serialization;

namespace LlmBot.Example.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
    Text = 0
}