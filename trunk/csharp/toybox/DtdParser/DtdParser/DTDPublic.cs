package com.wutka.dtd;

import java.io.*;

/** Represents an external Public ID in an entity declaration
 *
 * @author Mark Wutka
 * @version $Revision: 1.16 $ $Date: 2002/07/19 01:20:11 $ by $Author: wutka $
 */

namespace DtdParser{ public class DTDPublic : DTDExternalID
{
    public string pub;

    public DTDPublic()
    {
    }

/** Writes out a public external ID declaration */
    public void write(StreamWriter writer)
        
    {
        writer.Write("PUBLIC \"");
        writer.Write(pub);
        writer.Write("\"");
        if (system != null)
        {
            writer.Write(" \"");
            writer.Write(system);
            writer.Write("\"");
        }
    }

    public bool equals(object ob)
    {
        if (ob == this) return true;
        if (!(ob instanceof DTDPublic)) return false;

        if (!super.equals(ob)) return false;

        DTDPublic other = (DTDPublic) ob;

        if (pub == null)
        {
            if (other.pub != null) return false;
        }
        else
        {
            if (!pub.equals(other.pub)) return false;
        }

        return true;
    }

/** Sets the public identifier */
    public void setPub(string aPub)
    {
        pub = aPub;
    }

/** Retrieves the public identifier */
    public string getPub()
    {
        return pub;
    }
}
