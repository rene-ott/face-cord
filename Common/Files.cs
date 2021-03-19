using System.IO;

namespace FaceCord.Common
{
    public class Files
    {
        public static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
