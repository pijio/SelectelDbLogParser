using Newtonsoft.Json;

namespace SelectelDbLogParser;

public class SelectelLogsLoader
{
    private readonly string _authToken;
    private readonly string _url;
    private const string JobName = "mysql-slow-log";

    public SelectelLogsLoader(string authToken, string url)
    {
        _authToken = authToken;
        _url = url;
    }
    
    /// <summary>
    /// Получить лог от селектела
    /// </summary>
    public async Task<SelectelLog?> LoadLogFromApi(DateTime start, DateTime end)
    {
        var uriBuilder = new UriBuilder(_url);
        var endMs = (long)DateTimeHelper.ConvertToUnixTimestamp(end);
        var startMs = (long)DateTimeHelper.ConvertToUnixTimestamp(start);
        uriBuilder.Query = $"end={endMs}&job={JobName}&start={startMs}";
        var apiUrl = uriBuilder.ToString();
        using var httpClient = CreateHttpClientWithHeaders();
        try
        {
            var response = await httpClient.GetAsync(apiUrl);
            var jsonResult = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            var res = JsonConvert.DeserializeObject<SelectelLog>(jsonResult);
            if (res == null)
                throw new HttpRequestException("Сервер вернул некорректный ответ");
            return res;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка при выполнении HTTP-запроса: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Создает хттп клиент с фиксированным заголовками
    /// </summary>
    private HttpClient CreateHttpClientWithHeaders()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.9");
        httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        httpClient.DefaultRequestHeaders.Add("Host", "ru-2.dbaas.selcloud.ru");
        httpClient.DefaultRequestHeaders.Add("Origin", "https://my.selectel.ru");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://my.selectel.ru/");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 YaBrowser/23.11.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _authToken);
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"118\", \"YaBrowser\";v=\"23.11\", \"Not=A?Brand\";v=\"99\", \"Yowser\";v=\"2.5\"");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");

        return httpClient;
    }
}