using System;
using System.Diagnostics;
using System.IO;

namespace Gear_Shifting_Anim
{
    public class Utility
    {

        public static void Log(string msg)
        {
            try
            {
                string path = @"scripts/GShift/log.txt";

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var writer = new StreamWriter(path, true))
                {
                    writer.WriteLine($"{DateTime.Now:G}: {msg}.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
