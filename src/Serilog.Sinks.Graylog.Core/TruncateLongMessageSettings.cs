namespace Serilog.Sinks.Graylog.Core
{
    public class TruncateLongMessageSettings
    {
        public bool Available { get; set; } = false;

        public int MaxLengthMessage { get; set; } = 150000;

        public int MaxLengthPropertyMessage { get; set; } = 8000;

        public string PostfixTruncatedMessage { get; set; } = "(Truncated...)";

        public TruncateLongMessageSettings()
        {
        }

        public TruncateLongMessageSettings(bool available, int maxLengthMessage, int maxLengthPropertyMessage)
        {
            Available = available;
            MaxLengthMessage = maxLengthMessage;
            MaxLengthPropertyMessage = maxLengthPropertyMessage;
        }
    }
}