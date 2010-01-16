using System;
using System.IO;

namespace DtdParser
{
    public class DTDEntity : IDTDOutput
    {
        public string name { get; set;}
        public bool isParsed { get; set; }
        public string value { get; set; }
        public DTDExternalID externalID { get; set; }
        public string ndata { get; set; }
        public object defaultLocation { get; set; }

        
        public DTDEntity(string aName)
        {
            name = aName;
        }

        public DTDEntity(string aName, object aDefaultLocation)
        {
            name = aName;
            defaultLocation = aDefaultLocation;
        }

        /** Writes out an entity declaration for this entity */
        public void write(StreamWriter writer)
        {
            writer.Write("<!ENTITY ");
            if (isParsed)
            {
                writer.Write(" % ");
            }
            writer.Write(name);

            if (value != null)
            {
                char quoteChar = '"';
                if (value.IndexOf(quoteChar) >= 0) quoteChar='\'';
                writer.Write(quoteChar);
                writer.Write(value);
                writer.Write(quoteChar);
            }
            else
            {
                externalID.write(writer);
                if (ndata != null)
                {
                    writer.Write(" NDATA ");
                    writer.Write(ndata);
                }
            }
            writer.WriteLine(">");
        }

        public string getExternalId()
        {
            return (externalID.system);
        }

        public StreamReader getReader()
        {
            // MAW Ver 1.19 - Added check for externalID == null
            if (externalID == null)
            {
                return null;
            }

            StreamReader rd = getReader(externalID.system);

            return rd;
        }

        //TODO: what's going on here? 
        //there's a fallback chain of defaultLocation (string/Uri/null) then entityName
        public StreamReader getReader(string entityName)
        {
            try
            {
                if (defaultLocation != null)
                {
                    if (defaultLocation is string)
                    {
                        string loc = (string) defaultLocation;

                        StreamReader reader = new StreamReader(loc);

                        return reader;
                    }
                    else if (defaultLocation is Uri)
                    {
                        // MAW Version 1.17
                        // Changed to construct new URL based on default
                        // location plus the entity name just like is done
                        // with the File-based name. This allows parsing of
                        // a URL-based DTD file that references other files either
                        // relatively or absolutely.
                        Uri url = new Uri((Uri) defaultLocation, entityName);

                        StreamReader reader = new StreamReader(url);

                        return reader;
                    }
                }
                StreamReader reader = new StreamReader(entityName);

                return reader;
            }
            catch (Exception ignore)
            {
            }

            try
            {
                Uri url = new Uri(entityName);

                StreamReader reader = new StreamReader(url);

                return reader;
            }
            catch (Exception ignore)
            {
            }

            return null;
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDEntity)) return false;

            DTDEntity other = (DTDEntity) ob;

            if (name == null)
            {
                if (other.name != null) return false;
            }
            else
            {
                if (!name.Equals(other.name)) return false;
            }

            if (isParsed != other.isParsed) return false;


            if (value == null)
            {
                if (other.value != null) return false;
            }
            else
            {
                if (!value.Equals(other.value)) return false;
            }

            if (externalID == null)
            {
                if (other.externalID != null) return false;
            }
            else
            {
                if (!externalID.equals(other.externalID)) return false;
            }

            if (ndata == null)
            {
                if (other.ndata != null) return false;
            }
            else
            {
                if (!ndata.Equals(other.ndata)) return false;
            }

            return true;
        }
    }
}