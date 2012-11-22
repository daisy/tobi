using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.media;
using urakawa.media.data;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;
using urakawa.media.data.image;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.property.alt;
using urakawa.xuk;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsViewModel
    {

        public void ImportDiagramXML(string xmlFilePath)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode treeNode = selection.Item2 ?? selection.Item1;
            var altProp = treeNode.GetAlternateContentProperty();



            XmlDocument diagramXML = XmlReaderWriterHelper.ParseXmlDocument(xmlFilePath, false, false);

            XmlNode description = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(diagramXML, false, "description", DiagramContentModelHelper.NS_URL_DIAGRAM);
            if (description == null)
            {
                return;
            }

            XmlAttributeCollection descrAttrs = description.Attributes;
            if (descrAttrs != null)
            {
                for (int i = 0; i < descrAttrs.Count; i++)
                {
                    XmlAttribute attr = descrAttrs[i];

                    if (!attr.Name.StartsWith(XmlReaderWriterHelper.NS_PREFIX_XML + ":"))
                    {
                        continue;
                    }

                    Metadata altContMetadata = treeNode.Presentation.MetadataFactory.CreateMetadata();
                    altContMetadata.NameContentAttribute = new MetadataAttribute();
                    altContMetadata.NameContentAttribute.Name = attr.Name;
                    altContMetadata.NameContentAttribute.NamespaceUri = XmlReaderWriterHelper.NS_URL_XML;
                    altContMetadata.NameContentAttribute.Value = attr.Value;
                    AlternateContentMetadataAddCommand cmd_AltPropMetadata_XML =
                        treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                            treeNode,
                            altProp,
                            null,
                            altContMetadata,
                            null
                            );
                    treeNode.Presentation.UndoRedoManager.Execute(cmd_AltPropMetadata_XML);
                }
            }

            XmlNode head = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(description, false, "head", DiagramContentModelHelper.NS_URL_DIAGRAM);
            if (head != null)
            {
                foreach (XmlNode metaNode in XmlDocumentHelper.GetChildrenElementsOrSelfWithName(head, true, "meta", DiagramContentModelHelper.NS_URL_ZAI, false))
                {
                    if (metaNode.NodeType != XmlNodeType.Element || metaNode.LocalName != "meta")
                    {
#if DEBUG
                        Debugger.Break();
#endif // DEBUG
                        continue;
                    }



                    //XmlNode childMetadata = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(node, false, "meta", DiagramContentModelHelper.NS_URL_ZAI);
                    //if (childMetadata != null)
                    //{
                    //    continue;
                    //}
                    bool foundAtLeastOneChildMeta = false;
                    foreach (XmlNode child in XmlDocumentHelper.GetChildrenElementsOrSelfWithName(metaNode, false, "meta", DiagramContentModelHelper.NS_URL_ZAI, false))
                    {
                        if (child == metaNode) continue;

                        foundAtLeastOneChildMeta = true;
                        break;
                    }
                    if (foundAtLeastOneChildMeta)
                    {
                        continue;
                    }



                    XmlAttributeCollection mdAttributes = metaNode.Attributes;
                    if (mdAttributes == null || mdAttributes.Count <= 0)
                    {
                        continue;
                    }

                    XmlNode attrName = mdAttributes.GetNamedItem("name");
                    XmlNode attrProperty = mdAttributes.GetNamedItem("property");

                    string property = (attrName != null && !String.IsNullOrEmpty(attrName.Value))
                                          ? attrName.Value
                                          : (attrProperty != null && !String.IsNullOrEmpty(attrProperty.Value)
                                                 ? attrProperty.Value
                                                 : null);

                    XmlNode attrContent = mdAttributes.GetNamedItem("content");

                    string content = (attrContent != null && !String.IsNullOrEmpty(attrContent.Value))
                                         ? attrContent.Value
                                         : metaNode.InnerText;

                    if (!(
                             String.IsNullOrEmpty(property) && String.IsNullOrEmpty(content)
                             ||
                             !String.IsNullOrEmpty(property) && !String.IsNullOrEmpty(content)
                         ))
                    {
                        continue;
                    }

                    Metadata altContMetadata = treeNode.Presentation.MetadataFactory.CreateMetadata();
                    altContMetadata.NameContentAttribute = new MetadataAttribute();
                    altContMetadata.NameContentAttribute.Name = String.IsNullOrEmpty(property)
                                                                    ? DiagramContentModelHelper.NA
                                                                    : property;
                    altContMetadata.NameContentAttribute.NamespaceUri =
                        String.IsNullOrEmpty(property)
                            ? null
                            : (
                                  property.StartsWith(DiagramContentModelHelper.NS_PREFIX_DIAGRAM_METADATA + ":")
                                      ? DiagramContentModelHelper.NS_URL_DIAGRAM
                                      : (property.StartsWith(SupportedMetadata_Z39862005.NS_PREFIX_DUBLIN_CORE + ":")
                                             ? SupportedMetadata_Z39862005.NS_URL_DUBLIN_CORE
                                             : null)
                              )
                        ;
                    altContMetadata.NameContentAttribute.Value = String.IsNullOrEmpty(content)
                                                                     ? DiagramContentModelHelper.NA
                                                                     : content;
                    AlternateContentMetadataAddCommand cmd_AltPropMetadata =
                        treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                            treeNode,
                            altProp,
                            null,
                            altContMetadata,
                            null
                            );
                    treeNode.Presentation.UndoRedoManager.Execute(cmd_AltPropMetadata);

                    bool parentIsMeta = metaNode.ParentNode.LocalName == "meta";

                    var listAttrs = new List<XmlAttribute>(mdAttributes.Count +
                        (parentIsMeta && metaNode.ParentNode.Attributes != null ? metaNode.ParentNode.Attributes.Count : 0)
                        );

                    for (int i = 0; i < mdAttributes.Count; i++)
                    {
                        XmlAttribute attribute = mdAttributes[i];
                        listAttrs.Add(attribute);
                    }

                    if (parentIsMeta && metaNode.ParentNode.Attributes != null)
                    {
                        for (int i = 0; i < metaNode.ParentNode.Attributes.Count; i++)
                        {
                            XmlAttribute attribute = metaNode.ParentNode.Attributes[i];
                            if (mdAttributes.GetNamedItem(attribute.LocalName, attribute.NamespaceURI) == null)
                            {
                                listAttrs.Add(attribute);
                            }
                        }
                    }

                    foreach (var attribute in listAttrs)
                    {
                        if (attribute.LocalName == DiagramContentModelHelper.Name
                            || attribute.LocalName == DiagramContentModelHelper.Property
                            || attribute.LocalName == DiagramContentModelHelper.Content)
                        {
                            continue;
                        }


                        if (attribute.Name.StartsWith(XmlReaderWriterHelper.NS_PREFIX_XMLNS + ":"))
                        {
                            //
                        }
                        else if (attribute.Name == XmlReaderWriterHelper.NS_PREFIX_XMLNS)
                        {
                            //
                        }
                        else
                        {
                            MetadataAttribute metadatattribute = new MetadataAttribute();
                            metadatattribute.Name = attribute.Name;
                            metadatattribute.NamespaceUri =
                                attribute.Name.IndexOf(':') >= 0
                                //attribute.Name.Contains(":")
                                ? attribute.NamespaceURI : null;
                            metadatattribute.Value = attribute.Value;
                            AlternateContentMetadataAddCommand cmd_AltPropMetadataAttr =
                                treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                                    treeNode,
                                    altProp,
                                    null,
                                    altContMetadata,
                                    metadatattribute
                                    );
                            treeNode.Presentation.UndoRedoManager.Execute(cmd_AltPropMetadataAttr);
                        }
                    }
                }
            }

            XmlNode body = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(description, false, "body", DiagramContentModelHelper.NS_URL_DIAGRAM);
            if (body != null)
            {
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_Summary);
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_LondDesc);
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_SimplifiedLanguageDescription);

                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_Tactile);
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_SimplifiedImage);

                //#if true || SUPPORT_ANNOTATION_ELEMENT
                //                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.Annotation);
                //#endif //SUPPORT_ANNOTATION_ELEMENT



                diagramXmlParseBody(xmlFilePath, treeNode, body);
            }

            OnPanelLoaded();

        }


        private void diagramXmlParseBody(string xmlFilePath, TreeNode treeNode, XmlNode body)
        {
            IEnumerator enumerator = body.GetEnumerator();
            while (enumerator.MoveNext())
            {
                XmlNode node = (XmlNode)enumerator.Current;

                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }


                string name = node.Name;
                if (name == DiagramContentModelHelper.D_Summary
                    || name == DiagramContentModelHelper.D_LondDesc
                    || name == DiagramContentModelHelper.D_SimplifiedLanguageDescription
                    || name == DiagramContentModelHelper.D_Tactile
                    || name == DiagramContentModelHelper.D_SimplifiedImage
                    //#if true || SUPPORT_ANNOTATION_ELEMENT
                    // || name == DiagramContentModelHelper.Annotation
                    //#endif //SUPPORT_ANNOTATION_ELEMENT
)
                {
                    continue;
                }

                diagramXmlParseBody_(node, xmlFilePath, treeNode, 0);
            }
        }

        private void diagramXmlParseBodySpecific(string xmlFilePath, TreeNode treeNode, XmlNode body, string diagramElementName)
        {
            string localName = DiagramContentModelHelper.StripNSPrefix(diagramElementName);
            foreach (XmlNode diagramElementNode in XmlDocumentHelper.GetChildrenElementsOrSelfWithName(body, false, localName, DiagramContentModelHelper.NS_URL_DIAGRAM, false))
            {
                if (diagramElementNode.NodeType != XmlNodeType.Element || diagramElementNode.LocalName != localName)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    // DEBUG
                    continue;
                }

                diagramXmlParseBody_(diagramElementNode, xmlFilePath, treeNode, 0);
            }
        }

        private void diagramXmlParseBody_(XmlNode diagramElementNode, string xmlFilePath, TreeNode treeNode, int objectIndex)
        {
            string diagramElementName = diagramElementNode.Name;

            AlternateContent altContent = treeNode.Presentation.AlternateContentFactory.CreateAlternateContent();
            AlternateContentAddCommand cmd_AltContent =
                treeNode.Presentation.CommandFactory.CreateAlternateContentAddCommand(treeNode, altContent);
            treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent);



            Metadata diagramElementName_Metadata = new Metadata();
            diagramElementName_Metadata.NameContentAttribute = new MetadataAttribute();
            diagramElementName_Metadata.NameContentAttribute.Name = DiagramContentModelHelper.DiagramElementName;
            diagramElementName_Metadata.NameContentAttribute.NamespaceUri = null;
            diagramElementName_Metadata.NameContentAttribute.Value = diagramElementName;
            AlternateContentMetadataAddCommand cmd_AltContent_diagramElementName_Metadata =
                treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                    treeNode,
                    null,
                    altContent,
                    diagramElementName_Metadata,
                    null
                    );
            treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent_diagramElementName_Metadata);


            if (diagramElementNode.Attributes != null)
            {
                for (int i = 0; i < diagramElementNode.Attributes.Count; i++)
                {
                    XmlAttribute attribute = diagramElementNode.Attributes[i];


                    if (attribute.Name.StartsWith(XmlReaderWriterHelper.NS_PREFIX_XMLNS + ":"))
                    {
                        //
                    }
                    else if (attribute.Name == XmlReaderWriterHelper.NS_PREFIX_XMLNS)
                    {
                        //
                    }
                    else if (attribute.Name == DiagramContentModelHelper.TOBI_Audio)
                    {
                        string fullPath = null;
                        if (FileDataProvider.isHTTPFile(attribute.Value))
                        {
                            fullPath = FileDataProvider.EnsureLocalFilePathDownloadTempDirectory(attribute.Value);
                        }
                        else
                        {
                            fullPath = Path.Combine(Path.GetDirectoryName(xmlFilePath), attribute.Value);
                        }
                        if (fullPath != null && File.Exists(fullPath))
                        {
                            string extension = Path.GetExtension(fullPath);

                            bool isWav = extension.Equals(DataProviderFactory.AUDIO_WAV_EXTENSION, StringComparison.OrdinalIgnoreCase);

                            AudioLibPCMFormat wavFormat = null;
                            if (isWav)
                            {
                                Stream fileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                try
                                {
                                    uint dataLength;
                                    wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                                }
                                finally
                                {
                                    fileStream.Close();
                                }
                            }
                            string originalFilePath = null;

                            DebugFix.Assert(treeNode.Presentation.MediaDataManager.EnforceSinglePCMFormat);

                            bool wavNeedsConversion = false;
                            if (wavFormat != null)
                            {
                                wavNeedsConversion = !wavFormat.IsCompatibleWith(treeNode.Presentation.MediaDataManager.DefaultPCMFormat.Data);
                            }
                            if (!isWav || wavNeedsConversion)
                            {
                                originalFilePath = fullPath;

                                var audioFormatConvertorSession =
                                    new AudioFormatConvertorSession(
                                    //AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY,
                                    treeNode.Presentation.DataProviderManager.DataFileDirectoryFullPath,
                                treeNode.Presentation.MediaDataManager.DefaultPCMFormat,
                                false,
                                m_UrakawaSession.IsAcmCodecsDisabled);

                                //filePath = m_AudioFormatConvertorSession.ConvertAudioFileFormat(filePath);

                                bool cancelled = false;

                                var converter = new AudioClipConverter(audioFormatConvertorSession, fullPath);

                                bool error = ShellView.RunModalCancellableProgressTask(true,
                                    "Converting audio...",
                                    converter,
                                    () =>
                                    {
                                        m_Logger.Log(@"Audio conversion CANCELLED", Category.Debug, Priority.Medium);
                                        cancelled = true;
                                    },
                                    () =>
                                    {
                                        m_Logger.Log(@"Audio conversion DONE", Category.Debug, Priority.Medium);
                                        cancelled = false;
                                    });

                                if (cancelled)
                                {
                                    //DebugFix.Assert(!result);
                                    break;
                                }

                                fullPath = converter.ConvertedFilePath;
                                if (string.IsNullOrEmpty(fullPath))
                                {
                                    break;
                                }

                                m_Logger.Log(string.Format("Converted audio {0} to {1}", originalFilePath, fullPath),
                                           Category.Debug, Priority.Medium);

                                //Stream fileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                //try
                                //{
                                //    uint dataLength;
                                //    wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                                //}
                                //finally
                                //{
                                //    fileStream.Close();
                                //}
                            }


                            ManagedAudioMedia manAudioMedia = treeNode.Presentation.MediaFactory.CreateManagedAudioMedia();
                            AudioMediaData audioMediaData = treeNode.Presentation.MediaDataFactory.CreateAudioMediaData(DataProviderFactory.AUDIO_WAV_EXTENSION);
                            manAudioMedia.AudioMediaData = audioMediaData;

                            FileDataProvider dataProv = (FileDataProvider)treeNode.Presentation.DataProviderFactory.Create(DataProviderFactory.AUDIO_WAV_MIME_TYPE);
                            dataProv.InitByCopyingExistingFile(fullPath);
                            audioMediaData.AppendPcmData(dataProv);

                            //                            Stream wavStream = null;
                            //                            try
                            //                            {
                            //                                wavStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                            //                                uint dataLength;
                            //                                AudioLibPCMFormat pcmInfo = AudioLibPCMFormat.RiffHeaderParse(wavStream, out dataLength);

                            //                                if (!treeNode.Presentation.MediaDataManager.DefaultPCMFormat.Data.IsCompatibleWith(pcmInfo))
                            //                                {
                            //#if DEBUG
                            //                                    Debugger.Break();
                            //#endif //DEBUG
                            //                                    wavStream.Close();
                            //                                    wavStream = null;

                            //                                    var audioFormatConvertorSession =
                            //                                        new AudioFormatConvertorSession(
                            //                                        //AudioFormatConvertorSession.TEMP_AUDIO_DIRECTORY,
                            //                                        treeNode.Presentation.DataProviderManager.DataFileDirectoryFullPath,
                            //                                    treeNode.Presentation.MediaDataManager.DefaultPCMFormat, m_UrakawaSession.IsAcmCodecsDisabled);

                            //                                    string newfullWavPath = audioFormatConvertorSession.ConvertAudioFileFormat(fullPath);

                            //                                    FileDataProvider dataProv = (FileDataProvider)treeNode.Presentation.DataProviderFactory.Create(DataProviderFactory.AUDIO_WAV_MIME_TYPE);
                            //                                    dataProv.InitByMovingExistingFile(newfullWavPath);
                            //                                    audioMediaData.AppendPcmData(dataProv);
                            //                                }
                            //                                else // use original wav file by copying it to data directory
                            //                                {
                            //                                    FileDataProvider dataProv = (FileDataProvider)treeNode.Presentation.DataProviderFactory.Create(DataProviderFactory.AUDIO_WAV_MIME_TYPE);
                            //                                    dataProv.InitByCopyingExistingFile(fullPath);
                            //                                    audioMediaData.AppendPcmData(dataProv);
                            //                                }
                            //                            }
                            //                            finally
                            //                            {
                            //                                if (wavStream != null) wavStream.Close();
                            //                            }



                            AlternateContentSetManagedMediaCommand cmd_AltContent_diagramElement_Audio =
                                treeNode.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(treeNode, altContent, manAudioMedia);
                            treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent_diagramElement_Audio);

                            //SetDescriptionAudio(altContent, audio, treeNode);
                        }
                    }
                    else
                    {
                        Metadata diagramElementAttribute_Metadata = new Metadata();
                        diagramElementAttribute_Metadata.NameContentAttribute = new MetadataAttribute();
                        diagramElementAttribute_Metadata.NameContentAttribute.Name = attribute.Name;
                        diagramElementAttribute_Metadata.NameContentAttribute.NamespaceUri = attribute.NamespaceURI;
                        diagramElementAttribute_Metadata.NameContentAttribute.Value = attribute.Value;
                        AlternateContentMetadataAddCommand cmd_AltContent_diagramElementAttribute_Metadata =
                            treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                                treeNode,
                                null,
                                altContent,
                                diagramElementAttribute_Metadata,
                                null
                                );
                        treeNode.Presentation.UndoRedoManager.Execute(
                            cmd_AltContent_diagramElementAttribute_Metadata);
                    }
                }
            }

            int nObjects = -1;

            XmlNode textNode = diagramElementNode;

            if (diagramElementName == DiagramContentModelHelper.D_SimplifiedImage
                || diagramElementName == DiagramContentModelHelper.D_Tactile)
            {
                string localTourName = DiagramContentModelHelper.StripNSPrefix(DiagramContentModelHelper.D_Tour);
                XmlNode tour =
                    XmlDocumentHelper.GetFirstChildElementOrSelfWithName(diagramElementNode, false,
                                                                         localTourName,
                                                                         DiagramContentModelHelper.NS_URL_DIAGRAM);
                textNode = tour;

                IEnumerable<XmlNode> objects = XmlDocumentHelper.GetChildrenElementsOrSelfWithName(diagramElementNode, false,
                                                                                          DiagramContentModelHelper.
                                                                                              Object,
                                                                                          DiagramContentModelHelper.
                                                                                              NS_URL_ZAI, false);
                nObjects = 0;
                foreach (XmlNode obj in objects)
                {
                    nObjects++;
                }

                int i = -1;
                foreach (XmlNode obj in objects)
                {
                    i++;
                    if (i != objectIndex)
                    {
                        continue;
                    }

                    if (obj.Attributes == null || obj.Attributes.Count <= 0)
                    {
                        break;
                    }

                    for (int j = 0; j < obj.Attributes.Count; j++)
                    {
                        XmlAttribute attribute = obj.Attributes[j];


                        if (attribute.Name.StartsWith(XmlReaderWriterHelper.NS_PREFIX_XMLNS + ":"))
                        {
                            //
                        }
                        else if (attribute.Name == XmlReaderWriterHelper.NS_PREFIX_XMLNS)
                        {
                            //
                        }
                        else if (attribute.Name == DiagramContentModelHelper.Src)
                        {
                            //
                        }
                        else if (attribute.Name == DiagramContentModelHelper.SrcType)
                        {
                            //
                        }
                        else
                        {
                            Metadata diagramElementAttribute_Metadata = new Metadata();
                            diagramElementAttribute_Metadata.NameContentAttribute = new MetadataAttribute();
                            diagramElementAttribute_Metadata.NameContentAttribute.Name = attribute.Name;
                            diagramElementAttribute_Metadata.NameContentAttribute.NamespaceUri = attribute.NamespaceURI;
                            diagramElementAttribute_Metadata.NameContentAttribute.Value = attribute.Value;
                            AlternateContentMetadataAddCommand cmd_AltContent_diagramElementAttribute_Metadata =
                                treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                                    treeNode,
                                    null,
                                    altContent,
                                    diagramElementAttribute_Metadata,
                                    null
                                    );
                            treeNode.Presentation.UndoRedoManager.Execute(
                                cmd_AltContent_diagramElementAttribute_Metadata);
                        }
                    }

                    XmlAttribute srcAttr = (XmlAttribute)obj.Attributes.GetNamedItem(DiagramContentModelHelper.Src);
                    if (srcAttr != null)
                    {
                        XmlAttribute srcType =
                            (XmlAttribute)obj.Attributes.GetNamedItem(DiagramContentModelHelper.SrcType);

                        ManagedImageMedia img = treeNode.Presentation.MediaFactory.CreateManagedImageMedia();

                        string imgFullPath = null;
                        if (FileDataProvider.isHTTPFile(srcAttr.Value))
                        {
                            imgFullPath = FileDataProvider.EnsureLocalFilePathDownloadTempDirectory(srcAttr.Value);
                        }
                        else
                        {
                            imgFullPath = Path.Combine(Path.GetDirectoryName(xmlFilePath), srcAttr.Value);
                        }
                        if (imgFullPath != null && File.Exists(imgFullPath))
                        {
                            string extension = Path.GetExtension(imgFullPath);

                            ImageMediaData imgData = treeNode.Presentation.MediaDataFactory.CreateImageMediaData(extension);
                            if (imgData != null)
                            {
                                imgData.InitializeImage(imgFullPath, Path.GetFileName(imgFullPath));
                                img.ImageMediaData = imgData;

                                AlternateContentSetManagedMediaCommand cmd_AltContent_Image =
                                    treeNode.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(
                                        treeNode, altContent, img);
                                treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent_Image);
                            }
                        }
                    }
                }
            }

            if (textNode != null)
            {
                string strText = textNode.InnerXml;

                if (!string.IsNullOrEmpty(strText))
                {
                    strText = strText.Trim();
                    strText = Regex.Replace(strText, @"\s+", " ");
                    strText = strText.Replace("\r\n", "\n");
                }

                if (!string.IsNullOrEmpty(strText))
                {
                    TextMedia txtMedia = treeNode.Presentation.MediaFactory.CreateTextMedia();
                    txtMedia.Text = strText;
                    AlternateContentSetManagedMediaCommand cmd_AltContent_Text =
                        treeNode.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(treeNode,
                                                                                                          altContent,
                                                                                                          txtMedia);
                    treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent_Text);
                }
            }

            if (nObjects > 0 && ++objectIndex <= nObjects - 1)
            {
                diagramXmlParseBody_(diagramElementNode, xmlFilePath, treeNode, objectIndex);
            }
        }
    }
}
