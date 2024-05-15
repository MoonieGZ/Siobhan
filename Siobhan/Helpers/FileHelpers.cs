using System.Text.Json;

namespace Siobhan.Helpers;

public abstract class FileHelpers
{
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    
        public static T ReadJson<T>(string filePath)
        {
            string text = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(text);
        }
}