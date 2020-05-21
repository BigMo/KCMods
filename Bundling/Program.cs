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
        private const string DEFINITIONSFILE = "mod_bundles.json";

        static void Main(string[] args)
        {
            var definitionsFile = new FileInfo(DEFINITIONSFILE);

            if (args.Length != 1)
            {
                Debug.WriteLine("Usage: Bundling <bundlename>");
                return;
            }

            try
            {
                var bundles = new ModBundleManager(definitionsFile);
                if (args[0] == "--all")
                    bundles.BundleAll();
                else
                    bundles.Bundle(bundles.GetModBundleDefinition(args[0]));

                Debug.WriteLine("Done!");
            }
            catch(FileNotFoundException)
            {
                Debug.WriteLine("No definitions found; creating example file");
                ModBundleManager.CreateSampleFile(definitionsFile);
            }
            catch(Exception ex)
            {
                PrintExceptionRecursive(ex);
            }
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
