using System;
using System.Collections.Generic;
using Tobi.Common.MVVM;
using urakawa.commands;
using urakawa.core;
using urakawa.metadata;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsViewModel
    {
        public void RemoveMetadata(AlternateContentProperty altProp, AlternateContent altContent,
            Metadata md)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp_ = node.GetProperty<AlternateContentProperty>();
            if (altProp_ == null) return;

            if (altProp != null && altProp_ != altProp) return;

            if (altContent != null && altProp_.AlternateContents.IndexOf(altContent) < 0) return;

            AlternateContentMetadataRemoveCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataRemoveCommand(node, altProp, altContent, md, null);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => Descriptions);
        }

        public void AddMetadata(AlternateContentProperty altProp, AlternateContent altContent,
            string newName, string newValue)
        {
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newValue)) return;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp_ = node.GetOrCreateAlternateContentProperty();

            if (altProp != null && altProp_ != altProp) return;

            if (altContent != null && altProp_.AlternateContents.IndexOf(altContent) < 0) return;

            Metadata meta = node.Presentation.MetadataFactory.CreateMetadata();
            meta.NameContentAttribute = new MetadataAttribute();
            meta.NameContentAttribute.Name = newName;
            //meta.NameContentAttribute.NamespaceUri = "dummy namespace";
            meta.NameContentAttribute.Value = newValue;
            AlternateContentMetadataAddCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(node, altProp, altContent, meta, null);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => Descriptions);
        }


        public void RemoveMetadataAttr(Metadata md, MetadataAttribute mdAttr)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            int index = altProp.Metadatas.IndexOf(md);
            if (index < 0) return;

            index = altProp.Metadatas.Get(index).OtherAttributes.IndexOf(mdAttr);
            if (index < 0) return;

            AlternateContentMetadataRemoveCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataRemoveCommand(node, altProp, null, md, mdAttr);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
        }

        public void AddMetadataAttr(Metadata md, string newName, string newValue)
        {
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newValue)) return;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            int index = altProp.Metadatas.IndexOf(md);
            if (index < 0) return;

            var metaAttr = new MetadataAttribute();
            metaAttr.Name = newName;
            //metaAttr.NamespaceUri = "dummy namespace";
            metaAttr.Value = newValue;

            AlternateContentMetadataAddCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(node, altProp, null, md, metaAttr);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
        }

        public void SetMetadataAttr(AlternateContentProperty altProp, AlternateContent altContent,
            Metadata md, MetadataAttribute mdAttr, string newName, string newValue)
        {
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newValue)) return;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp_ = node.GetProperty<AlternateContentProperty>();
            if (altProp_ == null) return;

            if (altProp != null && altProp_ != altProp) return;

            if (altContent != null && altProp_.AlternateContents.IndexOf(altContent) < 0) return;

            if (mdAttr == null)
            {
                MetadataAttribute attr = md.NameContentAttribute;

                if (attr.Name != newName)
                {
                    AlternateContentMetadataSetNameCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetNameCommand(
                            altProp,
                            altContent,
                            attr,
                            newName
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
                if (attr.Value != newValue)
                {
                    AlternateContentMetadataSetContentCommand cmd2 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetContentCommand(
                            altProp,
                            altContent,
                            attr,
                            newValue
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd2);
                }
            }
            else
            {
                if (mdAttr.Name != newName)
                {
                    AlternateContentMetadataSetNameCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetNameCommand(
                            altProp,
                            altContent,
                            mdAttr,
                            newName
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
                if (mdAttr.Value != newValue)
                {
                    AlternateContentMetadataSetContentCommand cmd2 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetContentCommand(
                            altProp,
                            altContent,
                            mdAttr,
                            newValue
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd2);
                }
            }


            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => Descriptions);
        }

        //public IEnumerable<MetadataAttribute> MetadataAttributes
        //{
        //    get
        //    {
        //        if (m_SelectedMedatadata == -1) return null;

        //        if (m_UrakawaSession.DocumentProject == null) return null;

        //        Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
        //        TreeNode node = selection.Item2 ?? selection.Item1;
        //        if (node == null) return null;

        //        AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
        //        if (altProp == null) return null;

        //        return altProp.Metadatas.Get(m_SelectedMedatadata).OtherAttributes.ContentsAs_Enumerable;
        //    }
        //}
        private Metadata m_SelectedMedatadata;
        public void SetSelectedMetadata(Metadata md)
        {
            m_SelectedMedatadata = md;
            RaisePropertyChanged(() => Metadatas);
        }

        [NotifyDependsOn("Metadatas")]
        public bool HasMetadataAttrs
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                if (altProp.Metadatas.Count <= 0) return false;

                if (m_SelectedMedatadata == null) return false;

                if (altProp.Metadatas.IndexOf(m_SelectedMedatadata) < 0) return false;

                return m_SelectedMedatadata.OtherAttributes.Count > 0;
            }
        }

        [NotifyDependsOn("Metadatas")]
        public bool HasMetadata
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                return altProp.Metadatas.Count > 0;
            }
        }

        public IEnumerable<Metadata> Metadatas //ObservableCollection
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
                return altProp.Metadatas.ContentsAs_Enumerable;
            }
        }

        private bool getValidationText_Metadata(ref string message)
        {
            bool first = true;

            string strDupIDS = "";
            foreach (var id in GetDuplicatedIDs(true, false))
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
                    message += "- Some identifiers are duplicated (this may be valid if used for grouping metadata): ";
                    message += strDupIDS;
                }
            }

            string strMissingIDS = "";
            foreach (var id in GetReferencedMissingIDs(true, false))
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
            foreach (var id in GetInvalidLanguageTags(true, false))
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

            string strInvalidDates = "";
            foreach (var id in GetInvalidDateStrings(true, false))
            {
                strInvalidDates += "[";
                strInvalidDates += id;
                strInvalidDates += "]";
            }

            if (!string.IsNullOrEmpty(strInvalidDates))
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
                    message += "- Some dates appear to be invalid: ";
                    message += strInvalidDates;
                }
            }

            bool hasMessages = !first;
            return hasMessages;
        }

        [NotifyDependsOn("ValidationText_Metadata")]
        public bool HasValidationWarning_Metadata
        {
            get
            {
                string str = null;
                return getValidationText_Metadata(ref str);
            }
        }

        [NotifyDependsOn("Metadatas")]
        public string ValidationText_Metadata
        {
            get
            {
                string str = "";
                if (HasValidationWarning_Metadata)
                {
                    getValidationText_Metadata(ref str);
                }
                return str;
            }
        }
    }
}
