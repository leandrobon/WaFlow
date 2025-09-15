using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WebApplication1.Models.Enums;

namespace WebApplication1.Models
{
    public sealed class WebhookDelivery
    {
        [JsonPropertyName("waFlowVersion")]
        public string WaFlowVersion { get; init; } = "v0";

        [Required, MinLength(1), JsonPropertyName("messages")]
        public required List<WebhookDeliveryMessage> Messages { get; init; }
        
    }

    public sealed class WebhookDeliveryMessage
    {
        [Required, StringLength(128, MinimumLength = 1), JsonPropertyName("from")]
        public required string From { get; init; }

        [Required, JsonPropertyName("type")]
        public required MessageType Type { get; init; }  

        [JsonPropertyName("text")]
        public TextBody? Text { get; init; } // Required if type = Text

        [Required, JsonPropertyName("timestamp")]
        public required DateTimeOffset Timestamp { get; init; }
    }
}

