using Newtonsoft.Json;

namespace SelectelDbLogParser;

public class SelectelLog
{
    [JsonProperty("logs")]
    public SelectelLogEntry[] Logs { get; set; }
}