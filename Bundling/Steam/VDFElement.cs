using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bundling.Steam
{
    public class VDFElement
    {
        #region CONSTANTS
        private const string EMPTY = "<empty>";
        #endregion

        #region PROPERTIES
        public List<VDFElement> Children { get; set; }
        public VDFElement Parent { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        #endregion

        #region CONSTRUCTOR
        public VDFElement()
        {
            Children = new List<VDFElement>();
            Value = EMPTY;
        }
        #endregion

        #region OPERATORS
        public VDFElement this[int key]
        {
            get
            {
                return Children[key];
            }
        }
        public VDFElement this[string key]
        {
            get
            {
                return Children.First(x => x.Name == key);
            }
        }
        #endregion

        #region METHODS
        public override string ToString()
        {
            return string.Format("[{0}: {1}, {2} children]", Name, Value, Children.Count);
        }
        public string ToList(int indent = 0)
        {
            StringBuilder builder = new StringBuilder();
            string tabs = new string(' ', indent);
            if (Value != EMPTY)
            {
                builder.AppendFormat("{2}{0} = {1}\n", Name, Value, tabs);
            }
            else
            {
                builder.AppendFormat("{0}{1}\n", tabs, Name);
                foreach (VDFElement child in Children)
                    builder.Append(child.ToList(indent + 1));
            }
            return builder.ToString();
        }
        public string ToVDF(int indent = 0)
        {
            StringBuilder builder = new StringBuilder();
            string tabs = new string('\t', indent);
            if (Value != EMPTY)
            {
                builder.AppendFormat("{2}\"{0}\"\t\t\"{1}\"\n", Name, Value, tabs);
            }
            else
            {
                builder.AppendFormat("{0}{1}\n{0}{2}\n", tabs, Name, "{");
                foreach (VDFElement child in Children)
                    builder.Append(child.ToVDF(indent + 1));
                builder.AppendFormat("{0}{1}\n", tabs, "}");
            }
            return builder.ToString();
        }
        public bool ContainsElement(string key)
        {
            return Children.Any(x => x.Name == key);
        }
        public bool ContainsValue(string key)
        {
            return Children.Any(x => x.Value == key);
        }
        #endregion
    }
}
