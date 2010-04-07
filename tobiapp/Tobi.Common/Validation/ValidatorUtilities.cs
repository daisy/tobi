using urakawa.core;

namespace Tobi.Common.Validation
{
    /// <summary>
    /// Static functions useful to validators (and perhaps others too, we'll see; for now they can live here)
    /// </summary>
    public class ValidatorUtilities
    {
        //returns a 1-child-deep "flat" xml representation
        public static string GetNodeXml(TreeNode node, bool flat)
        {
            if (node == null) return "";
            if (flat)
                return GetNodeXml_Flat(node);
            else
                return GetNodeXml_Deep(node, 0);
        }

        private static string GetNodeXml_Flat(TreeNode node)
        {
            string xml = "";
            string nodeName = GetTreeNodeName(node);
            xml = string.Format("<{0}>", nodeName);

            if (node.GetTextMedia() != null)
            {
                if (node.GetTextMedia().Text.Length > 10)
                {
                    xml += node.GetTextMedia().Text.Substring(0, 10);
                    xml += "...";
                }
                else
                {
                    xml += node.GetTextMedia().Text;
                }
            }

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                string childNodeText = GetTreeNodeTextExcerpt(child);
                string childNodeName = GetTreeNodeName(child);
                xml += string.Format("\n\t<{0}>{1}</{0}>", childNodeName, childNodeText);
            }

            xml += string.Format("\n</{0}>", nodeName);
            return xml;
        }

        private static string GetNodeXml_Deep(TreeNode node, int level)
        {
            string xml = "";
            string nodeName = "";
            string indent = new string('\t', level);
            if (node.GetXmlElementQName() != null)
            {
                nodeName = node.GetXmlElementQName().LocalName;
                xml += string.Format("\n{0}<{1}>", indent, nodeName);
            }

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                xml += GetNodeXml_Deep(child, level++);
            }

            if (!string.IsNullOrEmpty(nodeName))
                xml += string.Format("\n{0}</{1}>", indent, nodeName);
            return xml;
        }

        //static helper functions
        public static string GetTreeNodeName(TreeNode node)
        {
            string nodeName = GetNearestTreeNodeName(node);
            if (string.IsNullOrEmpty(nodeName)) return "";
            return nodeName;
        }
        public static string GetNearestTreeNodeName(TreeNode node)
        {
            if (node.GetXmlElementQName() == null)
            {
                if (node.Parent != null)
                    return GetNearestTreeNodeName(node.Parent);
                else
                    return "";
            }

            return node.GetXmlElementQName().LocalName;
        }

        public static string GetTreeNodeTextExcerpt(TreeNode node)
        {

            string nodeText = node.GetTextMediaFlattened(false);
            if (nodeText == null) return "";
            if (nodeText.Length > 100)
                nodeText = nodeText.Substring(0, 100);

            nodeText += "...";

            //if this is a mixed content model node with previous sibling(s), add some ellipses before the text too
            if (node.GetXmlElementQName() == null && node.PreviousSibling != null)
                nodeText = "...\n" + nodeText;

            return nodeText;
        }

    }

}
