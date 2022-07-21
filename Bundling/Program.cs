using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Bundling
{
    class Program
    {
        private const string DEFINITIONSFILE = @"..\..\..\Scripts\mod_bundles.json";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0)
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
                    foreach (var bundleName in args)
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
            else
            {
                Application.Run(new frmMain());
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
