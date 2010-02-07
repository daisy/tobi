using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using urakawa.core;
using DtdSharp;

namespace UrakawaDomValidation
{
    public class UrakawaDomValidationReportItem
    {
        public enum ReportItemType
        {
            Trace,
            Error
        } ;

        public TreeNode Node {get; private set;}
        public string Message { get; private set; }
        public ReportItemType ItemType { get; private set; }
        
        public UrakawaDomValidationReportItem(TreeNode node, string message, ReportItemType type)
        {
            Node = node;
            Message = message;
            ItemType = type;
        }
    }
    public class UrakawaDomValidation
    {
        private DTD m_Dtd;
        private Hashtable m_DtdElementRegex;
        public List<UrakawaDomValidationReportItem> ValidationReportItems { get; private set; }
        
        public UrakawaDomValidation()
        {
            ValidationReportItems = new List<UrakawaDomValidationReportItem>();
        }
        public void UseDtd(StreamReader dtd)
        {
            DTDParser parser = new DTDParser(dtd);
            m_Dtd = parser.Parse(true);
            ParseDtdIntoHashmap();
        }
        public void UseCachedDtd(StreamReader dtd)
        {
            DtdCache.ReadFromCache(dtd);
        }
        public void SaveCachedDtd(StreamWriter dtd)
        {
            DtdCache.WriteToCache(m_DtdElementRegex, dtd);
        }
        //utility function
        public string GetRegex(TreeNode node)
        {
            return GetRegex(node.GetXmlElementQName().LocalName);
        }
        public string GetRegex(string nodename)
        {
            Regex regex = (Regex)m_DtdElementRegex[nodename];
            return regex != null ? regex.ToString() : "";
        }

        //take a DtdSharp data structure and create a hashmap where 
        //key: element name
        //value: regex representing the allowed children
        private void ParseDtdIntoHashmap()
        {
            m_DtdElementRegex = new Hashtable();

            foreach(DictionaryEntry entry in m_Dtd.Elements)
            {
                DTDElement dtdElement = (DTDElement) entry.Value;
                string regexStr = GenerateRegexForAllowedChildren(dtdElement.Content);
                Regex regex = new Regex(regexStr);
                m_DtdElementRegex.Add(dtdElement.Name, regex);
            }
        }
        
        private static string GenerateRegexForAllowedChildren(DTDItem dtdItem)
        {
            string regexStr = "";

            if (dtdItem is DTDAny)
            {
                regexStr += "";// "Any";
            }
            else if (dtdItem is DTDEmpty)
            {
                regexStr += "";
            }
            else if (dtdItem is DTDName)
            {
                regexStr += "(?:" + ((DTDName)dtdItem).Value + "#)";
                //regexStr += "(" + ((DTDName)dtdItem).Value + "#)";
            }
            else if (dtdItem is DTDChoice)
            {
                List<DTDItem> items = ((DTDChoice)dtdItem).Items;
                //if (items.Count > 1) regexStr += "(?:";
                if (items.Count > 1) regexStr += "(";

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst) regexStr += "|";
                    isFirst = false;
                    regexStr += GenerateRegexForAllowedChildren(item);
                }
                if (items.Count > 1) regexStr += ")";
            }
            else if (dtdItem is DTDSequence)
            {
                List<DTDItem> items = ((DTDSequence)dtdItem).Items;
                if (items.Count > 1) regexStr += "(?:";
                //if (items.Count > 1) regexStr += "(";

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst) regexStr += "";
                    regexStr += GenerateRegexForAllowedChildren(item);
                    isFirst = false;
                }
                if (items.Count > 1) regexStr += ")";
            }
            else if (dtdItem is DTDMixed)
            {
                List<DTDItem> items = ((DTDMixed)dtdItem).Items;
                if (items.Count > 1) regexStr += "(?:";
                //if (items.Count > 1) regexStr += "(";

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst) regexStr += "|";
                    regexStr += GenerateRegexForAllowedChildren(item);
                    isFirst = false;
                }
                if (items.Count > 1) regexStr += ")";
            }
            else if (dtdItem is DTDPCData)
            {
                regexStr +=  "#PCDATA";
            }
            else
            {
                regexStr += "**UNKNOWN**";
            }
            if (dtdItem.Cardinal == DTDCardinal.ZEROONE)
            {
                regexStr += "?";
            }
            else if (dtdItem.Cardinal == DTDCardinal.ZEROMANY)
            {
                regexStr += "*";
            }
            else if (dtdItem.Cardinal == DTDCardinal.ONEMANY)
            {
                regexStr += "+";
            }
            return regexStr;
        }
        
        //the recursive function
        public bool Validate(TreeNode node)
        {
            bool result = ValidateNodeContent(node);
            foreach (TreeNode child in node.Children.ContentsAs_ListAsReadOnly)
            {
                result = result & Validate(child);
            }
            return result;
        }
        //check a single node
        private bool ValidateNodeContent(TreeNode node)
        {
            if (node.HasXmlProperty)
            {
                string childrenNames = GetChildrenNames(node);
                Regex regExp = (Regex) m_DtdElementRegex[node.GetXmlElementQName().LocalName];
                if (regExp == null)
                {
                    string msg = string.Format("Definition for {0} not found", node.GetXmlElementQName().LocalName);
                    UrakawaDomValidationReportItem error = new UrakawaDomValidationReportItem(node, msg, UrakawaDomValidationReportItem.ReportItemType.Error);
                    ValidationReportItems.Add(error);
                    return false;
                }

                Match match = regExp.Match(childrenNames);
                string message = string.Format("Children:{0}\nMatch result:{1}", childrenNames, match.ToString());
                UrakawaDomValidationReportItem.ReportItemType type;

                if (match.Success && match.ToString() == childrenNames)
                {
                    type = UrakawaDomValidationReportItem.ReportItemType.Trace;
                }
                else
                {
                    type = UrakawaDomValidationReportItem.ReportItemType.Error;
                }
                UrakawaDomValidationReportItem report = new UrakawaDomValidationReportItem(node, message, type);
                ValidationReportItems.Add(report);
                return type != UrakawaDomValidationReportItem.ReportItemType.Error;
            }
            else //no XML property
            {
                return true;
            }
        }

        private static string GetChildrenNames(TreeNode node)
        {
            string names = "";

            if (node.GetTextMedia() != null)
            {
                names += "#PCDATA";
            }
            foreach (TreeNode child in node.Children.ContentsAs_ListAsReadOnly)
            {
                if (child.HasXmlProperty)
                {
                    names += child.GetXmlElementQName().LocalName + "#";
                }
            }
            return names;
        }
    }
}
