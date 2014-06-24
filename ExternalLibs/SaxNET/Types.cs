// NO WARRANTY!  This code is in the Public Domain.
// Written by Karl Waclawek (karl@waclawek.net).

using System;
// using System.Collections.Generic; -- leave out for now, not needed (yet)

namespace Org.System.Xml
{
  // public delegate string XmlStringIntern(string str); -- leave out for now, not needed (yet)

  /**<summary>Represents namespace aware name in XML.</summary> */
  public struct QName
  {
    private string localName;
    private string nsUri;

    /// <summary>Returns empty <see cref="QName"/> constant.</summary>
    /// <remarks>This implies that both, <see cref="LocalName"/> and
    /// <see cref="NsUri"/>, are the empty string.</remarks>
    public static QName Empty
    {
      get {
        QName result;
        result.localName = String.Empty;
        result.nsUri = String.Empty;
        return result;
      }
    }

    /// <overloads>
    /// <summary>Checks the <c>localname</c> and <c>nsUri</c> arguments and creates
    /// a new <see cref="QName"/> instance if they are well-formed.</summary>
    /// </overloads>
    /// <exception cref="ArgumentException">Thrown when the arguments are not well-formed.</exception>
    /// <remarks>Relative URIs will be rejected as namespace names since such use
    /// has been deprecated.</remarks>
    /// <param name="localName">Local part of qualified XML name.</param>
    /// <param name="nsUri">Namespace URI of qualified XML name.</param>
    /// <returns>Well-formed <see cref="QName"/> instance.</returns>
    public static QName Checked(string localName, string nsUri)
    {
      XmlChars.CheckNcName(localName);
      Uri uri = new Uri(nsUri);
      return new QName(localName, nsUri);
    }

    /// <param name="localName">Local part of qualified XML name.</param>
    /// <returns>Well-formed <see cref="QName"/> instance with empty namespace.</returns>
    public static QName Checked(string localName)
    {
      XmlChars.CheckNcName(localName);
      return new QName(localName);
    }

    /// <overloads>
    /// <summary>Initializes a new instance of the <c>QName</c> struct.</summary>
    /// <remarks><c>null</c> arguments will be converted to empty strings.
    /// Other than that there is no checking for well-formed arguments.</remarks>
    /// </overloads>
    /// <param name="localName">Local part of qualified XML name.</param>
    /// <param name="nsUri">Namespace URI of qualified XML name.</param>
    public QName(string localName, string nsUri)
    {
      this.localName = (localName == null) ? String.Empty : localName;
      this.nsUri = (nsUri == null) ? String.Empty : nsUri;
    }

    /// <param name="localName">Local part of qualified XML name.</param>
    public QName(string localName)
    {
      this.localName = (localName == null) ? String.Empty : localName;
      this.nsUri = String.Empty;
    }

    /// <summary>Indicates whether the <see cref="QName"/> is empty.</summary>
    public bool IsEmpty
    {
      get { return localName == String.Empty && nsUri == String.Empty; }
    }

    /// <summary>Local part of qualified XML name.</summary>
    public string LocalName
    {
      get { return localName; }
    }

    /// <summary>Namespace URI of qualified XML name.</summary>
    public string NsUri
    {
      get { return nsUri; }
    }

    /// <summary>Calculates hash value from local part and namespace URI.</summary>
    public override int GetHashCode()
    {
      return localName.GetHashCode() ^ nsUri.GetHashCode();
    }

    /// <summary>Overridden <c>Equals</c> method.</summary>
    public override bool Equals(object obj)
    {
      return obj is QName && this == (QName)obj;
    }

    /// <summary>Implementation of <c>==</c> operator.</summary>
    public static bool operator ==(QName x, QName y)
    {
      return x.localName == y.localName && x.nsUri == y.nsUri;
    }

    /// <summary>Implementation of <c>!=</c> operator.</summary>
    public static bool operator !=(QName x, QName y)
    {
      return !(x == y);
    }

    /// <summary>Returns string value of <see cref="QName"/>.</summary>
    /// <returns>String value in the format <b>{namespace}localname</b>,
    /// or just <b>localname</b> if there is no namespace defined.</returns>
    public override string ToString()
    {
      if (nsUri != String.Empty)
        return '{' + nsUri + '}' + localName;
      else
        return localName;
    }
  }
}
