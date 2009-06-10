using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tobi.Modules.MetadataPane
{
    public enum SupportedMetadataFieldType
    {
        //short vs long string just gives an initial value for the 
        //size of the text entry field
        ShortString,
        LongString,
        Integer,
        ClockValue,
        LanguageCode,
        Date,
        FileUri
    }

    public enum MetadataOccurence
    {
        Required,
        Recommended,
        Optional
    } ;

    public class SupportedMetadataItem
    {
        public SupportedMetadataFieldType FieldType { get; set; }
        public MetadataOccurence Occurence { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsReapeatable { get; set; }
        public string Name { get; set; }
        public string Description { get; set;}
        
        public SupportedMetadataItem(string name, SupportedMetadataFieldType fieldType, 
            MetadataOccurence occurence, bool isReadOnly, bool isRepeatable, string description) 
        {
            Name = name;
            FieldType = fieldType;
            Occurence = occurence;
            IsReadOnly = isReadOnly;
            isRepeatable = isRepeatable;
            Description = description;
        }
    }

    public static class SupportedMetadataList
    {
        public static readonly List<SupportedMetadataItem> MetadataList =
            new List<SupportedMetadataItem>
                {
                    new SupportedMetadataItem(
                        "dc:Date",
                        SupportedMetadataFieldType.Date,
                        MetadataOccurence.Required,
                        false,
                        true,
                        "Date of publication of the DTB. "),
                    new SupportedMetadataItem(
                        "dtb:sourceDate",
                        SupportedMetadataFieldType.Date,
                        MetadataOccurence.Recommended,
                        false,
                        false,
                        "Date of publication of the resource (e.g., a print original, ebook, etc.) from which the DTB is derived."),
                    new SupportedMetadataItem(
                        "dtb:producedDate",
                        SupportedMetadataFieldType.Date,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "Date of first generation of the complete DTB, i.e. Production completion date."),
                    new SupportedMetadataItem(
                        "dtb:revisionDate",
                        SupportedMetadataFieldType.Date,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "Date associated with the specific dtb:revision."),
                    new SupportedMetadataItem(
                        "dc:Title",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Required,
                        false,
                        true,
                        "The title of the DTB, including any subtitles."),
                    new SupportedMetadataItem(
                        "dc:Publisher",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Required,
                        false,
                        true,
                        "The agency responsible for making the DTB available."),
                    new SupportedMetadataItem(
                        "dc:Language",
                        SupportedMetadataFieldType.LanguageCode,
                        MetadataOccurence.Required,
                        false,
                        true,
                        "Language of the content of the publication."),
                    new SupportedMetadataItem(
                        "dc:Identifier",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Required,
                        false,
                        true,
                        "A string or number identifying the DTB."),
                    new SupportedMetadataItem(
                        "dc:Creator",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        false,
                        true,
                        "Names of primary author or creator of the intellectual content of the publication."),
                    new SupportedMetadataItem(
                        "dc:Subject",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        false,
                        true,
                        "The topic of the content of the publication."),
                    new SupportedMetadataItem(
                        "dc:Description",
                        SupportedMetadataFieldType.LongString,
                        MetadataOccurence.Optional,
                        false,
                        true,
                        "Plain text describing the publication's content."),
                    new SupportedMetadataItem(
                        "dc:Contributor",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Optional,
                        false,
                        true,
                        "A party whose contribution to the publication is secondary to those named in dc:Creator."),
                    new SupportedMetadataItem(
                        "dc:Source",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        false,
                        true,
                        "A reference to a resource (e.g., a print original, ebook, etc.) from which the DTB is derived. Best practice is to use the ISBN when available."),
                    new SupportedMetadataItem(
                        "dc:Relation",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Optional,
                        false,
                        true,
                        "A reference to a related resource."),
                    new SupportedMetadataItem(
                        "dc:Coverage",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Optional,
                        false,
                        true,
                        "The extent or scope of the content of the resource."),
                    new SupportedMetadataItem(
                        "dc:Rights",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Optional,
                        false,
                        true,
                        "Information about rights held in and over the DTB."),
                    new SupportedMetadataItem(
                        "dtb:sourceEdition",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        false,
                        false,
                        "A string describing the edition of the resource (e.g., a print original, ebook, etc.) from which the DTB is derived."),
                    new SupportedMetadataItem(
                        "dtb:sourcePublisher",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        false,
                        false,
                        "The agency responsible for making available the resource (e.g., a print original, ebook, etc.) from which the DTB is derived."),
                    new SupportedMetadataItem(
                        "dtb:sourceRights",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        false,
                        false,
                        "Information about rights held in and over the resource (e.g., a print original, ebook, etc.) from which the DTB is derived."),
                    new SupportedMetadataItem(
                        "dtb:sourceTitle",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "The title of the resource (e.g., a print original, ebook, etc.) from which the DTB is derived. To be used only if different from dc:Title."),
                    new SupportedMetadataItem(
                        "dtb:narrator",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        false,
                        true,
                        "Name of the person whose recorded voice is embodied in the DTB."),
                    new SupportedMetadataItem(
                        "dtb:producer",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Optional,
                        false,
                        true,
                        "Name of the organization/production unit that created the DTB."),
                    new SupportedMetadataItem(
                        "dtb:revisionDescription",
                        SupportedMetadataFieldType.LongString,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "The changes introduced in a specific dtb:revision"),
                    new SupportedMetadataItem(
                        "dtb:revision",
                        SupportedMetadataFieldType.Integer,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "Non-negative integer value of the specific version of the DTB. Incremented each time the DTB is revised."),
                    //from mathML
                    new SupportedMetadataItem(
                        "z39-86-extension-version",
                        SupportedMetadataFieldType.Integer,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "The version of the extension to the core Z39.86 specification."),
                    new SupportedMetadataItem(
                        "DTBook-XSLTFallback",
                        SupportedMetadataFieldType.FileUri,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "The fallback XSLT file"),

                    //read-only: Tobi should fill them in for the user
                    //things such as audio format might not be known until export
                    new SupportedMetadataItem(
                        "dc:Format",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Required,
                        true,
                        true,
                        "The standard or specification to which the DTB was produced."),
                    //audioOnly, audioNCX, audioPartText, audioFullText, textPartAudio, textNCX
                    new SupportedMetadataItem(
                        "dtb:multimediaType",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Required,
                        true,
                        false,
                        "One of the six types of DTB defined in the Structure Guidelines."),
                    //audio, text, and image
                    new SupportedMetadataItem(
                        "dtb:multimediaContent",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Required,
                        true,
                        false,
                        "Summary of the general types of media used in the content of this DTB."),
                    new SupportedMetadataItem(
                        "dtb:totalTime",
                        SupportedMetadataFieldType.ClockValue,
                        MetadataOccurence.Required,
                        true,
                        false,
                        "Total playing time of all SMIL files comprising the content of the DTB."),
                    new SupportedMetadataItem(
                        "dc:Type",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Optional,
                        true,
                        true,
                        "The nature of the content of the DTB (recommended are Dublin Core keywords \"audio\", \"text\", and \"image\")."),
                    //MP4-AAC, MP3, WAV
                    new SupportedMetadataItem(
                        "dtb:audioFormat",
                        SupportedMetadataFieldType.ShortString,
                        MetadataOccurence.Recommended,
                        true,
                        true,
                        "The format in which the audio files in the DTB file set are written."),

                    //TODO: this one only appears in DTBook files and is identical to dc:Identifier
                    //do we try to synchronize the two?
                    new SupportedMetadataItem(
                        "dtb:uid",
                        SupportedMetadataFieldType.LongString,
                        MetadataOccurence.Optional,
                        false,
                        false,
                        "The unique identifier.")
                };
    }

    public class MetadataFilter
    {
        /// <summary>
        /// based on the existing metadata, return a list of metadata fields available
        /// for addition
        /// </summary>
        public List<SupportedMetadataItem> GetAvailableMetadata()
        {
            return SupportedMetadataList.MetadataList;

        }
    }
}
