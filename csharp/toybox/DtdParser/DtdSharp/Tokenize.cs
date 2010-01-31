#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

#endregion

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */

namespace DtdSharp
{
    public class Tokenize
    {
        public static void TestParseDtd(string file, bool writeToLog)
        {
            try
            {
                DTDParser parser = null;
                // MAW Version 1.17
                // If it looks like the filename may be a URL, use the URL class
                if (file.IndexOf("://") > 0)
                {
                    parser = new DTDParser(new Uri(file), true);
                }
                else
                {
                    parser = new DTDParser(file, true);
                }
                

                // Parse the DTD and ask the parser to guess the root element
                DTD dtd = parser.Parse(true);

                FileStream ostrm = null;
                StreamWriter writer = null;
                TextWriter oldOut = null;
                
                if (writeToLog)
                {
                    oldOut = Console.Out;
                    ostrm = new FileStream("./log.txt", FileMode.OpenOrCreate, FileAccess.Write);
                    writer = new StreamWriter(ostrm);
                    Console.SetOut(writer);   
                }
                if (dtd.RootElement != null)
                {
                    Console.WriteLine("Root element is probably: " +
                                      dtd.RootElement.Name);
                }

                foreach (DictionaryEntry de in dtd.Elements)
                {
                    DTDElement elem = (DTDElement) de.Value;

                    Console.WriteLine("Element: " + elem.Name);
                    Console.Write("   Content: ");
                    DumpDtdItem(elem.Content);
                    Console.WriteLine();

                    if (elem.Attributes.Count > 0)
                    {
                        Console.WriteLine("   Attributes: ");
                        foreach (DictionaryEntry attr_de in elem.Attributes)
                        {
                            Console.Write("        ");
                            DTDAttribute attr = (DTDAttribute) attr_de.Value;
                            DumpAttribute(attr);
                        }
                        Console.WriteLine();
                    }
                }

                
                foreach (DictionaryEntry de in dtd.Entities)
                {
                    DTDEntity entity = (DTDEntity) de.Value;

                    if (entity.IsParsed) Console.Write("Parsed ");
                    
                    Console.WriteLine("Entity: " + entity.Name);

                    if (entity.Value != null)
                    {
                        Console.WriteLine("    Value: " + entity.Value);
                    }

                    if (entity.ExternalId != null)
                    {
                        if (entity.ExternalId is DTDSystem)
                        {
                            Console.WriteLine("    System: " + entity.ExternalId.System);
                        }
                        else
                        {
                            DTDPublic pub = (DTDPublic) entity.ExternalId;
                            Console.WriteLine("    Public: " + pub.Pub + " " + pub.System);
                        }
                    }

                    if (entity.Ndata != null)
                    {
                        Console.WriteLine("    NDATA " + entity.Ndata);
                    }
                }
                foreach (DictionaryEntry de in dtd.Notations)
                {
                    DTDNotation notation = (DTDNotation) de.Value;

                    Console.WriteLine("Notation: " + notation.Name);

                    if (notation.ExternalId != null)
                    {
                        if (notation.ExternalId is DTDSystem)
                        {
                            Console.WriteLine("    System: " +
                                              notation.ExternalId.System);
                        }
                        else
                        {
                            DTDPublic pub = (DTDPublic) notation.ExternalId;

                            Console.Write("    Public: " +
                                          pub.Pub + " ");
                            if (pub.System != null)
                            {
                                Console.WriteLine(pub.System);
                            }
                            else
                            {
                                Console.WriteLine();
                            }
                        }
                    }
                }
                if (writeToLog)
                {
                    Console.SetOut(oldOut);
                    writer.Close();
                    ostrm.Close();
                }
            }
            catch (Exception exc)
            {
                Trace.WriteLine(exc.StackTrace);
                Console.WriteLine(exc.Message);
            }


            Console.WriteLine("Done");
            Console.ReadKey(); //keep the console open
        }

        public static void DumpDtdItem(DTDItem item)
        {
            if (item == null) return;

            if (item is DTDAny)
            {
                Console.Write("Any");
            }
            else if (item is DTDEmpty)
            {
                Console.Write("Empty");
            }
            else if (item is DTDName)
            {
                Console.Write(((DTDName) item).Value);
            }
            else if (item is DTDChoice)
            {
                Console.Write("(");
                List<DTDItem> items = ((DTDChoice) item).Items;
                bool isFirst = true;
                foreach (DTDItem dtditem in items)
                {
                    if (!isFirst) Console.Write("|");
                    isFirst = false;
                    DumpDtdItem(dtditem);
                }
                Console.Write(")");
            }
            else if (item is DTDSequence)
            {
                Console.Write("(");
                List<DTDItem> items = ((DTDSequence) item).Items;
                bool isFirst = true;
                foreach (DTDItem dtditem in items)
                {
                    if (!isFirst) Console.Write(",");
                    isFirst = false;
                    DumpDtdItem(dtditem);
                }
                Console.Write(")");
            }
            else if (item is DTDMixed)
            {
                Console.Write("(");
                List<DTDItem> items = ((DTDMixed) item).Items;
                bool isFirst = true;
                foreach (DTDItem dtditem in items)
                {
                    if (!isFirst) Console.Write(",");
                    isFirst = false;
                    DumpDtdItem(dtditem);
                }
                Console.Write(")");
            }
            else if (item is DTDPCData)
            {
                Console.Write("#PCDATA");
            }

            if (item.Cardinal == DTDCardinal.ZEROONE)
            {
                Console.Write("?");
            }
            else if (item.Cardinal == DTDCardinal.ZEROMANY)
            {
                Console.Write("*");
            }
            else if (item.Cardinal == DTDCardinal.ONEMANY)
            {
                Console.Write("+");
            }
        }

        public static void DumpAttribute(DTDAttribute attr)
        {
            Console.Write(attr.Name + " ");
            if (attr.Type is string)
            {
                Console.Write(attr.Type);
            }
            else if (attr.Type is DTDEnumeration)
            {
                Console.Write("(");

                List<string> items = ((DTDEnumeration) attr.Type).Items;
                bool isFirst = true;
                foreach (string dtditem in items)
                {
                    if (!isFirst) Console.Write(",");
                    isFirst = false;
                    Console.Write(dtditem);
                }
                Console.Write(")");
            }
            else if (attr.Type is DTDNotationList)
            {
                Console.Write("Notation (");

                List<string> items = ((DTDNotationList) attr.Type).Items;
                bool isFirst = true;
                foreach (string dtditem in items)
                {
                    if (!isFirst) Console.Write(",");
                    isFirst = false;
                    Console.Write(dtditem);
                }
                Console.Write(")");
            }

            if (attr.Decl != null)
            {
                Console.Write(" " + attr.Decl.Name);
            }

            if (attr.DefaultValue != null)
            {
                Console.Write(" " + attr.DefaultValue);
            }

            Console.WriteLine();
        }
    }
}