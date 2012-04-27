using System;
using System.IO;
using System.Windows.Media;
using Microsoft.Practices.Unity;
using Tobi.Common.MVVM;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.daisy.export;
using urakawa.data;
using urakawa.media.data.image;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsViewModel
    {
        public void SetDescriptionImage(AlternateContent altContent, string fullPath)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (string.IsNullOrEmpty(fullPath))
            {
                if (altContent.Image != null)
                {
                    AlternateContentRemoveManagedMediaCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentRemoveManagedMediaCommand(node, altContent,
                                                                                                         altContent.Image);
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
            }
            else if (File.Exists(fullPath))
            {
                string extension = Path.GetExtension(fullPath);
                if (string.IsNullOrEmpty(extension)) return;

                ManagedImageMedia img1 = node.Presentation.MediaFactory.CreateManagedImageMedia();

                ImageMediaData imgData1 = node.Presentation.MediaDataFactory.CreateImageMediaData(extension);
                if (imgData1 == null)
                {
                    return;
                }

                imgData1.InitializeImage(fullPath, Path.GetFileName(fullPath));
                img1.ImageMediaData = imgData1;

                AlternateContentSetManagedMediaCommand cmd22 =
                    node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(node, altContent, img1);
                node.Presentation.UndoRedoManager.Execute(cmd22);
            }

            RaisePropertyChanged(() => Descriptions);
        }

        public ImageSource DescribableImage
        {
            get
            {
                TreeNode checkedNode = getCheckTreeNode();
                if (checkedNode == null) return null;

                return DescribableTreeNode.GetDescribableImage(checkedNode);
            }
        }

        public string DescribableImageInfo
        {
            get
            {
                TreeNode checkedNode = getCheckTreeNode();
                if (checkedNode == null) return null;

                return DescribableTreeNode.GetDescriptionLabel(checkedNode);
            }
        }

        private TreeNode getCheckTreeNode()
        {
            if (m_UrakawaSession.DocumentProject == null) return null;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return null;

            var navModel = m_Container.Resolve<DescriptionsNavigationViewModel>();
            if (navModel.DescriptionsNavigator == null) return null;

            bool found = false;
            foreach (DescribableTreeNode dnode in navModel.DescriptionsNavigator.DescribableTreeNodes)
            {
                found = dnode.TreeNode == node;
                if (found) break;
            }
            if (!found) return null;

            return node;
        }


        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionImage
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                if (altProp.AlternateContents.Count <= 0) return false;

                if (m_SelectedAlternateContent == null) return false;

                if (altProp.AlternateContents.IndexOf(m_SelectedAlternateContent) < 0) return false;

                return m_SelectedAlternateContent.Image != null;
            }
        }


        [NotifyDependsOn("HasDescriptionImage_Simplified")]
        public string DescriptionImage_Simplified
        {
            get
            {
                AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_SimplifiedImage);
                if (altContent == null
                    || altContent.Image == null
                    || altContent.Image.ImageMediaData == null
                    || altContent.Image.ImageMediaData.DataProvider == null
                    || !(altContent.Image.ImageMediaData.DataProvider is FileDataProvider)
                    || string.IsNullOrEmpty(((FileDataProvider)altContent.Image.ImageMediaData.DataProvider).DataFileFullPath)
                    )
                {
                    return null;
                }

                return ((FileDataProvider)altContent.Image.ImageMediaData.DataProvider).DataFileFullPath;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionImage_Simplified
        {
            get
            {
                string str = DescriptionImage_Simplified;
                return (!string.IsNullOrEmpty(str));
            }
        }

        [NotifyDependsOn("HasDescriptionImage_Tactile")]
        public string DescriptionImage_Tactile
        {
            get
            {
                AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_Tactile);
                if (altContent == null
                    || altContent.Image == null
                    || altContent.Image.ImageMediaData == null
                    || altContent.Image.ImageMediaData.DataProvider == null
                    || !(altContent.Image.ImageMediaData.DataProvider is FileDataProvider)
                    || string.IsNullOrEmpty(((FileDataProvider)altContent.Image.ImageMediaData.DataProvider).DataFileFullPath)
                    )
                {
                    return null;
                }

                return ((FileDataProvider)altContent.Image.ImageMediaData.DataProvider).DataFileFullPath;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionImage_Tactile
        {
            get
            {
                string str = DescriptionImage_Tactile;
                return (!string.IsNullOrEmpty(str));
            }
        }

        private bool getValidationText_BasicImage(ref string message)
        {
            bool first = true;

            AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_SimplifiedImage);
            if (altContent != null)
            {
                if (altContent.Image != null && altContent.Text == null)
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
                        message += "- It is recommended to specify a tour for the simplified image.";
                    }
                }

                bool otherDataInAdvancedMode = altContent.Audio != null
                                               || Daisy3_Export.AltContentHasSignificantMetadata(altContent);
                if (altContent.Image == null
                    && (
                    altContent.Text != null
                    || otherDataInAdvancedMode
                    )
                    )
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
                        message += "- The simplified image is missing.";
                        if (otherDataInAdvancedMode)
                        {
                            message += " (has other data, see advanced editor)";
                        }
                    }
                }
                //if (altContent.Image == null && altContent.Text != null)
                //{
                //    if (!first)
                //    {
                //        if (message != null)
                //        {
                //            message += "\n";
                //        }
                //    }
                //    first = false;
                //    if (message != null)
                //    {
                //        message += "- A tour is specified without its associated simplified image.";
                //    }
                //}
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
                        message += "- The simplified image has audio but no corresponding tour.";
                    }
                }
            }
            altContent = GetAltContent(DiagramContentModelHelper.D_Tactile);
            if (altContent != null)
            {
                if (altContent.Image != null && altContent.Text == null)
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
                        message += "- It is recommended to specify a tour for the tactile image.";
                    }
                }
                bool otherDataInAdvancedMode = altContent.Audio != null
                                               || Daisy3_Export.AltContentHasSignificantMetadata(altContent);
                if (altContent.Image == null
                    && (
                    altContent.Text != null
                    || otherDataInAdvancedMode
                    )
                    )
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
                        message += "- The tactile image is missing.";
                        if (otherDataInAdvancedMode)
                        {
                            message += " (has other data, see advanced editor)";
                        }
                    }
                }
                //if (altContent.Image == null && altContent.Text != null)
                //{
                //    if (!first)
                //    {
                //        if (message != null)
                //        {
                //            message += "\n";
                //        }
                //    }
                //    first = false;
                //    if (message != null)
                //    {
                //        message += "- A tour is specified without its associated tactile image.";
                //    }
                //}
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
                        message += "- The tactile image has audio but no corresponding tour.";
                    }
                }
            }

            bool hasMessages = !first;
            return hasMessages;
        }

        [NotifyDependsOn("ValidationText_BasicImage")]
        public bool HasValidationWarning_BasicImage
        {
            get
            {
                string str = null;
                return getValidationText_BasicImage(ref str);
            }
        }

        [NotifyDependsOn("Descriptions")]
        public string ValidationText_BasicImage
        {
            get
            {
                string str = "";
                if (HasValidationWarning_BasicImage)
                {
                    getValidationText_BasicImage(ref str);
                }
                return str;
            }
        }
    }
}
