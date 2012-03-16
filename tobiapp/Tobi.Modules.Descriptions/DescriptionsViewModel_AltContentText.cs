using System;
using Tobi.Common.MVVM;
using urakawa.commands;
using urakawa.core;
using urakawa.media;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsViewModel
    {

        public void SetDescriptionText(AlternateContent altContent, string txt)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (string.IsNullOrEmpty(txt))
            {
                if (altContent.Text != null)
                {
                    AlternateContentRemoveManagedMediaCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentRemoveManagedMediaCommand(node, altContent,
                                                                                                         altContent.Text);
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
            }
            else
            {
                TextMedia txt2 = node.Presentation.MediaFactory.CreateTextMedia();
                txt2.Text = txt;

                AlternateContentSetManagedMediaCommand cmd22 =
                    node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(node, altContent, txt2);
                node.Presentation.UndoRedoManager.Execute(cmd22);
            }

            RaisePropertyChanged(() => Descriptions);
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionText
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

                return m_SelectedAlternateContent.Text != null;
            }
        }

    }
}
