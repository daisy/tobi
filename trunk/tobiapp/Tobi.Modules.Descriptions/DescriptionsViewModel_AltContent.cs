using System;
using System.Collections.Generic;
using Tobi.Common.MVVM;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.property.alt;
using urakawa.xuk;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsViewModel
    {
        public AlternateContent AddDescription(string uid, string descriptionName)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return null;

            //var altProp = node.GetOrCreateAlternateContentProperty();
            //if (altProp == null) return;

            AlternateContent altContent = node.Presentation.AlternateContentFactory.CreateAlternateContent();

            AlternateContentAddCommand cmd1 =
                node.Presentation.CommandFactory.CreateAlternateContentAddCommand(node, altContent);
            node.Presentation.UndoRedoManager.Execute(cmd1);

            RaisePropertyChanged(() => Descriptions);

            if (!string.IsNullOrEmpty(uid))
            {
                AddMetadata(null, altContent, XmlReaderWriterHelper.XmlId, uid);
            }
            if (!string.IsNullOrEmpty(descriptionName))
            {
                AddMetadata(null, altContent, DiagramContentModelHelper.DiagramElementName, descriptionName);
            }

            return altContent;
        }

        public void RemoveDescription(AlternateContent altContent)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            AlternateContentRemoveCommand cmd1 =
                node.Presentation.CommandFactory.CreateAlternateContentRemoveCommand(node, altContent);
            node.Presentation.UndoRedoManager.Execute(cmd1);

            RaisePropertyChanged(() => Descriptions);
        }


        private AlternateContent m_SelectedAlternateContent;
        public void SetSelectedAlternateContent(AlternateContent altContent)
        {
            m_SelectedAlternateContent = altContent;
            RaisePropertyChanged(() => Descriptions);
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptions
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                //return new ObservableCollection<Metadata>(altProp.Metadatas.ContentsAs_Enumerable);
                return altProp.AlternateContents.Count > 0;
            }
        }

        public IEnumerable<AlternateContent> Descriptions //ObservableCollection
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return null;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return null;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return null;

                //return new ObservableCollection<Metadata>(altProp.Metadatas.ContentsAs_Enumerable);
                return altProp.AlternateContents.ContentsAs_Enumerable;
            }
        }

        private bool getValidationText_Descriptions(ref string message)
        {
            bool first = true;

            int count = 0;
            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_LondDesc))
            {
                count++;
            }
            if (count ==0)
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- No long description is specified.";
                }
            }
            else if (count > 1)
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- There are more than one long description (";
                    message += count;
                    message += ")";
                }
            }
            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_LondDesc))
            {
                if (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- The long description text is missing.";
                    }

                    break;
                }
            }

            count = 0;
            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Summary))
            {
                count++;
            }
            if (count == 0)
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Specifying a summary is recommended.";
                }
            }
            else if (count > 1)
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- There are more than one summary (";
                    message += count;
                    message += ")";
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Summary))
            {
                if (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- The summary text is missing.";
                    }

                    break;
                }
            }

            count = 0;
            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedLanguageDescription))
            {
                count++;
            }
            if (count > 1)
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- There are more than one simplified language (";
                    message += count;
                    message += ")";
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedLanguageDescription))
            {
                if (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "-The simplified language text is missing.";
                    }

                    break;
                }
            }

            count = 0;
            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedImage))
            {
                count++;
            }
            if (count > 1)
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- There are more than one simplified image (";
                    message += count;
                    message += "), which will be grouped if they have the same identifier and share the same tour.";
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedImage))
            {
                if (altContent.Image == null)
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- Image is missing for simplified image.";
                    }

                    break;
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedImage))
            {
                if (altContent.Image != null
                    && (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text)))
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- Tour text is recommended for for simplified image.";
                    }

                    break;
                }
            }

            count = 0;
            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Tactile))
            {
                count++;
            }
            if (count > 1)
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- There are more than one tactile image (";
                    message += count;
                    message += "), which will be grouped if they have the same identifier and share the same tour.";
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Tactile))
            {
                if (altContent.Image == null)
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- Image is missing for tactile image.";
                    }

                    break;
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Tactile))
            {
                if (altContent.Image != null
                    && (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text)))
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- Tour text is recommended for for tactile image.";
                    }

                    break;
                }
            }

            string strUnknownDIAGRAMs = "";
            foreach (var id in GetUnknownDIAGRAMnames())
            {
                strUnknownDIAGRAMs += "[";
                strUnknownDIAGRAMs += id;
                strUnknownDIAGRAMs += "]";
            }

            if (!string.IsNullOrEmpty(strUnknownDIAGRAMs))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Unknown DIAGRAM elements: ";
                    message += strUnknownDIAGRAMs;
                }
            }

            string strInvalidDIAGRAMs = "";
            foreach (var id in GetInvalidDIAGRAMnames())
            {
                strInvalidDIAGRAMs += "[";
                strInvalidDIAGRAMs += id;
                strInvalidDIAGRAMs += "]";
            }

            if (!string.IsNullOrEmpty(strInvalidDIAGRAMs))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Invalid syntax for DIAGRAM elements: ";
                    message += strInvalidDIAGRAMs;
                }
            }

            string strInvalidIDS = "";
            foreach (var id in GetInvalidIDs(false, true))
            {
                strInvalidIDS += "[";
                strInvalidIDS += id;
                strInvalidIDS += "]";
            }

            if (!string.IsNullOrEmpty(strInvalidIDS))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Some identifiers are invalid: ";
                    message += strInvalidIDS;
                }
            }

            string strDupIDS = "";
            foreach (var id in GetDuplicatedIDs(false, true))
            {
                strDupIDS += "[";
                strDupIDS += id;
                strDupIDS += "]";
            }

            if (!string.IsNullOrEmpty(strDupIDS))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Some identifiers are duplicated (this may be valid if used for grouping image objects): ";
                    message += strDupIDS;
                }
            }

            string strMissingIDS = "";
            foreach (var id in GetReferencedMissingIDs(false, true))
            {
                strMissingIDS += "[";
                strMissingIDS += id;
                strMissingIDS += "]";
            }

            if (!string.IsNullOrEmpty(strMissingIDS))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Some identifiers are referenced, but are missing: ";
                    message += strMissingIDS;
                }
            }

            string strInvalidLangs = "";
            foreach (var id in GetInvalidLanguageTags(false, true))
            {
                strInvalidLangs += "[";
                strInvalidLangs += id;
                strInvalidLangs += "]";
            }

            if (!string.IsNullOrEmpty(strInvalidLangs))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Some language tags are invalid: ";
                    message += strInvalidLangs;
                }
            }

            bool hasMessages = !first;
            return hasMessages;
        }

        [NotifyDependsOn("ValidationText_Descriptions")]
        public bool HasValidationWarning_Descriptions
        {
            get
            {
                string str = null;
                return getValidationText_Descriptions(ref str);
            }
        }

        [NotifyDependsOn("Descriptions")]
        public string ValidationText_Descriptions
        {
            get
            {
                string str = "";
                if (HasValidationWarning_Descriptions)
                {
                    getValidationText_Descriptions(ref str);
                }
                return str;
            }
        }
    }
}
