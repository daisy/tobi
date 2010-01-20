using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace DtdParser
{
    public class Tokenize
    {
        public static void Main(string[] args)
	    {
		    try
		    {
                DTDParser parser = null;
    // MAW Version 1.17
    // If it looks like the filename may be a URL, use the URL class
               /* if (args[0].IndexOf("://") > 0)
                {
                    parser = new DTDParser(new URL(args[0]), true);
                }
                else
                {
                    parser = new DTDParser(new File(args[0]), true);
                }
                * */
                //StreamReader reader = new StreamReader(args[0]);
		        StreamReader reader = new StreamReader(@"..\..\..\dtbook-2005-3-mod.dtd");
                parser = new DTDParser(reader, true);

    // Parse the DTD and ask the parser to guess the root element
                DTD dtd = parser.parse(true);

                if (dtd.rootElement != null)
                {
                    System.Console.WriteLine("Root element is probably: "+
                        dtd.rootElement.name);
                }

                foreach (DictionaryEntry de in dtd.elements)
                {
                    DTDElement elem = (DTDElement) de.Value;

                    System.Console.WriteLine("Element: "+elem.name);
                    System.Console.Write("   Content: ");
                    dumpDTDItem(elem.content);
                    System.Console.WriteLine();

                    if (elem.attributes.Count > 0)
                    {
                        System.Console.WriteLine("   Attributes: ");
                        foreach (DictionaryEntry attr_de in elem.attributes)
                        {
                            System.Console.Write("        ");
                            DTDAttribute attr = (DTDAttribute) attr_de.Value;
                            dumpAttribute(attr);
                        }
                        System.Console.WriteLine();
                    }
                }

                foreach(DictionaryEntry de in dtd.entities)
                {
                    DTDEntity entity = (DTDEntity) de.Value;

                    if (entity.isParsed) System.Console.Write("Parsed ");

                    System.Console.WriteLine("Entity: "+entity.name);
                    
                    if (entity.value != null)
                    {
                        System.Console.WriteLine("    Value: "+entity.value);
                    }

                    if (entity.externalID != null)
                    {
                        if (entity.externalID is DTDSystem)
                        {
                            System.Console.WriteLine("    System: "+
                                entity.externalID.system);
                        }
                        else
                        {
                            DTDPublic pub = (DTDPublic) entity.externalID;

                            System.Console.WriteLine("    Public: "+
                                pub.Pub+" "+pub.system);
                        }
                    }

                    if (entity.ndata != null)
                    {
                        System.Console.WriteLine("    NDATA "+entity.ndata);
                    }
                }
                foreach (DictionaryEntry de in dtd.notations)
                {
                    DTDNotation notation = (DTDNotation) de.Value;

                    System.Console.WriteLine("Notation: "+notation.name);
                    
                    if (notation.externalID != null)
                    {
                        if (notation.externalID is DTDSystem)
                        {
                            System.Console.WriteLine("    System: "+
                                notation.externalID.system);
                        }
                        else
                        {
                            DTDPublic pub = (DTDPublic) notation.externalID;

                            System.Console.Write("    Public: "+
                                pub.Pub+" ");
                            if (pub.system != null)
                            {
                                System.Console.WriteLine(pub.system);
                            }
                            else
                            {
                                System.Console.WriteLine();
                            }
                        }
                    }
                }
		    }
		    catch (Exception exc)
		    {
			    Trace.WriteLine(exc.StackTrace);
                Console.WriteLine(exc.Message);
		    }

            Console.ReadKey(); //keep the console open
	    }

        public static void dumpDTDItem(DTDItem item)
        {
            if (item == null) return;

            if (item is DTDAny)
            {
                System.Console.Write("Any");
            }
            else if (item is DTDEmpty)
            {
                System.Console.Write("Empty");
            }
            else if (item is DTDName)
            {
                System.Console.Write(((DTDName) item).value);
            }
            else if (item is DTDChoice)
            {
                System.Console.Write("(");
                List<DTDItem> items = ((DTDChoice) item).items;
                bool isFirst = true;
                foreach (DTDItem dtditem in items)
                {
                    if (!isFirst) System.Console.Write("|");
                    isFirst = false;
                    dumpDTDItem(dtditem);
                }
                System.Console.Write(")");
            }
            else if (item is DTDSequence)
            {
                System.Console.Write("(");
                List<DTDItem> items = ((DTDSequence) item).items;
                bool isFirst = true;
                foreach (DTDItem dtditem in items)
                {
                    if (!isFirst) System.Console.Write(",");
                    isFirst = false;
                    dumpDTDItem(dtditem);
                }
                System.Console.Write(")");
            }
            else if (item is DTDMixed)
            {
                System.Console.Write("(");
                List<DTDItem> items = ((DTDMixed) item).items;
                bool isFirst = true;
                foreach (DTDItem dtditem in items)
                {
                    if (!isFirst) System.Console.Write(",");
                    isFirst = false;
                    dumpDTDItem(dtditem);
                }
                System.Console.Write(")");
                
            }
            else if (item is DTDPCData)
            {
                System.Console.Write("#PCDATA");
            }

            if (item.cardinal == DTDCardinal.OPTIONAL)
            {
                System.Console.Write("?");
            }
            else if (item.cardinal == DTDCardinal.ZEROMANY)
            {
                System.Console.Write("*");
            }
            else if (item.cardinal == DTDCardinal.ONEMANY)
            {
                System.Console.Write("+");
            }
        }

        public static void dumpAttribute(DTDAttribute attr)
        {
            System.Console.Write(attr.name+" ");
            if (attr.type is string)
            {
                System.Console.Write(attr.type);
            }
            else if (attr.type is DTDEnumeration)
            {
                System.Console.Write("(");

                List<string> items = ((DTDEnumeration) attr.type).items;
                bool isFirst = true;
                foreach (string dtditem in items)
                {
                    if (!isFirst) System.Console.Write(",");
                    isFirst = false;
                    System.Console.Write(dtditem);
                }
                System.Console.Write(")");
                
            }
            else if (attr.type is DTDNotationList)
            {
                System.Console.Write("Notation (");
                
                List<string> items = ((DTDNotationList)attr.type).items;
                bool isFirst = true;
                foreach (string dtditem in items)
                {
                    if (!isFirst) System.Console.Write(",");
                    isFirst = false;
                    System.Console.Write(dtditem);
                }
                System.Console.Write(")");
            }

            if (attr.decl != null)
            {
                System.Console.Write(" "+attr.decl.name);
            }

            if (attr.defaultValue != null)
            {
                System.Console.Write(" "+attr.defaultValue);
            }

            System.Console.WriteLine();
        }
    }
}