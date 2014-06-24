// NO WARRANTY!  This code is in the Public Domain.
// Written by Karl Waclawek (karl@waclawek.net).

using System;
using System.Resources;
using System.Reflection;

namespace Org.System.Xml.Sax
{
  /**<summary>Identifies localized string constants.</summary> */
  public enum RsId
  {
    // for Org.System.Xml.Sax namespace
    CannotChangeExceptionId,
    FeatureNotSupported,
    FeatureReadNotSupported,
    FeatureWriteNotSupported,
    FeatureWhenParsing,
    FeatureNotRecognized,
    PropertyNotSupported,
    PropertyNotRecognized,
    // for Org.System.Xml.Sax.Helpers namespace
    CapacityTooSmall,
    AttIndexOutOfBounds,
    AttributeNotFound,
    AttributeNotFoundNS,
    NonEmptyStringRequired,
    NoFilterParent,
    NoXmlReaderInAssembly,
    NoDefaultXmlReader
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
      rm = new ResourceManager("Org.System.Xml.Sax.Sax.Sax",
          typeof(Resources).Assembly
          //Assembly.GetExecutingAssembly()
          );
    }
  }
}
