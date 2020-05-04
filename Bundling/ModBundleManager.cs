using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bundling
{
    public class ModBundleManager
    {
        private FileInfo definitionsFile;
        private List<ModBundleDefinition> modBundles;

        public ModBundleManager(FileInfo definitionsFile)
        {
            this.definitionsFile = definitionsFile;
            if (!definitionsFile.Exists) throw new FileNotFoundException("Failed to parse mod bundle definitions; file not found", definitionsFile.FullName);
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

        public void Bundle(ModBundleDefinition definition)
        {
            Console.WriteLine($"[#] Bundling {definition.bundleName}...");
            var usings = new List<string>();
            var code = new StringBuilder();
            BundleModCode(definition, usings, code);

            var fileName = $"{definition.bundleName}.Bundled.cs";
            foreach (var chr in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(chr, '.');

            if (definition.targetDirectory == null) throw new Exception("Target directory unspecified!");
            var targetDir = new DirectoryInfo(definition.targetDirectory);
            if (!targetDir.Exists)
            {
                Console.WriteLine($"[#] Creating target directory \"{targetDir.FullName}\"...");
                targetDir.Create();
            }

            //Source code
            var targetFile = new FileInfo(Path.Combine(targetDir.FullName, fileName));
            Console.WriteLine($"[#] Writing code to \"{fileName}\"...");
            using (var writer = new StreamWriter(targetFile.FullName, false))
            {
                writer.WriteLine($"/* Bundled at {DateTime.Now.Year}-{DateTime.Now.Month.ToString("00")}-{DateTime.Now.Day.ToString("00")} */");
                usings.Sort();
                foreach (var use in usings) writer.WriteLine(use);
                writer.WriteLine(code.ToString());
            }

            //Assets
            if (definition.assetBundleSource != null)
            {
                var sourceDir = new DirectoryInfo(definition.assetBundleSource);
                if (!sourceDir.Exists) throw new Exception("Assets source directory does not exist!");
                var assetsDirectory = new DirectoryInfo(Path.Combine(targetDir.FullName, "Assets"));
                if (assetsDirectory.Exists)
                {
                    Console.WriteLine($"[#] Removing (clearing) Assets directory \"{assetsDirectory.FullName}\"...");
                    assetsDirectory.Delete(true);
                }
                Console.WriteLine($"[#] Creating Assets directory \"{assetsDirectory.FullName}\"...");
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
                Console.WriteLine($"[#->] Creating Assets platform directory \"{dst.FullName}\"...");
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
                    Console.WriteLine($"[#->] Creating Assets platform directory \"{dstFile.Directory.Name}\"...");
                    dstFile.Directory.Create();
                }
                if (dstFile.Exists) dstFile.Delete();
                File.Copy(srcFile.FullName, dstFile.FullName);
            }
        }

        private void BundleModCode(ModBundleDefinition definition, List<string> usings, StringBuilder code)
        {
            if (definition.dependencies != null)
                foreach (var dependency in definition.dependencies)
                    BundleModCode(GetModBundleDefinition(dependency), usings, code);

            // Sources
            Console.WriteLine($"[#->] Bundling {definition.bundleName}'s sources...");
            var dir = new DirectoryInfo(definition.sourceDirectory);
            if (!dir.Exists) throw new DirectoryNotFoundException("Failed to traverse source directory; directory not found");
            var files = dir.GetFiles("*.cs", SearchOption.AllDirectories);
            if (definition.excludePatterns != null) files = files.Where(f => !definition.excludePatterns.Any(e => Regex.IsMatch(f.FullName, e))).ToArray();
            foreach (var file in files)
                ProcessCodeFile(file, usings, code);
        }

        private void ProcessCodeFile(FileInfo file, List<string> usings, StringBuilder code)
        {
            Console.WriteLine($"[#-->] Parsing \"{file.Name}\"...");
            int lineNumber = 0;
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
                                code.AppendLine(line);
                            }
                        }
                        else
                        {
                            code.AppendLine(line);
                        }
                    }
                }
            }
        }
    }
}
