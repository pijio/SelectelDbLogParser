using Newtonsoft.Json;

namespace SelectelDbLogParser;

public class SelectelLogEntry
{
    [JsonIgnore]
    public DateTime UtcQueryDate { get; set; }
    
    [JsonIgnore]
    private long _timeStamp;
    
    [JsonProperty("ts")]
    public long TimeStamp
    {
        get => _timeStamp;
        set
        {
            _timeStamp = value;
            // селектел пихает огромное непонятное число из которых нам нужно для даты только первые 10 символов
            var firstTenDigits = (long)(value / Math.Pow(10, Math.Floor(Math.Log10(value) - 9)));
            UtcQueryDate = DateTimeHelper.ConvertFromUnixTimestamp(firstTenDigits);
        } 
    }
    
    [JsonProperty("datastore_id")]
    public string DatastoreId { get; set; }

    [JsonProperty("ip")]
    public string IpAddress { get; set; }

    [JsonProperty("value")]
    public string Query { get; set; }

    [JsonProperty("labels")]
    public Labels Labels { get; set; }
}

public class Labels
{
    [JsonProperty("bytes_sent")]
    public string BytesSent { get; set; }

    [JsonProperty("lock_time")]
    public string LockTime { get; set; }

    [JsonProperty("last_errno")]
    public string LastErrno { get; set; }

    private string _queryTimeString;
    [JsonProperty("query_time")]
    public string QueryTimeString { get => _queryTimeString;
        set
        {
            _queryTimeString = value;
            QueryTime = Double.Parse(value.Replace(".", ","));
        } 
    }
    
    [JsonIgnore]
    public double QueryTime { get; set; }
    
    [JsonProperty("user")]
    public string User { get; set; }

    [JsonProperty("rows_sent")]
    public string RowsSent { get; set; }

    [JsonProperty("rows_examined")]
    public string RowsExamined { get; set; }

    [JsonProperty("db")]
    public string Database { get; set; }

    [JsonProperty("killed")]
    public string Killed { get; set; }

    [JsonProperty("rows_affected")]
    public string RowsAffected { get; set; }
}