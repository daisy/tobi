using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Globalization;

/* Required Metadata (from Z39.86-2005)
 * dc:Title
 * dc:Publisher
 * dc:Date - YYYY[-MM[-DD]]
 *  should we be "smart" and convert other date formats?  probably add this at some point.
 * dc:Format "ANSI/NISO z39.86-2005"
 *  @scheme for format - does Urakawa SDK support it?
 * dc:Identifier
 * dc:Language RFC 3066 (validated as a string only)
 *  could be a cool searchable list of all RFC 3066 codes.  in the future.
 * dtb:multimediaType audioOnly, audioNCX, audioPartText, audioFullText, textPartAudio, textNCX
 * dtb:multimediaContent audio, text, image
 * dtb:totalTime a SMIL clock value
 * 
 * Optional Metadata (from Z39.86-2005)
 * dc:Creator
 *  @role, @file-as -- does Urakawa SDK suppor these?
 * dc:Subject
 * dc:Description
 * dc:Contributor
 *  @role, @file-as -- does Urakawa SDK support these?
 * dc:Type - From the Dublin Core Type list (recommended are audio, text, and image)
 * dc:Source - use the ISBN if available - the UI should recommend this but not require it
 * dc:Relation
 * dc:Coverage
 * dc:Rights
 * dtb:sourceDate - YYYY[-MM[-DD]]
 * dtb:sourceEdition
 * dtb:sourcePublisher
 * dtb:sourceRights
 * dtb:sourceTitle
 * dtb:narrator
 * dtb:producer
 * dtb:producedDate - YYYY[-MM[-DD]] - Date of first generation of the complete DTB
 * dtb:revision
 * dtb:revisionDate - YYYY[-MM[-DD]] - Date associated with the specific dtb:revision
 * dtb:revisionDescription
 * dtb:audioFormat - such as MP4-AAC, MP3, WAV
 * 
 * Of these, it seems that some will (should) be automatically filled in by Tobi.
 * They are read-only fields for now.
 * dc:Format, dtb:multimediaType, dtb:multimediaContent, dtb:totalTime, dc:Type, dtb:audioFormat
 * 
 * It would also make sense for Tobi to fill in the revisionDate whenever the revision is incremented. 
 * However, it is unclear right now how the dtb:revision value is calculated (in the metadata it is 
 * an editable string)
 * 
 * */

namespace Frustration
{
    public abstract class BaseValidationRule : ValidationRule
    {
        public string ErrorMessage { get; set; }
    }

    //this simply checks that the value is a non-empty string
    public class RequiredStringValidationRule : BaseValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (((string)value).Length > 0)
                return new ValidationResult(true, null);
            else
                return new ValidationResult(false, ErrorMessage);
        }
    }

    //YYYY-MM-DD is the required format
    //however, the string is allowed to be empty
    public class OptionalDateValidationRule : BaseValidationRule
    {
        public override ValidationResult Validate(object obj, CultureInfo cultureInfo)
        {
            if (obj == null)
            {
                return new ValidationResult(true, null);
            }
            else
            {
                //if there is text in this field, it should follow the rules
                RequiredDateValidationRule rule = new RequiredDateValidationRule();
                return rule.Validate(obj, cultureInfo);
            }
        }
    }

    //YYYY-MM-DD is the required format
    public class RequiredDateValidationRule : BaseValidationRule
    {
        public override ValidationResult Validate(object obj, CultureInfo cultureInfo)
        {
            if (obj == null)
                return new ValidationResult(false, ErrorMessage);

            string date = (string)obj;
            
            //Require at least the year field
            if (date.Length < 4 || date.Length > 10)
                return new ValidationResult(false, ErrorMessage);

            string[] dateArray = date.Split('-');
            int year = 0;
            int month = 0;
            int day = 0;

            //the year has to be 4 digits
            if (dateArray[0].Length != 4)
                return new ValidationResult(false, ErrorMessage);
            
            //the year has to be digits
            try
            {
                year = Convert.ToInt32(dateArray[0]);
            }
            catch
            {
                return new ValidationResult(false, ErrorMessage);
            }
                
            //check for a month value (it's optional)
            if (dateArray.Length >= 2)
            {
                //the month has to be numeric
                try
                {
                    month = Convert.ToInt32(dateArray[1]);
                }
                catch
                {
                    return new ValidationResult(false, ErrorMessage);
                }
                //the month has to be in this range
                if (month < 1 || month > 12)
                    return new ValidationResult(false, ErrorMessage);
            }
            //check for a day value (it's optional but only if a month is specified)
            if (dateArray.Length == 3)
            {
                //the day has to be a number
                try
                {
                    day = Convert.ToInt32(dateArray[2]);
                }
                catch
                {
                    return new ValidationResult(false, ErrorMessage);
                }
                //it has to be in this range
                if (day < 1 || day > 31)
                    return new ValidationResult(false, ErrorMessage);
            }
            
            return new ValidationResult(true, null);
        }
    }

}