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

    public class SupportedMetadataItem
    {
        public SupportedMetadataFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public string Name { get; set; }
        
        public SupportedMetadataItem(string name, SupportedMetadataFieldType fieldType)
        {
            Name = name;
            FieldType = fieldType;
            IsRequired = false;
            IsReadOnly = false;
        }
        public SupportedMetadataItem(string name, SupportedMetadataFieldType fieldType,
            bool isRequired) : this(name, fieldType)
        {
            IsRequired = isRequired;
        }
        public SupportedMetadataItem(string name, SupportedMetadataFieldType fieldType, 
            bool isRequired, bool isReadOnly) : this(name, fieldType)
        {
            IsRequired = isRequired;
            IsReadOnly = isReadOnly;
        }
    }

    public class CreateSupportedMetadataList
    {
        public CreateSupportedMetadataList(List<SupportedMetadataItem> thelist)
        {
            thelist.Add(new SupportedMetadataItem("dc:Date", SupportedMetadataFieldType.Date, true));
            
            thelist.Add(new SupportedMetadataItem("dtb:sourceDate", SupportedMetadataFieldType.Date));
            thelist.Add(new SupportedMetadataItem("dtb:producedDate", SupportedMetadataFieldType.Date));
            thelist.Add(new SupportedMetadataItem("dtb:revisionDate", SupportedMetadataFieldType.Date));

            thelist.Add(new SupportedMetadataItem("dc:Title", SupportedMetadataFieldType.ShortString, true));
            thelist.Add(new SupportedMetadataItem("dc:Publisher", SupportedMetadataFieldType.ShortString, true));
            thelist.Add(new SupportedMetadataItem("dc:Language", SupportedMetadataFieldType.LanguageCode, true));
            thelist.Add(new SupportedMetadataItem("dc:Identifier", SupportedMetadataFieldType.ShortString, true));
            
            thelist.Add(new SupportedMetadataItem("dc:Creator", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dc:Subject", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dc:Description", SupportedMetadataFieldType.LongString));
            thelist.Add(new SupportedMetadataItem("dc:Contributor", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dc:Source", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dc:Relation", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dc:Coverage", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dc:Rights", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dtb:sourceEdition", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dtb:sourcePublisher", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dtb:sourceRights", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dtb:sourceTitle", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dtb:narrator", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dtb:producer", SupportedMetadataFieldType.ShortString));
            thelist.Add(new SupportedMetadataItem("dtb:revisionDescription", SupportedMetadataFieldType.LongString));

            thelist.Add(new SupportedMetadataItem("dtb:revision", SupportedMetadataFieldType.Integer));

            //from mathML
            thelist.Add(new SupportedMetadataItem("z39-86-extension-version", SupportedMetadataFieldType.Integer));
            thelist.Add(new SupportedMetadataItem("DTBook-XSLTFallback", SupportedMetadataFieldType.FileUri));
            
            //read-only: Tobi should fill them in for the user
            //things such as audio format might not be known until export
            thelist.Add(new SupportedMetadataItem("dc:Format", SupportedMetadataFieldType.ShortString, true, true));
            //audioOnly, audioNCX, audioPartText, audioFullText, textPartAudio, textNCX
            thelist.Add(new SupportedMetadataItem("dtb:multimediaType", SupportedMetadataFieldType.ShortString, true, true));
            //audio, text, and image
            thelist.Add(new SupportedMetadataItem("dtb:multimediaContent", SupportedMetadataFieldType.ShortString, true, true));
            thelist.Add(new SupportedMetadataItem("dtb:totalTime", SupportedMetadataFieldType.ClockValue, true, true));
            thelist.Add(new SupportedMetadataItem("dc:Type", SupportedMetadataFieldType.ShortString, false, true));
            //MP4-AAC, MP3, WAV
            thelist.Add(new SupportedMetadataItem("dtb:audioFormat", SupportedMetadataFieldType.ShortString, false, true));
            
            //TODO: this one only appears in DTBook files and is identical to dc:Identifier
            //do we try to synchronize the two?
            thelist.Add(new SupportedMetadataItem("dtb:uid", SupportedMetadataFieldType.LongString));

        }

    }
}
