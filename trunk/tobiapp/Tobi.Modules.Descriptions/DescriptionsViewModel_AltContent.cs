using System;
using System.Collections.Generic;
using Tobi.Common.MVVM;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.daisy.export;
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

            var altProp = node.GetAlternateContentProperty();
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

                AlternateContentProperty altProp = node.GetAlternateContentProperty();
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

                AlternateContentProperty altProp = node.GetAlternateContentProperty();
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
                    message += Tobi_Plugin_Descriptions_Lang.LongDescMissing;
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
                    message += Tobi_Plugin_Descriptions_Lang.LongDescMoreThanOne;
                    message += "(";
                    message += count;
                    message += ")";
                }
            }

            //foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_LondDesc))
            //{
            //    if (altContent.Audio != null && altContent.Text == null)
            //    {
            //        if (!first)
            //        {
            //            if (message != null)
            //            {
            //                message += "\n";
            //            }
            //        }
            //        first = false;
            //        if (message != null)
            //        {
            //            message += "- The long description has audio but no corresponding text.";
            //        }

            //        break;
            //    }
            //}

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_LondDesc))
            {
                if (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
                {
                    bool otherDataInAdvancedMode = altContent.Audio != null
                                                   || Daisy3_Export.AltContentHasSignificantMetadata(altContent);
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
                        message += Tobi_Plugin_Descriptions_Lang.LongDescTextMissing;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "] ";
                        }
                        if (otherDataInAdvancedMode)
                        {
                            message += " ";
                            message += Tobi_Plugin_Descriptions_Lang.LongDescHasOtherData;
                        }
                    }
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_LondDesc))
            {
                if (altContent.Image != null)
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
                        message += Tobi_Plugin_Descriptions_Lang.LongDescNoImage;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "]";
                        }
                    }
                }
            }
            count = 0;
            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Summary))
            {
                count++;
            }
            if (count == 0)
            {
                //if (!first)
                //{
                //    if (message != null)
                //    {
                //        message += "\n";
                //    }
                //}
                //first = false;
                //if (message != null)
                //{
                //    message += "- Specifying a summary is recommended.";
                //}
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
                    message += Tobi_Plugin_Descriptions_Lang.SummaryMoreThanOne;
                    message += " (";
                    message += count;
                    message += ")";
                }
            }

            //foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Summary))
            //{
            //    if (altContent.Audio != null && altContent.Text == null)
            //    {
            //        if (!first)
            //        {
            //            if (message != null)
            //            {
            //                message += "\n";
            //            }
            //        }
            //        first = false;
            //        if (message != null)
            //        {
            //            message += "- The summary has audio but no corresponding text.";
            //        }

            //        break;
            //    }
            //}

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Summary))
            {
                if (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
                {
                    bool otherDataInAdvancedMode = altContent.Audio != null
                                                   || Daisy3_Export.AltContentHasSignificantMetadata(altContent);
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
                        message += Tobi_Plugin_Descriptions_Lang.SummaryTextMissing;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "] ";
                        }
                        if (otherDataInAdvancedMode)
                        {
                            message += " ";
                            message += Tobi_Plugin_Descriptions_Lang.LongDescHasOtherData;
                        }
                    }
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Summary))
            {
                if (altContent.Image != null)
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
                        message += Tobi_Plugin_Descriptions_Lang.SummaryNoImage;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "]";
                        }
                    }
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
                    message += Tobi_Plugin_Descriptions_Lang.SimplifiedLanguageMoreThanOne;
                    message += " (";
                    message += count;
                    message += ")";
                }
            }

            //foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedLanguageDescription))
            //{
            //    if (altContent.Audio != null && altContent.Text == null)
            //    {
            //        if (!first)
            //        {
            //            if (message != null)
            //            {
            //                message += "\n";
            //            }
            //        }
            //        first = false;
            //        if (message != null)
            //        {
            //            message += "- The simplified language has audio but no corresponding text.";
            //        }

            //        break;
            //    }
            //}

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedLanguageDescription))
            {
                if (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
                {
                    bool otherDataInAdvancedMode = altContent.Audio != null
                                                   || Daisy3_Export.AltContentHasSignificantMetadata(altContent);
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
                        message += Tobi_Plugin_Descriptions_Lang.SimplifiedLanguageTextMissing;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "] ";
                        }
                        if (otherDataInAdvancedMode)
                        {
                            message += " ";
                            message += Tobi_Plugin_Descriptions_Lang.LongDescHasOtherData;
                        }
                    }
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedLanguageDescription))
            {
                if (altContent.Image != null)
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
                        message += Tobi_Plugin_Descriptions_Lang.SimplifiedLanguageNoImage;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "]";
                        }
                    }
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
                    message += Tobi_Plugin_Descriptions_Lang.SimplifiedImageMoreThanOne;
                    message += " (";
                    message += count;
                    message += "), ";
                    message += Tobi_Plugin_Descriptions_Lang.SimplifiedImageGroupID;
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedImage))
            {
                if (altContent.Audio != null && altContent.Text == null)
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
                        message += Tobi_Plugin_Descriptions_Lang.SimplifiedImageAudioNoTour;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "]";
                        }
                    }
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_SimplifiedImage))
            {
                if (altContent.Image == null)
                {
                    bool otherDataInAdvancedMode = altContent.Audio != null
                                                   || altContent.Text != null
                                                   || Daisy3_Export.AltContentHasSignificantMetadata(altContent);
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
                        message += Tobi_Plugin_Descriptions_Lang.SimplifiedImageMissingImage;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "] ";
                        }
                        if (otherDataInAdvancedMode)
                        {
                            message += " ";
                            message += Tobi_Plugin_Descriptions_Lang.LongDescHasOtherData;
                        }
                    }
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
                        message += Tobi_Plugin_Descriptions_Lang.SimplifiedImageTourRecommended;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "]";
                        }
                    }
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
                    message += Tobi_Plugin_Descriptions_Lang.TactileImageMoreThanOne;
                    message += " (";
                    message += count;
                    message += "), ";
                    message += Tobi_Plugin_Descriptions_Lang.SimplifiedImageGroupID;
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Tactile))
            {
                if (altContent.Audio != null && altContent.Text == null)
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
                        message += Tobi_Plugin_Descriptions_Lang.TactileImageAudioNoTour;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "]";
                        }
                    }
                }
            }

            foreach (var altContent in GetAltContents(DiagramContentModelHelper.D_Tactile))
            {
                bool otherDataInAdvancedMode = altContent.Audio != null
                                               || altContent.Text != null
                                               || Daisy3_Export.AltContentHasSignificantMetadata(altContent);
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
                        message += Tobi_Plugin_Descriptions_Lang.TactileImageMissingImage;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "] ";
                        }
                        if (otherDataInAdvancedMode)
                        {
                            message += " ";
                            message += Tobi_Plugin_Descriptions_Lang.LongDescHasOtherData;
                        }
                    }
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
                        message += Tobi_Plugin_Descriptions_Lang.TactileImageTourRecommended;

                        string xmlId = GetXmlID(altContent);
                        if (!String.IsNullOrEmpty(xmlId))
                        {
                            message += " [" + xmlId + "]";
                        }
                    }
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
                    message += Tobi_Plugin_Descriptions_Lang.DiagramUnknownElements;
                    message += " ";
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
                    message += Tobi_Plugin_Descriptions_Lang.DiagramElementsInvalidSyntax;
                    message += " ";
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
                    message += Tobi_Plugin_Descriptions_Lang.InvalidIDs;
                    message += " ";
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
                    message += Tobi_Plugin_Descriptions_Lang.DuplicatedIDs;
                    message += " ";
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
                    message += Tobi_Plugin_Descriptions_Lang.MissingIDs;
                    message += " ";
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
                    message += Tobi_Plugin_Descriptions_Lang.InvalidLanguageTags;
                    message += " ";
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
