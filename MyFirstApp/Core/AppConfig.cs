using System;
using System.IO;

namespace MyFirstApp.Core
{
    public static class AppConfig
    {
        // Simple text file to remember settings between restarts
        private static string ConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wiredgeist.config");

        public static float VoxelSize { get; private set; } = 0.5f; // Default

        static AppConfig()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    string txt = File.ReadAllText(ConfigFile);
                    if (float.TryParse(txt, out float val))
                    {
                        VoxelSize = val;
                    }
                }
            }
            catch { }
        }

        public static void Save(float size)
        {
            try
            {
                File.WriteAllText(ConfigFile, size.ToString());
                VoxelSize = size;
            }
            catch { }
        }
    }
}