// See https://aka.ms/new-console-template for more information

using System.Text;
using Newtonsoft.Json;
using SelectelDbLogParser;


internal class Program
{
    public static async Task Main(string[] args)
    {
        var log = await LoadLogs();
    }

    static async Task<SelectelLog> LoadLogs()
    {
        Console.Write("Введите токен авторизации для Selectel: ");
        var authToken = Console.ReadLine();
        if (string.IsNullOrEmpty(authToken))
        {
            throw new Exception("Введена пустая строка");
        }
        Console.Write("Введите url запроса: ");
        var url = Console.ReadLine();
        if (string.IsNullOrEmpty(url))
        {
            throw new Exception("Введена пустая строка");
        }
        Console.Write("Введите дату С в формате дд-мм-гггг чч:мм:сс: ");
        var start = DateTime.ParseExact(Console.ReadLine(), "dd-MM-yyyy HH:mm:ss", null).ToUniversalTime();
        Console.Write("Введите дату ПО в формате дд-мм-гггг чч:мм:сс: ");
        var end = DateTime.ParseExact(Console.ReadLine(), "dd-MM-yyyy HH:mm:ss", null).ToUniversalTime();
        var logs = (await LoadLogsFromApi(start, end, authToken, url)).ToArray();
        return new SelectelLog
        {
            Logs = logs
        };
    }

    static async Task SaveLogs(SelectelLog log)
    {
        // Вот здесь можно вернуть объект через return
        Console.Write("Введите путь для сохранения лога: ");
        var path = Console.ReadLine();
        if (string.IsNullOrEmpty(path))
        {
            throw new Exception("Введена пустая строка");
        }
        await SaveLogJson(log, path);
    }

    static void FilterLogs()
    {
        Console.WriteLine("Введите путь до json-лога");
        var path = Console.ReadLine();
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("Введена пустая строка");
            return;
        }

        if (!File.Exists(path))
        {
            Console.WriteLine("Файл по введеному пути не существуют");
            return;
        }
        var jsonStr = File.ReadAllText(path);
        var log = JsonConvert.DeserializeObject<SelectelLog>(jsonStr);
        if (log == null)
        {
            Console.WriteLine("Не удалось десериализовать json");
            return;
        }
        Console.Write("Введите ключевое слово для фильтрации запросов (название таблицы, столбца и т.п.): ");
        var keyword = Console.ReadLine() ?? string.Empty;
        FilterLogsAndSaveQueries(keyword, log);
    }
    
    static async Task SaveLogJson(SelectelLog log, string path)
    {
        var jsonStr = JsonConvert.SerializeObject(log);
        if(File.Exists(path))
            File.Delete(path);
        await File.WriteAllTextAsync(path, jsonStr);
        Console.WriteLine($"Успешно сохранено в {path}");
    }

    static void FilterLogsAndSaveQueries(string keyword, SelectelLog log)
    {
        var logsDn = log.Logs.OrderByDescending(x => x.Labels.QueryTime)
            .Where(x => x.Query.Contains(keyword)).ToArray();
        Console.Write($"Найдено {logsDn.Length} запросов по ключевому слову {keyword}.\nВведите путь для сохранения: ");
        var pathToSave = Console.ReadLine();
        if(string.IsNullOrEmpty(pathToSave))
        {
            Console.WriteLine("Некорретный путь");
            return;
        }
        MakeSqlFileForEntries(logsDn, pathToSave);
    }

    static void MakeSqlFileForEntries(IEnumerable<SelectelLogEntry> entries, string savePath)
    {
        if (File.Exists(savePath)) 
            File.Delete(savePath);
        var sb = new StringBuilder();
        foreach (var entry in entries)
        {
            sb.Append(entry.Query);
            sb.AppendLine();
            sb.AppendLine();
        }
        File.WriteAllText(savePath, sb.ToString());
    }
    
    static SelectelLog? LoadLogsFromFile()
    {
        Console.Write("Path to json log file: ");
        var path = Console.ReadLine();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Console.WriteLine("Некорретный путь");
            return null;
        }

        var lines = File.ReadAllText(path);
        var logs = JsonConvert.DeserializeObject<SelectelLog>(lines);
        return logs;
    }
    
    static async Task<IEnumerable<SelectelLogEntry>> LoadLogsFromApi(DateTime start, DateTime end, string authToken, string url)
    {
        // поскольку селектел долбоебы которые выгружают данные с конченой пагинацией
        // то мы будем двигать start в зависимости от даты последнего запроса в ответе добавляя + 1 минуту
        var currentStart = start;
        var result = new LinkedList<SelectelLogEntry>();
        var loader = new SelectelLogsLoader(authToken, url);
        var firstPortion = await loader.LoadLogFromApi(currentStart, end);
        if (firstPortion == null)
            throw new Exception("Не удалось получить логи от селектела");
        var lastQueryTime = firstPortion.Logs.MaxBy(x => x.UtcQueryDate)?.UtcQueryDate;
        if (lastQueryTime == null)
            return Array.Empty<SelectelLogEntry>();
        result.AppendRange(firstPortion.Logs);
        currentStart = lastQueryTime.Value.AddMinutes(1);
        while (currentStart <= end)
        {
            Console.WriteLine($"Выгрузка логов с {currentStart:s} по {end:s}");
            var portion = await loader.LoadLogFromApi(currentStart, end);
            if (portion == null)
            {
                Console.WriteLine($"За промежуток с {currentStart:s} по {end:s} логи выгрузить не удалось. Повторная попытка");
                continue;
            } 
            lastQueryTime = portion.Logs.MaxBy(x => x.UtcQueryDate)?.UtcQueryDate;
            if (lastQueryTime == null)
            {
                Console.WriteLine($"За промежуток с {currentStart:s} по {end:s} логи выгрузить не удалось. Повторная попытка");
                continue;
            }

            currentStart = lastQueryTime.Value.AddMinutes(1);
            result.AppendRange(portion.Logs);
            await Task.Delay(20 * 1000);
        }

        return result;
    }
}