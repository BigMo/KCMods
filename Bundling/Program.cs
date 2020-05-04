using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                Console.WriteLine("Usage: Bundling <bundlename>");
                return;
            }

            try
            {
                var bundles = new ModBundleManager(definitionsFile);
                bundles.Bundle(bundles.GetModBundleDefinition(args[0]));
                Console.WriteLine("Done!");
            }
            catch(FileNotFoundException)
            {
                Console.WriteLine("No definitions found; creating example file");
                ModBundleManager.CreateSampleFile(definitionsFile);
            }
            catch(Exception ex)
            {
                PrintExceptionRecursive(ex);
            }
        }

        private static void PrintExceptionRecursive(Exception ex)
        {
            Console.WriteLine("-".PadRight(20, '-'));
            Console.WriteLine(ex.Message);
            Console.WriteLine("-".PadRight(20, '-'));
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null) PrintExceptionRecursive(ex.InnerException);
        }
    }
}
