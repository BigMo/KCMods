using Bundling.Steam;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Bundling
{
    public class ModBundleManager
    {
        private readonly List<ModBundleDefinition> modBundles;
        private readonly DirectoryInfo kcDirectory;

        public ModBundleManager(FileInfo definitionsFile)
        {
            var steam = new SteamManager();
            var kcApp = steam.GetAppByID(569480) ?? steam.GetAppByName("Kingdoms and Castles");
            if (kcApp == null) throw new Exception("Unable to find K&C install directory; steam version required!");
            kcDirectory = new DirectoryInfo(kcApp.Path);
            if (!kcDirectory.Exists) throw new Exception("K&C install directory does not exist!");
            if (!definitionsFile.Exists)
                throw new FileNotFoundException("Failed to parse mod bundle definitions; file not found", definitionsFile.FullName);
            modBundles = JsonConvert.DeserializeObject<List<ModBundleDefinition>>(File.ReadAllText(definitionsFile.FullName));
        }

        public ModBundleDefinition GetModBundleDefinition(string name)
        {
            return modBundles.FirstOrDefault(d => d.bundleName == name);
        }

        public static void CreateSampleFile(FileInfo definitionsFile)
        {
            var definition = new ModBundleDefinition()
            {
                bundleName = "Name.Of.Your.Bundle",
                sourceDirectory = @"Path\To\Your\Sources",
                assetBundleName = "Name.Of.Your.Assets",
                assetBundleSource = @"Path\To\Your\Assets",
                dependencies = new string[] {"BundlesYouDependOn"}
            };
            File.WriteAllText(definitionsFile.FullName, JsonConvert.SerializeObject(new ModBundleDefinition[] { definition }));
        }

        public void BundleAll()
        {
            foreach (var bundle in modBundles.Where(b => b.deploy))
                Bundle(bundle);
        }

        public void Bundle(ModBundleDefinition definition)
        {
            Debug.WriteLine($"[#] Bundling {definition.bundleName}...");
            var usings = new List<string>();
            var code = new StringBuilder();
            var sourceFileSize = 0L;
            BundleModCode(definition, usings, code, ref sourceFileSize);

            var fileName = $"{definition.bundleName}.Bundled.cs";
            foreach (var chr in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(chr, '.');

            if (definition.targetDirectory == null) throw new Exception("Target directory unspecified!");
            var targetDir = new DirectoryInfo(Path.Combine(kcDirectory.FullName, "KingdomsAndCastles_Data", "mods", definition.targetDirectory));
            if (!targetDir.Exists)
            {
                Debug.WriteLine($"[#] Creating target directory \"{targetDir.FullName}\"...");
                targetDir.Create();
            }

            //Source code
            var targetFile = new FileInfo(Path.Combine(targetDir.FullName, fileName));
            Debug.WriteLine($"[#] Writing code to \"{fileName}\"...");
            using (var writer = new StreamWriter(targetFile.FullName, false))
            {
                writer.WriteLine("/* Bundled at {0}-{1} using {2} {3}*/",
                    $"{DateTime.Now.Year}-{DateTime.Now.Month.ToString("00")}",
                    $"{DateTime.Now.Day.ToString("00")}",
                    Assembly.GetExecutingAssembly().GetName().Name,
                    Assembly.GetExecutingAssembly().GetName().Version);
                usings.Sort();
                foreach (var use in usings) writer.WriteLine(use);
                writer.WriteLine(code.ToString());
            }
            var targetFileSize = targetFile.Length;
            var decrease = 1 - ((double)targetFileSize / (double)sourceFileSize);
            Debug.WriteLine($"[#->] Decreased total code size by {(decrease * 100).ToString("0.00")}% (before: {sourceFileSize} bytes, after: {targetFileSize} bytes)");

            //Assets
            if (definition.assetBundleSource != null)
            {
                var sourceDir = new DirectoryInfo(definition.assetBundleSource);
                if (!sourceDir.Exists) throw new Exception("Assets source directory does not exist!");
                var assetsDirectory = new DirectoryInfo(Path.Combine(targetDir.FullName, "Assets"));
                if (assetsDirectory.Exists)
                {
                    Debug.WriteLine($"[#] Removing (clearing) Assets directory \"{assetsDirectory.FullName}\"...");
                    assetsDirectory.Delete(true);
                }
                Debug.WriteLine($"[#] Creating Assets directory \"{assetsDirectory.FullName}\"...");
                assetsDirectory.Create();

                CopyPlatformFiles("linux", definition.assetBundleName, sourceDir, assetsDirectory);
                CopyPlatformFiles("osx", definition.assetBundleName, sourceDir, assetsDirectory);
                CopyPlatformFiles("win32", definition.assetBundleName, sourceDir, assetsDirectory);
                CopyPlatformFiles("win64", definition.assetBundleName, sourceDir, assetsDirectory);
            }
        }

        private void CopyPlatformFiles(string platformName, string assetsName, DirectoryInfo sourceDirectory, DirectoryInfo assetsDirectory)
        {
            var src = new DirectoryInfo(Path.Combine(sourceDirectory.FullName, platformName));
            var dst = new DirectoryInfo(Path.Combine(assetsDirectory.FullName, platformName));
            if (!src.Exists) throw new Exception($"Missing platform \"{platformName}\"!");
            if (!dst.Exists)
            {
                Debug.WriteLine($"[#->] Creating Assets platform directory \"{dst.FullName}\"...");
                dst.Create();
            }

            var platformFiles = new FileInfo[]
            {
                new FileInfo(Path.Combine(src.FullName, $"{platformName}")),
                new FileInfo(Path.Combine(src.FullName, $"{platformName}.manifest"))
            };
            var contentFiles = src.GetFiles()
                .Where(f => f.Name.StartsWith($"{assetsName}_"))
                .Where(f => !f.Name.EndsWith(".meta"))
                .OrderByDescending(f => f.CreationTime)
                .ToArray();
            if (contentFiles.Length == 0) throw new Exception($"Missing content files for asset bundle \"{assetsName}\" in platform {platformName}");
            var contentManifestFile = new FileInfo(Path.Combine(src.FullName, $"{assetsName}.manifest"));
            var allFiles = new FileInfo[]
            {
                platformFiles[0],
                platformFiles[1],
                contentFiles[0],
                contentManifestFile
            };
            var missing = allFiles.Where(f => !f.Exists).ToArray();
            if (missing.Length != 0) throw new Exception($"Missing files for {platformName}: {string.Join(", ", missing.Select(f => f.Name).ToArray())}");
            foreach(var srcFile in allFiles)
            {
                var dstFile = new FileInfo(srcFile.FullName.Replace(sourceDirectory.FullName, assetsDirectory.FullName));
                if (!dstFile.Directory.Exists)
                {
                    Debug.WriteLine($"[#->] Creating Assets platform directory \"{dstFile.Directory.Name}\"...");
                    dstFile.Directory.Create();
                }
                if (dstFile.Exists) dstFile.Delete();
                File.Copy(srcFile.FullName, dstFile.FullName);
            }
        }

        private void BundleModCode(ModBundleDefinition definition, List<string> usings, StringBuilder code, ref long totalFileSize)
        {
            if (definition.dependencies != null)
                foreach (var dependency in definition.dependencies)
                    BundleModCode(GetModBundleDefinition(dependency), usings, code, ref totalFileSize);

            // Sources
            Debug.WriteLine($"[#->] Bundling {definition.bundleName}'s sources...");
            var dir = new DirectoryInfo(definition.sourceDirectory);
            if (!dir.Exists) throw new DirectoryNotFoundException("Failed to traverse source directory; directory not found");
            var files = dir.GetFiles("*.cs", SearchOption.AllDirectories);
            if (definition.excludePatterns != null) files = files.Where(f => !definition.excludePatterns.Any(e => Regex.IsMatch(f.FullName, e))).ToArray();
            foreach (var file in files)
                ProcessCodeFile(file, usings, code, definition.minify, ref totalFileSize);
        }

        private void ProcessCodeFile(FileInfo file, List<string> usings, StringBuilder code, bool minify, ref long totalFileSize)
        {
            Debug.WriteLine($"[#-->] Parsing \"{file.Name}\"...");
            int lineNumber = 0;
            totalFileSize += file.Length;
            using (var str = file.OpenRead())
            {
                using (var reader = new StreamReader(str))
                {
                    bool parsingUsings = true;
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        var trimmed = line.Trim();
                        var isUsing = trimmed.StartsWith("using ");
                        if (parsingUsings)
                        {
                            if (isUsing)
                            {
                                if (trimmed.Contains("=")) throw new Exception($"Detected \"=\" in using statement in {file.FullName}[{lineNumber}]");
                                if (!usings.Contains(trimmed)) usings.Add(trimmed);
                            }
                            else
                            {
                                parsingUsings = false;
                                if (minify) Minify(line, code);
                                else code.AppendLine(line);
                            }
                        }
                        else
                        {
                            if (minify) Minify(line, code);
                            else code.AppendLine(line);
                        }
                    }
                }
            }
        }

        private void Minify(string line, StringBuilder code)
        {
            var trimmed = line.Trim();
            if (
                trimmed.StartsWith("//") ||
                trimmed.Length == 0 ||
                trimmed.StartsWith("#region") ||
                trimmed.StartsWith("#endregion")
            ) return;

            code.Append($"{trimmed} ");
            return;

            if (
                trimmed == "{" || trimmed == "}" ||
                trimmed == "(" || trimmed == ")" ||
                trimmed.StartsWith("else") || trimmed.StartsWith("catch") || 
                trimmed.EndsWith(";") ||
                trimmed.EndsWith(",") ||
                trimmed.EndsWith(")") || trimmed.EndsWith("(") ||
                trimmed.EndsWith("{") || trimmed.EndsWith("}"))
                code.Append($" {trimmed}");
            else
                code.Append($"\n{line}");
        }
    }
}
