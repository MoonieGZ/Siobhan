namespace Siobhan.Helpers;

public abstract class FileHelpers
{
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}