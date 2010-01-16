package com.wutka.dtd;

import java.io.*;
import java.util.*;

/** Represents a sequence in an element's content.
 * A sequence is declared in the DTD as (value1,value2,value3,etc.)
 *
 * @author Mark Wutka
 * @version $Revision: 1.16 $ $Date: 2002/07/19 01:20:11 $ by $Author: wutka $
 */
namespace DtdParser{ public class DTDSequence : DTDContainer
{
    public DTDSequence()
    {
    }

/** Writes out a declaration for this sequence */
    public void write(StreamWriter writer)
        
    {
        writer.Write("(");

        Enumeration e = getItemsVec().elements();
        bool isFirst = true;

        while (e.hasMoreElements())
        {
            if (!isFirst) writer.Write(",");
            isFirst = false;

            DTDItem item = (DTDItem) e.nextElement();
            item.write(out);
        }
        writer.Write(")");
        cardinal.write(out);
    }

    public bool equals(object ob)
    {
        if (ob == this) return true;
        if (!(ob instanceof DTDSequence)) return false;

        return super.equals(ob);
    }
}
