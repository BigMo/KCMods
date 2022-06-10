using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Bundling
{
    class Program
    {
        private const string DEFINITIONSFILE = @"..\..\..\Scripts\mod_bundles.json";

        static void Main(string[] args)
        {
            var definitionsFile = new FileInfo(DEFINITIONSFILE);
            System.Environment.CurrentDirectory = definitionsFile.DirectoryName;

            if (args.Length < 1)
            {
                Debug.WriteLine("Usage: Bundling <bundlename> [<bundlename>, ...]");
                return;
            }

            //try
            //{
                var bundles = new ModBundleManager(definitionsFile);
                if (args[0] == "--all")
                    bundles.BundleAll();
                else
                    foreach(var bundleName in args)
                        bundles.Bundle(bundles.GetModBundleDefinition(bundleName));

                Debug.WriteLine("Done!");
            //}
            //catch(FileNotFoundException)
            //{
            //    Debug.WriteLine("No definitions found; creating example file");
            //    ModBundleManager.CreateSampleFile(definitionsFile);
            //}
            //catch(Exception ex)
            //{
            //    PrintExceptionRecursive(ex);
            //}
        }

        private static void PrintExceptionRecursive(Exception ex)
        {
            Debug.WriteLine("-".PadRight(20, '-'));
            Debug.WriteLine(ex.Message);
            Debug.WriteLine("-".PadRight(20, '-'));
            Debug.WriteLine(ex.StackTrace);
            if (ex.InnerException != null) PrintExceptionRecursive(ex.InnerException);
        }
    }
}
