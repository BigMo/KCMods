using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bundling.Steam
{
    public class VDFFile
    {
        #region VARIABLES
        private readonly Regex regNested = new Regex(@"\""(.*?)\""");
        private readonly Regex regValuePair = new Regex(@"\""(.*?)\""\s*\""(.*?)\""");
        #endregion

        #region PROPERTIES
        public List<VDFElement> RootElements { get; set; }
        #endregion

        #region CONSTRUCTORS
        public VDFFile(string filePath)
        {
            RootElements = new List<VDFElement>();
            Parse(filePath);
        }
        #endregion

        #region METHODS
        public string ToVDF()
        {
            StringBuilder builder = new StringBuilder();
            foreach (VDFElement child in RootElements)
                builder.Append(child.ToVDF());
            return builder.ToString();
        }
        private void Parse(string filePath)
        {
            VDFElement currentLevel = null;
            using (var reader = new StreamReader(filePath))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    string[] parts = line.Split('"');

                    if (regValuePair.Match(line).Success)
                    {
                        VDFElement subElement = new VDFElement();
                        subElement.Name = parts[1];
                        subElement.Value = parts[3];
                        subElement.Parent = currentLevel;
                        if (currentLevel == null)
                            RootElements.Add(subElement);
                        else
                            currentLevel.Children.Add(subElement);
                    }
                    else if (regNested.Match(line).Success)
                    {
                        VDFElement nestedElement = new VDFElement();
                        nestedElement.Name = parts[1];
                        nestedElement.Parent = currentLevel;
                        if (currentLevel == null)
                            RootElements.Add(nestedElement);
                        else
                            currentLevel.Children.Add(nestedElement);
                        currentLevel = nestedElement;
                    }
                    else if (line == "}")
                    {
                        currentLevel = currentLevel.Parent;
                    }
                    /*else if (line == "{")
                    {
                        //Nothing to do here
                    }*/
                }
            }
        }
        #endregion

        #region OPERATORS
        public VDFElement this[int key]
        {
            get
            {
                return RootElements[key];
            }
        }
        public VDFElement this[string key]
        {
            get
            {
                return RootElements.First(x => x.Name == key);
            }
        }
        #endregion
    }
}
