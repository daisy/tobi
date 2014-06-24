using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
	public class DTD : IDTDOutput
	{
	/** Contains all the elements defined in the DTD */
	    public Hashtable Elements { get; set;}

	/** Contains all the entities defined in the DTD */
	    public Hashtable Entities { get; set;}

	/** Contains all the notations defined in the DTD */
        public Hashtable Notations { get; set; }

	/** Contains parsed DTD's for any external entity DTD declarations */
        public Hashtable ExternalDTDs { get; set; }

	/** Contains all the items defined in the DTD in their original order */
        //in practice, this list contains any of the following object types:
        //DTDAttlist, DTDNotation, DTDElement, DTDEntity, DTDProcessingInstruction, DTDComment
        public List<object> Items { get; set; }

	/** Contains the element that is most likely the root element  or null
	    if the root element can't be determined.  */
        public DTDElement RootElement { get; set; }

	/** Creates a new DTD */
	    public DTD()
	    {
	        Elements = new Hashtable();
	        Entities = new Hashtable();
	        Notations = new Hashtable();
	        ExternalDTDs = new Hashtable();
	        Items = new List<object>();
	    }

	/** Writes the DTD to an output writer in standard DTD format (the format
	 *  the parser normally reads).
	 *  @param outWriter The writer where the DTD will be written
	 */
	    public void Write(StreamWriter writer)
	    {
	        foreach (object item in Items)
            {
                IDTDOutput dtdOutput = (IDTDOutput) item;
                dtdOutput.Write(writer);
            }

	    }

	/** Returns true if this object is equal to another */
	    public override bool Equals(object ob)
	    {
	        if (this == ob) return true;

	        if (!(ob is DTD)) return false;

	        DTD otherDTD = (DTD) ob;

	        return Items.Equals(otherDTD.Items);
	    }
	

	/** Retrieves a list of items of a particular type */
	    public List<object> GetItemsByType(Type itemType)
	    {
	        return Items.FindAll(s => s.GetType() == itemType);
	    }
	}
}