using System;
using System.IO;
using Tobi.Common.MVVM;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.exception;
using urakawa.media.data.audio;
using urakawa.media.timing;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsViewModel
    {
        public void SetDescriptionAudio(AlternateContent altContent, ManagedAudioMedia manMedia)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            SetDescriptionAudio(altContent, manMedia, node);
        }

        public void SetDescriptionAudio(AlternateContent altContent, ManagedAudioMedia manMedia, TreeNode node)
        {
            if (manMedia == null
                || manMedia.HasActualAudioMediaData && !manMedia.Duration.IsGreaterThan(Time.Zero))
            {
                if (altContent.Audio != null)
                {
                    AlternateContentRemoveManagedMediaCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentRemoveManagedMediaCommand(node, altContent,
                                                                                                         altContent.Audio);
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
            }
            else
            {
                ManagedAudioMedia audio1 = node.Presentation.MediaFactory.CreateManagedAudioMedia();
                AudioMediaData audioData1 = node.Presentation.MediaDataFactory.CreateAudioMediaData();
                audio1.AudioMediaData = audioData1;

                // WARNING: WavAudioMediaData implementation differs from AudioMediaData:
                // the latter is naive and performs a stream binary copy, the latter is optimized and re-uses existing WavClips. 
                //  WARNING 2: The audio data from the given parameter gets emptied !
                //audio1.AudioMediaData.MergeWith(manMedia.AudioMediaData);

                if (!audio1.AudioMediaData.PCMFormat.Data.IsCompatibleWith(manMedia.AudioMediaData.PCMFormat.Data))
                {
                    throw new InvalidDataFormatException(
                        "Can not merge description audio with a AudioMediaData with incompatible audio data");
                }
                Stream stream = manMedia.AudioMediaData.OpenPcmInputStream();
                try
                {
                    audio1.AudioMediaData.AppendPcmData(stream, null); //manMedia.AudioMediaData.AudioDuration
                }
                finally
                {
                    stream.Close();
                }

                AlternateContentSetManagedMediaCommand cmd22 =
                    node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(node, altContent, audio1);
                node.Presentation.UndoRedoManager.Execute(cmd22);
            }

            RaisePropertyChanged(() => Descriptions);
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionAudio
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

                return m_SelectedAlternateContent.Audio != null;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasAudio_LongDesc
        {
            get
            {
                AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_LondDesc);
                if (altContent != null)
                {
                    return altContent.Audio != null;
                }
                return false;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasAudio_Summary
        {
            get
            {
                AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_Summary);
                if (altContent != null)
                {
                    return altContent.Audio != null;
                }
                return false;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasAudio_SimplifiedLanguage
        {
            get
            {
                AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_SimplifiedLanguageDescription);
                if (altContent != null)
                {
                    return altContent.Audio != null;
                }
                return false;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasAudio_SimplifiedImage
        {
            get
            {
                AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_SimplifiedImage);
                if (altContent != null)
                {
                    return altContent.Audio != null;
                }
                return false;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasAudio_TactileImage
        {
            get
            {
                AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_Tactile);
                if (altContent != null)
                {
                    return altContent.Audio != null;
                }
                return false;
            }
        }

    }
}
