package com.wutka.dtd;

import java.io.*;

/** Represents an external System ID in an entity declaration
 *
 * @author Mark Wutka
 * @version $Revision: 1.16 $ $Date: 2002/07/19 01:20:11 $ by $Author: wutka $
 */

namespace DtdParser{ public class DTDSystem : DTDExternalID
{
    public DTDSystem()
    {
    }

/** Writes out a declaration for this SYSTEM ID */
    public void write(StreamWriter writer)
    {
        if (system != null)
        {
            writer.Write("SYSTEM \"");
            writer.Write(system);
            writer.Write("\"");
        }
    }

    public bool equals(object ob)
    {
        if (ob == this) return true;
        if (!(ob instanceof DTDSystem)) return false;

        return super.equals(ob);
    }
}
