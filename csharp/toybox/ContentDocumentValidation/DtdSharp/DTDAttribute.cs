using System;
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDAttribute : IDTDOutput
    {
    /** The name of the attribute */
        public string Name { get; set;}

    /** The type of the attribute (either string, DTDEnumeration or
        DTDNotationList) */
        private object m_Type;
        public object Type 
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

            m_Type = value;
            } 
        }

    /** The attribute's declaration (required, fixed, implied) */
        public DTDDecl Decl { get; set;}

    /** The attribute's default value (null if not declared) */
        public string DefaultValue { get; set;}

        public DTDAttribute()
        {
        }

        public DTDAttribute(string aName)
        {
            Name = aName;
        }

    /** Writes this attribute to an output stream */
        public void Write(StreamWriter writer)
        {
            writer.Write(Name+" ");
            if (Type is string)
            {
                writer.Write(Type);
            }
            else if (Type is DTDEnumeration)
            {
                DTDEnumeration dtdEnum = (DTDEnumeration) Type;
                dtdEnum.Write(writer);
            }
            else if (Type is DTDNotationList)
            {
                DTDNotationList dtdnl = (DTDNotationList) Type;
                dtdnl.Write(writer);
            }

            if (Decl != null)
            {
                Decl.Write(writer);
            }

            if (DefaultValue != null)
            {
                writer.Write(" \"");
                writer.Write(DefaultValue);
                writer.Write("\"");
            }
            //writer.WriteLine(">");                           original java comment: "Bug!"
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDAttribute)) return false;

            DTDAttribute other = (DTDAttribute) ob;

            if (Name == null)
            {
                if (other.Name != null) return false;
            }
            else
            {
                if (!Name.Equals(other.Name)) return false;
            }

            if (Type == null)
            {
                if (other.Type != null) return false;
            }
            else
            {
                if (!Type.Equals(other.Type)) return false;
            }

            if (Decl == null)
            {
                if (other.Decl != null) return false;
            }
            else
            {
                if (!Decl.Equals(other.Decl)) return false;
            }

            if (DefaultValue == null)
            {
                if (other.DefaultValue != null) return false;
            }
            else
            {
                if (!DefaultValue.Equals(other.DefaultValue)) return false;
            }

            return true;
        }

    
    }
}