using System;
using System.IO;

namespace DtdParser
{
    public class DTDAttribute : IDTDOutput
    {
    /** The name of the attribute */
        public string name;

    /** The type of the attribute (either string, DTDEnumeration or
        DTDNotationList) */
        private object m_Type;
        public object type 
        { 
            get { return m_Type;}
            set
            {
                if (!(value is string) &&
                !(value is DTDEnumeration) &&
                !(value is DTDNotationList))
            {
                throw new Exception(
                    "Must be string, DTDEnumeration or DTDNotationList");
            }

            type = value;
            } 
        }

    /** The attribute's declaration (required, fixed, implied) */
        public DTDDecl decl { get; set;}

    /** The attribute's default value (null if not declared) */
        public string defaultValue { get; set;}

        public DTDAttribute()
        {
        }

        public DTDAttribute(string aName)
        {
            name = aName;
        }

    /** Writes this attribute to an output stream */
        public void write(StreamWriter writer)
            
        {
            writer.Write(name+" ");
            if (type is string)
            {
                writer.Write(type);
            }
            else if (type is DTDEnumeration)
            {
                DTDEnumeration dtdEnum = (DTDEnumeration) type;
                dtdEnum.write(writer);
            }
            else if (type is DTDNotationList)
            {
                DTDNotationList dtdnl = (DTDNotationList) type;
                dtdnl.write(writer);
            }

            if (decl != null)
            {
                decl.write(writer);
            }

            if (defaultValue != null)
            {
                writer.Write(" \"");
                writer.Write(defaultValue);
                writer.Write("\"");
            }
            //writer.WriteLine(">");                            Bug!
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDAttribute)) return false;

            DTDAttribute other = (DTDAttribute) ob;

            if (name == null)
            {
                if (other.name != null) return false;
            }
            else
            {
                if (!name.Equals(other.name)) return false;
            }

            if (type == null)
            {
                if (other.type != null) return false;
            }
            else
            {
                if (!type.Equals(other.type)) return false;
            }

            if (decl == null)
            {
                if (other.decl != null) return false;
            }
            else
            {
                if (!decl.equals(other.decl)) return false;
            }

            if (defaultValue == null)
            {
                if (other.defaultValue != null) return false;
            }
            else
            {
                if (!defaultValue.Equals(other.defaultValue)) return false;
            }

            return true;
        }

    
    }
}