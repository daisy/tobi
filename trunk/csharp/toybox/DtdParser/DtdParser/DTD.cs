using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DtdParser
{
	public class DTD : IDTDOutput
	{
	/** Contains all the elements defined in the DTD */
	    public Hashtable elements { get; set;}

	/** Contains all the entities defined in the DTD */
	    public Hashtable entities { get; set;}

	/** Contains all the notations defined in the DTD */
        public Hashtable notations { get; set; }

	/** Contains parsed DTD's for any external entity DTD declarations */
        public Hashtable externalDTDs { get; set; }

	/** Contains all the items defined in the DTD in their original order */
	    //TODO what datatype?
        public List<object> items { get; set; }

	/** Contains the element that is most likely the root element  or null
	    if the root element can't be determined.  */
        public DTDElement rootElement { get; set; }

	/** Creates a new DTD */
	    public DTD()
	    {
	        elements = new Hashtable();
	        entities = new Hashtable();
	        notations = new Hashtable();
	        externalDTDs = new Hashtable();
	        items = new List<object>();
	    }

	/** Writes the DTD to an output writer in standard DTD format (the format
	 *  the parser normally reads).
	 *  @param outWriter The writer where the DTD will be written
	 */
	    public void write(StreamWriter writer)
	    {
	        foreach (object item in items)
            {
                IDTDOutput dtdOutput = (IDTDOutput) item;
                dtdOutput.write(writer);
            }

	    }

	/** Returns true if this object is equal to another */
	    public bool equals(object ob)
	    {
	        if (this == ob) return true;

	        if (!(ob is DTD)) return false;

	        DTD otherDTD = (DTD) ob;

	        return items.Equals(otherDTD.items);
	    }
	

	/** Retrieves a list of items of a particular type */
	    public List<object> getItemsByType(Type itemType)
	    {
	        return items.FindAll(s => s.GetType() == itemType);
	    }
	}
}