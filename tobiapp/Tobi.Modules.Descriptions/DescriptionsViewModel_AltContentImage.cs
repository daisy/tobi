using System;
using System.IO;
using System.Windows.Media;
using Microsoft.Practices.Unity;
using Tobi.Common.MVVM;
using urakawa.commands;
using urakawa.core;
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


    }
}
