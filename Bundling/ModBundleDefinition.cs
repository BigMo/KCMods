using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bundling
{
    public class ModBundleDefinition
    {
        public string sourceDirectory;
        public string targetDirectory;
        public string bundleName;
        public string assetBundleSource;
        public string assetBundleName;
        public string[] dependencies;
        public string[] excludePatterns;
    }
}
