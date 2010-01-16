package com.wutka.dtd;

import java.io.*;
import java.util.*;
import java.net.URL;

/** Example program to read a DTD and print out its object model
 *
 * @author Mark Wutka
 * @version $Revision: 1.17 $ $Date: 2002/07/28 13:33:12 $ by $Author: wutka $
 */

class Tokenize
{
	public static void main(string[] args)
	{
		try
		{
            DTDParser parser = null;
// MAW Version 1.17
// If it looks like the filename may be a URL, use the URL class
            if (args[0].indexOf("://") > 0)
            {
                parser = new DTDParser(new URL(args[0]), true);
            }
            else
            {
                parser = new DTDParser(new File(args[0]), true);
            }

// Parse the DTD and ask the parser to guess the root element
            DTD dtd = parser.parse(true);

            if (dtd.rootElement != null)
            {
                System.writer.WriteLine("Root element is probably: "+
                    dtd.rootElement.name);
            }

            Enumeration e = dtd.elements.elements();

            while (e.hasMoreElements())
            {
                DTDElement elem = (DTDElement) e.nextElement();

                System.writer.WriteLine("Element: "+elem.name);
                System.writer.Write("   Content: ");
                dumpDTDItem(elem.content);
                System.writer.WriteLine();

                if (elem.attributes.size() > 0)
                {
                    System.writer.WriteLine("   Attributes: ");
                    Enumeration attrs = elem.attributes.elements();
                    while (attrs.hasMoreElements())
                    {
                        System.writer.Write("        ");
                        DTDAttribute attr = (DTDAttribute) attrs.nextElement();
                        dumpAttribute(attr);
                    }
                    System.writer.WriteLine();
                }
            }

            e = dtd.entities.elements();

            while (e.hasMoreElements())
            {
                DTDEntity entity = (DTDEntity) e.nextElement();

                if (entity.isParsed) System.writer.Write("Parsed ");

                System.writer.WriteLine("Entity: "+entity.name);
                
                if (entity.value != null)
                {
                    System.writer.WriteLine("    Value: "+entity.value);
                }

                if (entity.externalID != null)
                {
                    if (entity.externalID instanceof DTDSystem)
                    {
                        System.writer.WriteLine("    System: "+
                            entity.externalID.system);
                    }
                    else
                    {
                        DTDPublic pub = (DTDPublic) entity.externalID;

                        System.writer.WriteLine("    Public: "+
                            pub.pub+" "+pub.system);
                    }
                }

                if (entity.ndata != null)
                {
                    System.writer.WriteLine("    NDATA "+entity.ndata);
                }
            }
            e = dtd.notations.elements();

            while (e.hasMoreElements())
            {
                DTDNotation notation = (DTDNotation) e.nextElement();

                System.writer.WriteLine("Notation: "+notation.name);
                
                if (notation.externalID != null)
                {
                    if (notation.externalID instanceof DTDSystem)
                    {
                        System.writer.WriteLine("    System: "+
                            notation.externalID.system);
                    }
                    else
                    {
                        DTDPublic pub = (DTDPublic) notation.externalID;

                        System.writer.Write("    Public: "+
                            pub.pub+" ");
                        if (pub.system != null)
                        {
                            System.writer.WriteLine(pub.system);
                        }
                        else
                        {
                            System.writer.WriteLine();
                        }
                    }
                }
            }
		}
		catch (Exception exc)
		{
			exc.printStackTrace(System.out);
		}
	}

    public static void dumpDTDItem(DTDItem item)
    {
        if (item == null) return;

        if (item instanceof DTDAny)
        {
            System.writer.Write("Any");
        }
        else if (item instanceof DTDEmpty)
        {
            System.writer.Write("Empty");
        }
        else if (item instanceof DTDName)
        {
            System.writer.Write(((DTDName) item).value);
        }
        else if (item instanceof DTDChoice)
        {
            System.writer.Write("(");
            DTDItem[] items = ((DTDChoice) item).getItems();

            for (int i=0; i < items.length; i++)
            {
                if (i > 0) System.writer.Write("|");
                dumpDTDItem(items[i]);
            }
            System.writer.Write(")");
        }
        else if (item instanceof DTDSequence)
        {
            System.writer.Write("(");
            DTDItem[] items = ((DTDSequence) item).getItems();

            for (int i=0; i < items.length; i++)
            {
                if (i > 0) System.writer.Write(",");
                dumpDTDItem(items[i]);
            }
            System.writer.Write(")");
        }
        else if (item instanceof DTDMixed)
        {
            System.writer.Write("(");
            DTDItem[] items = ((DTDMixed) item).getItems();

            for (int i=0; i < items.length; i++)
            {
                if (i > 0) System.writer.Write(",");
                dumpDTDItem(items[i]);
            }
            System.writer.Write(")");
        }
        else if (item instanceof DTDPCData)
        {
            System.writer.Write("#PCDATA");
        }

        if (item.cardinal == DTDCardinal.OPTIONAL)
        {
            System.writer.Write("?");
        }
        else if (item.cardinal == DTDCardinal.ZEROMANY)
        {
            System.writer.Write("*");
        }
        else if (item.cardinal == DTDCardinal.ONEMANY)
        {
            System.writer.Write("+");
        }
    }

    public static void dumpAttribute(DTDAttribute attr)
    {
        System.writer.Write(attr.name+" ");
        if (attr.type instanceof string)
        {
            System.writer.Write(attr.type);
        }
        else if (attr.type instanceof DTDEnumeration)
        {
            System.writer.Write("(");
            string[] items = ((DTDEnumeration) attr.type).getItems();

            for (int i=0; i < items.length; i++)
            {
                if (i > 0) System.writer.Write(",");
                System.writer.Write(items[i]);
            }
            System.writer.Write(")");
        }
        else if (attr.type instanceof DTDNotationList)
        {
            System.writer.Write("Notation (");
            string[] items = ((DTDNotationList) attr.type).getItems();

            for (int i=0; i < items.length; i++)
            {
                if (i > 0) System.writer.Write(",");
                System.writer.Write(items[i]);
            }
            System.writer.Write(")");
        }

        if (attr.decl != null)
        {
            System.writer.Write(" "+attr.decl.name);
        }

        if (attr.defaultValue != null)
        {
            System.writer.Write(" "+attr.defaultValue);
        }

        System.writer.WriteLine();
    }
}
