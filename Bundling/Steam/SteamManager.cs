using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Bundling.Steam
{
    public class SteamManager
    {
        private const string REG_PATH = @"HKEY_CURRENT_USER\Software\Valve\Steam";
        private const string REG_KEY = "SteamPath";

        public string InstallPath { get; private set; }
        public SteamLibrary[] SteamLibraries { get; private set; }

        public SteamManager()
        {
            InstallPath = (string)Registry.GetValue(REG_PATH, REG_KEY, null);
            if (string.IsNullOrEmpty(InstallPath))
                throw new Exception("Could not find Steam installation");

            var dir = Path.Combine(InstallPath, "steamapps");
            if (!Directory.Exists(dir))
                throw new Exception($"\"{dir}\" does not exist");

            var dirs = new List<SteamLibrary>();
            dirs.Add(new SteamLibrary(0, InstallPath));

            var vdf = Path.Combine(dir, "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                var libs = new VDFFile(vdf);
                if (libs.RootElements.Count > 0 && libs.RootElements.Any(x => x.Name == "LibraryFolders"))
                {
                    foreach (var e in libs["LibraryFolders"].Children)
                    {
                        int id = 0;
                        if (int.TryParse(e.Name, out id))
                            dirs.Add(new SteamLibrary(id, e.Value.Replace(@"\\", @"\")));
                    }
                }
            }
            SteamLibraries = dirs.ToArray();
        }

        public AppState GetAppByID(int id)
        {
            AppState app = null;
            foreach (var lib in SteamLibraries)
            {
                app = lib.Apps.FirstOrDefault(x => x.AppId == id);
                if (app != null) return app;
            }
            return null;
        }
        public AppState GetAppByName(string name)
        {
            AppState app = null;
            foreach (var lib in SteamLibraries)
            {
                app = lib.Apps.FirstOrDefault(x => x.Name == name);
                if (app != null) return app;
            }
            return null;
        }
    }
}
