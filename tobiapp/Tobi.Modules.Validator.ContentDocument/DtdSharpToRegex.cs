using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AudioLib;
using DtdSharp;
using urakawa.core;

namespace Tobi.Plugin.Validator.ContentDocument
{
    /// <summary>
    /// Transform a DtdSharp DTD object into a table of regular expressions
    /// Also provides functions to store and retrive a cached version of the table
    /// </summary>
    public class DtdSharpToRegex
    {
        public const char DELIMITER = '~';
        public const char NAMESPACE_PREFIX_SEPARATOR = '@';
        public const string PCDATA = "€PCDATA€";
        public const string UNKNOWN = "€UNKNOWN€";
        
        public Dictionary<string, Regex> DtdRegexTable { get; private set; }

        public Regex GetRegex(StringBuilder strBuilder, TreeNode node)
        {
            if (DtdRegexTable == null || DtdRegexTable.Count == 0)
            {
                return null;
            }

            strBuilder.Clear();
            buildPrefixedQualifiedName(strBuilder, node);
            string key = strBuilder.ToString();

            Regex regex;
            DtdRegexTable.TryGetValue(key, out regex);

            //return DtdRegexTable[key];
            return regex;
        }

        //public void Reset()
        //{
        //    DtdRegexTable = null;
        //}

        //take a DtdSharp data structure and create a hashmap where 
        //key: element name
        //value: regex representing the allowed children
        public void ParseDtdIntoHashtable(DTD dtd)
        {
            if (DtdRegexTable == null)
            {
                DtdRegexTable = new Dictionary<string, Regex>();
            }
            var strBuilder = new StringBuilder();
            foreach (DictionaryEntry entry in dtd.Elements)
            {
                DTDElement dtdElement = (DTDElement)entry.Value;

                strBuilder.Clear();
                GenerateRegexForAllowedChildren(strBuilder, dtdElement.Content);
                string regexStr = strBuilder.ToString();
                Regex regex = new Regex(regexStr);
                string key = dtdElement.Name.Replace(':', NAMESPACE_PREFIX_SEPARATOR);

                DtdRegexTable[key] = regex;
                //DtdRegexTable.Add(key, regex);
            }
        }

        /// <summary>
        ///  Read from cache into a hashtable
        /// 
        /// Cache file format:
        /// line 1: Element name
        /// line 2: Regex string of element's allowed children
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public void ReadFromCache(StreamReader reader)
        {
            if (DtdRegexTable == null)
            {
                DtdRegexTable = new Dictionary<string, Regex>();
            }
            try
            {
                string name = reader.ReadLine();
                string regExpStr = reader.ReadLine();

                while (name != null && regExpStr != null)
                {
                    Regex regEx = new Regex(regExpStr);
                    DtdRegexTable[name] = regEx;
                    //DtdRegexTable.Add(name, regEx);
                    name = reader.ReadLine();
                    regExpStr = reader.ReadLine();
                }
            }
            catch
            {
                //DebugFix.Assert(false);
#if DEBUG
                Debugger.Break();
#endif // DEBUG
                DtdRegexTable = null;
            }
        }

        /// <summary>
        /// Write hashtable data to cache
        /// 
        /// The hashtable is formatted like:
        /// key: element name
        /// value: regex (object) of element's allowed children
        /// 
        /// Cache file format:
        /// line 1: Element name
        /// line 2: Regex string of element's allowed children
        /// </summary>
        /// <param name="writer"></param>
        public void WriteToCache(StreamWriter writer)
        {
            if (DtdRegexTable == null || DtdRegexTable.Count == 0)
            {
                return;
            }

            foreach (string key in DtdRegexTable.Keys)
            {
                string val = DtdRegexTable[key].ToString();
                writer.WriteLine(key);
                writer.WriteLine(val);
            }
        }

        private static void buildPrefixedQualifiedName(StringBuilder strBuilder, TreeNode n)
        {
            if (n.NeedsXmlNamespacePrefix())
            {
                string nsUri = n.GetXmlNamespaceUri();
                string prefix = n.GetXmlNamespacePrefix(nsUri);

                strBuilder.Append(prefix);
                strBuilder.Append(NAMESPACE_PREFIX_SEPARATOR);
            }

            strBuilder.Append(n.GetXmlElementLocalName());
        }

        //return a string list of the child node names
        //so that they are compatible with the regular expression 
        //created from the DTD
        public static string GenerateChildNameList(StringBuilder strBuilder, TreeNode node)
        {
            strBuilder.Clear();

            if (node.GetTextMedia() != null)
            {
                strBuilder.Append(PCDATA);
            }
            foreach (TreeNode child in node.Children.ContentsAs_Enumerable)
            {
                if (child.HasXmlProperty)
                {
                    buildPrefixedQualifiedName(strBuilder, child);
                    strBuilder.Append(DELIMITER);
                }
            }

            return strBuilder.ToString();
        }

        private static void GenerateRegexForAllowedChildren(StringBuilder stringBuilder, DTDItem dtdItem)
        {
            if (dtdItem is DTDAny)
            {
                stringBuilder.Append("");// "Any";
            }
            else if (dtdItem is DTDEmpty)
            {
                stringBuilder.Append("");
            }
            else if (dtdItem is DTDName)
            {
                stringBuilder.Append("(?:");
                string name = ((DTDName) dtdItem).Value;
                name = name.Replace(':', NAMESPACE_PREFIX_SEPARATOR);
                stringBuilder.Append(Regex.Escape(name));
                stringBuilder.Append(DELIMITER);
                stringBuilder.Append(")");
            }
            else if (dtdItem is DTDChoice)
            {
                List<DTDItem> items = ((DTDChoice)dtdItem).Items;
                if (items.Count > 1)
                {
                    stringBuilder.Append("(?:");
                }

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst)
                    {
                        stringBuilder.Append("|");
                    }
                    isFirst = false;
                    
                    GenerateRegexForAllowedChildren(stringBuilder, item);
                }
                if (items.Count > 1)
                {
                    stringBuilder.Append(")");
                }
            }
            else if (dtdItem is DTDSequence)
            {
                List<DTDItem> items = ((DTDSequence)dtdItem).Items;
                if (items.Count > 1)
                {
                    stringBuilder.Append("(?:");
                }

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst)
                    {
                        stringBuilder.Append("");
                    }
                    GenerateRegexForAllowedChildren(stringBuilder, item);
                    isFirst = false;
                }
                if (items.Count > 1)
                {
                    stringBuilder.Append(")");
                }
            }
            else if (dtdItem is DTDMixed)
            {
                List<DTDItem> items = ((DTDMixed)dtdItem).Items;
                if (items.Count > 1)
                {
                    stringBuilder.Append("(?:");
                }

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst)
                    {
                        stringBuilder.Append("|");
                    }
                    
                    GenerateRegexForAllowedChildren(stringBuilder, item);
                    isFirst = false;
                }
                if (items.Count > 1)
                {
                    stringBuilder.Append(")");
                }
            }
            else if (dtdItem is DTDPCData)
            {
                stringBuilder.Append(Regex.Escape(PCDATA));
            }
            else
            {

                //DebugFix.Assert(false);
#if DEBUG
                Debugger.Break();
#endif // DEBUG
                stringBuilder.Append(Regex.Escape(UNKNOWN));
            }

            if (dtdItem.Cardinal == DTDCardinal.ZEROONE)
            {
                stringBuilder.Append("?");
            }
            else if (dtdItem.Cardinal == DTDCardinal.ZEROMANY)
            {
                stringBuilder.Append("*");
            }
            else if (dtdItem.Cardinal == DTDCardinal.ONEMANY)
            {
                stringBuilder.Append("+");
            }
        }
    }
}
