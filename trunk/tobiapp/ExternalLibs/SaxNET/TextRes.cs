/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Resources;
using System.Reflection;

namespace Kds.Text
{
  /**<summary>Identifies localized string constants.</summary> */
  public enum RsId
  {
    ArrayOutOfBounds,
    IllegalLength,
    SizeTooSmall
  }

  /**<summary>Enables access to localized resources.</summary> */
  public class Resources
  {
    private static ResourceManager rm;

    private Resources() {}

    /// <summary>Returns localized string constants.</summary>
    public static string GetString(RsId id)
    {
      string name = Enum.GetName(typeof(RsId), id);
      return rm.GetString(name);
    }

    static Resources()
    {
        rm = new ResourceManager("Org.System.Xml.Sax.KdsText",
          typeof(Resources).Assembly
        //Assembly.GetExecutingAssembly()
          );
    }
  }
}
