namespace TargetCombo_V1;

public class Utils
{
    public static void CreateSourceFolderIfNot()
    {
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var sourceDirectory = Path.Combine(exeDirectory, "source");
        if(!Directory.Exists(sourceDirectory))
        {
            Directory.CreateDirectory(sourceDirectory);
        }
    }
}