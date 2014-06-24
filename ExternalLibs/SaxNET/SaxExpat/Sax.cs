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
using GUID = System.Runtime.InteropServices.GuidAttribute;

namespace Kds.Xml.Sax
{
  /**<summary>Represents the three SAX error levels.</summary> */
  public enum ErrorLevel: byte
  {
    Warning,
    Error,
    Fatal
  }

  /**<summary>Represents standalone attribute values in XML declaration.</summary> */
  public enum XmlDocStandalone: byte
  {
    Undefined,
    Yes,
    No
  }

  /**<summary>Identifies localized string constants.</summary> */
  public enum RsId
  {
    IllegalWhenNotParsing,
    IllegalWhenParsing,
    InvalidInputSource,
    InvalidInputStream,
    InvalidInputReader,
    UnknownEntity,
    CannotResolveEntity,
    NoEntityResolver,
    MissingElementName,
    MissingAttributeName,
    MissingExternalId
  }

  /**<summary>Defines constants for the <see cref="Kds.Xml.Sax"/> namespace.</summary> */
  public class Constants
  {
    private static ResourceManager rm;

    private Constants() {}

    /// <summary>Character constant.</summary>
    public const char
      Blank = ' ',
      LeftAng = '<',
      RightAng = '>',
      LeftPar = '(',
      RightPar = ')';

    /// <summary>String constant.</summary>
    public const string
      XmlNmToken = "NMTOKEN",
      XmlNotation = "NOTATION",
      XmlEnumeration = "ENUMERATION",
      XmlCData = "CDATA",
      XmlUndeclared = "UNDECLARED",
      XmlDtdName = "[dtd]";

    /// <summary>Base name for SAX extension features in the <see cref="Kds.Xml.Sax"/> namespace.</summary>
    public const string KdsFeatures = "http://kd-soft.net/sax/features/";

    /// <summary>SAX extension feature name.</summary>
    public const string
      ParameterEntitiesFeature = KdsFeatures + "parameter-entities",
      SkipInternalFeature = KdsFeatures + "skip-internal-entities",
      ParseUnlessStandaloneFeature = KdsFeatures + "parse-unless-standalone",
      StandaloneErrorFeature = KdsFeatures + "standalone-error";

    /// <summary>Base name for SAX extension properties in the <see cref="Kds.Xml.Sax"/> namespace.</summary>
    public const string KdsProperties = "http://kd-soft.net/sax/properties/";

    /// <summary>SAX extension property name.</summary>
    public const string
      DefaultHandlerProperty = KdsProperties + "default-handler";

    /// <summary>Interface GUID constants for COM interop.</summary>
    public const string
      IidIDefaultHandler = "8BAA5518-779C-411B-B91E-8564A5F5B339";

    /// <summary>Returns localized string constants.</summary>
    public static string GetString(RsId id)
    {
      string name = Enum.GetName(typeof(RsId), id);
      return rm.GetString(name);
    }

    static Constants()
    {
      rm = new ResourceManager(
          "Org.System.Xml.Sax.SaxExpat.KdsSax",
          typeof(Constants).Assembly
          //Assembly.GetExecutingAssembly()
          );
    }
  }

  /**<summary>Interface for reporting data that are not passed to any other handler.</summary> */
  [GUID(Constants.IidIDefaultHandler)]
  public interface IDefaultHandler
  {
    /// <summary>Callback for unhandled data.</summary>
    void UnhandledData(char[] data, int start, int length);
  }

}
