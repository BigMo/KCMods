using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bundling
{
    public class ModBundleDefinition
    {
        [JsonProperty("sourceDirectory")]
        public virtual string SourceDirectory { get; set; }

        [JsonProperty("targetDirectory")]
        public virtual string TargetDirectory { get; set; }

        [JsonProperty("bundleName")]
        public virtual string BundleName { get; set; }

        [JsonProperty("assetBundleSource")]
        public virtual string AssetBundleSource { get; set; }

        [JsonProperty("assetBundleName")]
        public virtual string AssetBundleName { get; set; }

        [JsonProperty("dependencies")]
        public virtual string[] Dependencies { get; set; } = new string[0];

        [JsonProperty("excludePatterns")]
        public virtual string[]  ExcludePatterns { get; set; } = new string[0];

        [JsonProperty("deploy")]
        public virtual bool Deploy { get; set; }

        [JsonProperty("minify")]
        public virtual bool Minify { get; set; }

        public override string ToString()
        {
            return BundleName;
        }
    }
}
