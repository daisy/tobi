using System;
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public class DTDEntity : IDTDOutput
    {
        public string Name { get; set;}
        public bool IsParsed { get; set; }
        public string Value { get; set; }
        public DTDExternalID ExternalId { get; set; }
        public string Ndata { get; set; }
        public object DefaultLocation { get; set; }
        
        public DTDEntity(string aName)
        {
            Name = aName;
        }

        public DTDEntity(string aName, object aDefaultLocation)
        {
            Name = aName;
            DefaultLocation = aDefaultLocation;
        }

        /** Writes out an entity declaration for this entity */
        public void Write(StreamWriter writer)
        {
            writer.Write("<!ENTITY ");
            if (IsParsed)
            {
                writer.Write(" % ");
            }
            writer.Write(Name);

            if (Value != null)
            {
                char quoteChar = '"';
                if (Value.IndexOf(quoteChar) >= 0) quoteChar='\'';
                writer.Write(quoteChar);
                writer.Write(Value);
                writer.Write(quoteChar);
            }
            else
            {
                ExternalId.Write(writer);
                if (Ndata != null)
                {
                    writer.Write(" NDATA ");
                    writer.Write(Ndata);
                }
            }
            writer.WriteLine(">");
        }

        public string GetExternalId()
        {
            return (ExternalId.System);
        }

        public StreamReader GetReader()
        {
            // MAW Ver 1.19 - Added check for externalID == null
            if (ExternalId == null)
            {
                return null;
            }

            StreamReader rd = GetReader(ExternalId.System);
            return rd;
        }

        //TODO: what's going on here? 
        //there's a fallback chain of defaultLocation (string/Uri/null) then entityName
        public StreamReader GetReader(string entityName)
        {
            try
            {
                if (DefaultLocation != null)
                {
                    if (DefaultLocation is string)
                    {
                        string loc = (string) DefaultLocation;

                        return new StreamReader(loc);
                    }
                   /* TODO: URI support
                    if (defaultLocation is Uri)
                    {
                        // MAW Version 1.17
                        // Changed to construct new URL based on default
                        // location plus the entity name just like is done
                        // with the File-based name. This allows parsing of
                        // a URL-based DTD file that references other files either
                        // relatively or absolutely.
                        Uri url = new Uri((Uri) defaultLocation, entityName);

                        return new StreamReader(url);

                    }*/
                }
                return  new StreamReader(entityName);
            }
            catch (Exception ignore)
            {
            }

            /*TODO: URL support
            try
            {
                Uri url = new Uri(entityName);

                StreamReader reader = new StreamReader(url);

                return reader;
            }
            catch (Exception ignore)
            {
            }*/

            return null;
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDEntity)) return false;

            DTDEntity other = (DTDEntity) ob;

            if (Name == null)
            {
                if (other.Name != null) return false;
            }
            else
            {
                if (!Name.Equals(other.Name)) return false;
            }

            if (IsParsed != other.IsParsed) return false;


            if (Value == null)
            {
                if (other.Value != null) return false;
            }
            else
            {
                if (!Value.Equals(other.Value)) return false;
            }

            if (ExternalId == null)
            {
                if (other.ExternalId != null) return false;
            }
            else
            {
                if (!ExternalId.Equals(other.ExternalId)) return false;
            }

            if (Ndata == null)
            {
                if (other.Ndata != null) return false;
            }
            else
            {
                if (!Ndata.Equals(other.Ndata)) return false;
            }

            return true;
        }
    }
}