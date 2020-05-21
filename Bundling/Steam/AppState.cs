using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bundling.Steam
{
    public class AppState
    {
        public int AppId { get; private set; }
        public string Name { get; private set; }
        public string SubDirectory { get; private set; }
        public string Path { get { return System.IO.Path.Combine(Library.Path, "steamapps", "common", SubDirectory); } }
        public SteamLibrary Library { get; private set; }

        public AppState(SteamLibrary lib, string path) : this(lib, new VDFFile(path)) { }

        public AppState(SteamLibrary lib, VDFFile vdf)
        {
            Library = lib;
            AppId = int.Parse(vdf["AppState"]["appid"].Value);
            Name = vdf["AppState"]["name"].Value;
            SubDirectory = vdf["AppState"]["installdir"].Value;
        }

        public override string ToString()
        {
            return $"\"{Name}\" ({AppId})";
        }
    }
}
