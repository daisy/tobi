// NO WARRANTY!  This code is in the Public Domain.
// Based on Java source at www.saxproject.org.
// Ported and written by Karl Waclawek (karl@waclawek.net).

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Configuration;
using GUID = System.Runtime.InteropServices.GuidAttribute;

/* Documented in SAX.ndoc, as namespaces cannot have XML documentation comments. */
namespace Org.System.Xml.Sax
{
  /**<summary>Defines constants for the <see cref="Org.System.Xml.Sax"/> namespace.</summary> */
  public class Constants
  {
    private Constants() { }

    /// <summary>Base name for standard SAX features.</summary>
    public const string Features = "http://xml.org/sax/features/";

    /// <summary>Standard SAX feature name.
    /// See <see href="http://www.saxproject.org/apidoc/org/xml/sax/package-summary.html#package_description">
    /// Feature table</see> on www.saxproject.org.</summary>
    public const string
      NamespacesFeature = Features + "namespaces",
      NamespacePrefixesFeature = Features + "namespace-prefixes",
      ValidationFeature = Features + "validation",
      ExternalGeneralFeature = Features + "external-general-entities",
      ExternalParameterFeature = Features + "external-parameter-entities",
      ResolveDtdUrisFeature = Features + "resolve-dtd-uris",
      LexicalParameterFeature = Features + "lexical-handler/parameter-entities",
      XmlNsUrisFeature = Features + "xmlns-uris",
      Xml11Feature = Features + "xml-1.1",
      UnicodeNormCheckFeature = Features + "unicode-normalization-checking";

    /// <summary>True if parser provides implementations for the
    /// <see cref="ILocator.XmlVersion"/> and <see cref="ILocator.Encoding"/> properties,
    /// false otherwise.</summary>
    /// <remarks>Read-only. Replaces the UseLocator2Feature feature in SAX for .NET 1.0.</remarks>
    /// <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Locator2.html">
    /// Locator2 on www.saxproject.org</seealso>
    public const string XmlDeclFeature = Features + "xml-declaration";

    /// <summary>True if parser calls back on <see cref="IEntityResolver.GetExternalSubset"/>
    /// to process an application-provided DTD, false if no such call-back is made.</summary>
    /// <remarks>Read-only. Replaces the UseEntityResolver2Feature feature in SAX for .NET 1.0.</remarks>
    /// <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/ext/EntityResolver2.html">
    /// EntityResolver2 on www.saxproject.org</seealso>
    public const string UseExternalSubsetFeature = Features + "use-external-subset";

    /// <summary>True if suspending and resuming the parse process is supported,
    /// false otherwise.</summary>
    /// <remarks>Read-only. If false, then calling <see cref="IXmlReader.Suspend"/>,
    /// <see cref="IXmlReader.Resume"/> or <see cref="IXmlReader.Abort"/>
    /// will throw a <see cref="NotSupportedException"/>.</remarks>
    public const string ReaderControlFeature = Features + "reader-control";

    /// <summary>Base name for standard SAX properties.</summary>
    public const string Properties = "http://xml.org/sax/properties/";

    /// <summary>Standard SAX property name.
    /// See <see href="http://www.saxproject.org/apidoc/org/xml/sax/package-summary.html#package_description">
    /// Property table</see> on www.saxproject.org.</summary>
    public const string
      XmlStringProperty = Properties + "xml-string",
      DomNodeProperty = Properties + "dom-node";

    /// <summary>Interface GUID constant for COM interop.</summary>
    public const string
      IidIAttributes = "B88C9D0F-1AFD-412A-B9EF-2313AB251CF1",
      IidIContentHandler = "755DA957-795C-4888-B3D2-6FD4EA36B8EA",
      IidIDtdHandler = "8CE723C8-EBC9-4FE9-97B5-CC70D4927476",
      IidIDeclHandler = "75534E2D-BB12-4379-8298-486C00141B6A",
      IidILexicalHandler = "1EE9A4D2-BA2E-47C8-81CB-1C81704D9F20",
      IidIEntityResolver = "FC8D2E95-DC19-4A3A-BADC-B8EEEA2E4D8C",
      IidIErrorHandler = "F5157F20-8B53-489F-B41B-A67A41E3AF3F",
      IidILocator = "9D0C1A95-C9EA-40E4-B4A2-5E83FCC62CCF",
      IidIXmlReader = "CA11495E-6F47-4C06-8A69-BEF7342BB81D",
      IidIXmlFilter = "96CFADBE-A3C2-4940-A833-7E3784F176C0",
      IidIGenericProperty = "C8615F83-5A93-4cf1-B1C2-B6458ECE7E19",
      IidIProperty = "2937C64B-FFC6-47BD-9D2C-B99F514CB0B3",
      IidIBooleanProperty = "41AADAC0-20BF-482E-B350-6F0F41CFE114",
      IidIIntegerProperty = "0F0A7E92-D039-40AA-A634-340631AE90A1",
      IidIInterfaceProperty = "A3D3718E-9933-4928-81B8-5C4D20D220A2",
      IidIStringProperty = "AE0E551F-57BB-4BAD-B9AB-68FD6F7DBF6E";


    /// <summary>Character constant.</summary>
    public const char
      XmlColon = ':';
  }

  /**<summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html">
   * Attributes</see> and <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Attributes2.html">
   * Attributes2</see> on www.saxproject.org.</summary>
   * <remarks>
   * This interface combines features from the <c>Attributes</c> and <c>Attributes2</c>
   * interfaces specified in the original Java SAX API.
   * <list type="bullet">
   *   <item>Some methods behave differently from Java - check method documentation.</item>
   *   <item>Outside of the <see cref="IContentHandler.StartElement"/> call-back, an
   *     <c>IAttributes</c> instance is not required to return meaningful information.
   *     It will depend on the implementation if the instance is a unique object retaining all
   *     information, if accessing it throws a <see cref="SaxException"/>, or if it returns
   *     random information.</item>
   *   <item>Attribute names are treated the same as element names with respect to namespaces.
   *     See <see cref="IContentHandler.StartElement"/>.</item>
   *   <item>Namespace declarations (xmlns attributes) will be reported as attributes if the
   *     <c>namespace-prefixes</c> feature is true, even if namespace processing is turned on.</item>
   *   <item>The functionality of the <c>Attributes2.isDeclared</c> method in the Java API
   *     has been translated into a new return value <c>"UNDECLARED"</c> for the
   *     <see cref="IAttributes.GetType"/> method.</item>
   * </list></remarks>
   */
  [GUID(Constants.IidIAttributes)]
  public interface IAttributes
  {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getLength()">
    /// Attributes.getLength</see> on www.saxproject.org.</summary>
    int Length { get; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getURI(int)">
    /// Attributes.getURI</see> on www.saxproject.org.</summary>
    /// <remarks>
    /// <para>returns the empty string if there is no namespace for the attribute name.</para>
    /// Differences to Java:
    /// <list type="bullet">
    ///   <item>Throws standard .NET exceptions.</item>
    /// </list></remarks>
    /// <exception cref="IndexOutOfRangeException">Thrown when index out of range.</exception>
    string GetUri(int index);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getLocalName(int)">
    /// Attributes.getLocalName</see> on www.saxproject.org.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Throws standard .NET exceptions.</item>
    ///   <item>Always returns a non-empty string value.</item>
    /// </list></remarks>
    /// <exception cref="IndexOutOfRangeException">Thrown when index out of range.</exception>
    string GetLocalName(int index);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getQName(int)">
    /// Attributes.getQName</see> on www.saxproject.org.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Throws standard .NET exceptions.</item>
    ///   <item>Always returns a non-empty string value.</item>
    /// </list></remarks>
    /// <exception cref="IndexOutOfRangeException">Thrown when index out of range.</exception>
    string GetQName(int index);
    /// <overloads>
    ///   <summary>Returns type of specified attribute.</summary>
    ///   <remarks>Differences to Java:
    ///   <list type="bullet">
    ///     <item>Throws standard .NET exceptions.</item>
    ///     <item>Allows for two additional return values, <c>"ENUMERATION"</c> when the attribute
    ///       is an enumerated type, and <c>"UNDECLARED"</c> when no declaration for the attribute
    ///       has been read. The latter replaces
    ///       <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Attributes2.html#isDeclared(int)">
    ///       Attributes2.isDeclared</see> from the Java API.</item>
    ///   </list></remarks>
    /// </overloads>
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getType(int)">
    /// Attributes.getType</see> on www.saxproject.org.</summary>
    /// <exception cref="IndexOutOfRangeException">Thrown when index out of range.</exception>
    string GetType(int index);
    /// <overloads>
    ///   <summary>Returns value of specified attribute.</summary>
    ///   <remarks>Differences to Java:
    ///   <list type="bullet">
    ///     <item>Throws standard .NET exceptions.</item>
    ///   </list></remarks>
    /// </overloads>
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getValue(int)">
    /// Attributes.getValue</see> on www.saxproject.org.</summary>
    /// <exception cref="IndexOutOfRangeException">Thrown when index out of range.</exception>
    string GetValue(int index);
    /// <overloads>Returns index of specified attribute, or -1 if the attribute does not exist.</overloads>
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getIndex(java.lang.String, java.lang.String)">
    /// Attributes.getIndex</see> on www.saxproject.org.</summary>
    /// <remarks>If there is no namespace, pass the empty string for the <c>uri</c> argument.</remarks>
    int GetIndex(string uri, string localName);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getIndex(java.lang.String)">
    /// Attributes.getIndex</see> on www.saxproject.org.</summary>
    int GetIndex(string qName);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getType(java.lang.String, java.lang.String)">
    /// Attributes.getType</see> on www.saxproject.org.</summary>
    /// <remarks>If there is no namespace, pass the empty string for the <c>uri</c> argument.</remarks>
    /// <exception cref="ArgumentException">Thrown when no matching attribute can be found.</exception>
    string GetType(string uri, string localName);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getType(java.lang.String)">
    /// Attributes.getType</see> on www.saxproject.org.</summary>
    /// <exception cref="ArgumentException">Thrown when no matching attribute can be found.</exception>
    string GetType(string qName);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getValue(java.lang.String, java.lang.String)">
    /// Attributes.getValue</see> on www.saxproject.org.</summary>
    /// <remarks>If there is no namespace, pass the empty string for the <c>uri</c> argument.</remarks>
    /// <exception cref="ArgumentException">Thrown when no matching attribute can be found.</exception>
    string GetValue(string uri, string localName);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Attributes.html#getValue(java.lang.String)">
    /// Attributes.getValue</see> on www.saxproject.org.</summary>
    /// <exception cref="ArgumentException">Thrown when no matching attribute can be found.</exception>
    string GetValue(string qName);
    /// <overloads>
    ///   <summary>Indicates if attribute was specified, that is, not defaulted from DTD.</summary>
    ///   <remarks>Differences to Java:
    ///   <list type="bullet">
    ///     <item>Throws standard .NET exceptions.</item>
    ///     <item>For a parser that does not read the DTD this will always return true.</item>
    ///   </list></remarks>
    /// </overloads>
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Attributes2.html#isSpecified(int)">
    /// Attributes2.isSpecified</see> on www.saxproject.org.</summary>
    /// <exception cref="IndexOutOfRangeException">Thrown when index out of range.</exception>
    bool IsSpecified(int index);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Attributes2.html#isSpecified(java.lang.String)">
    /// Attributes2.isSpecified</see> on www.saxproject.org.</summary>
    /// <exception cref="ArgumentException">Thrown when no matching attribute can be found.</exception>
    bool IsSpecified(string qName);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Attributes2.html#isSpecified(java.lang.String, java.lang.String)">
    /// Attributes2.isSpecified</see> on www.saxproject.org.</summary>
    /// <remarks>If there is no namespace, pass the empty string for the <c>uri</c> argument.</remarks>
    /// <exception cref="ArgumentException">Thrown when no matching attribute can be found.</exception>
    bool IsSpecified(string uri, string localName);
  }

  /**<summary>Reports the logical content of an XML document.
   * See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html">
   * ContentHandler</see> on www.saxproject.org.</summary>
   * <remarks>The order of events in this interface must mirror the order of information
   * in a well-formed XML document, but it is not necessary that the call-backs are generated
   * from parsing an actual XML document - see <see cref="Org.System.Xml.Sax"/>.</remarks>
   */
  [GUID(Constants.IidIContentHandler)]
  public interface IContentHandler
  {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#setDocumentLocator(org.xml.sax.Locator)">
    /// ContentHandler.setDocumentLocator</see> on www.saxproject.org.</summary>
    void SetDocumentLocator(ILocator locator);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#startDocument()">
    /// ContentHandler.startDocument</see> on www.saxproject.org.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Stricter about when to call:
    ///     For a stream of SAX events that represent an XML document, the SAX event producer
    ///     must call <c>StartDocument</c> exactly once, <b>before</b> any part of the input, on
    ///     which the SAX events are based, is processed.</item>
    /// </list></remarks>
    void StartDocument();
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#endDocument()">
    /// ContentHandler.endDocument</see> on www.saxproject.org.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Stricter about when to call: <c>EndDocument</c> <b>must</b> be called by the
    ///     SAX event producer exactly once as the last event in a SAX event stream if it was
    ///     initiated  by a <see cref="IContentHandler.StartDocument"/> call, regardless of any
    ///     exceptions or errors encountered, even after <see cref="IXmlReader.Abort"/> was called.
    ///     Depending on the call communication mechanism, however, this is no guarantee that
    ///     the SAX event consumer will also receive the call.</item>
    /// </list></remarks>
    void EndDocument();
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#startPrefixMapping(java.lang.String, java.lang.String)">
    /// ContentHandler.startPrefixMapping</see> on www.saxproject.org.</summary>
    /// <remarks>The prefix argument for the default namespace is the empty string, and not <c>null</c>.</remarks>
    void StartPrefixMapping(string prefix, string uri);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#endPrefixMapping(java.lang.String)">
    /// ContentHandler.endPrefixMapping</see> on www.saxproject.org.</summary>
    /// <remarks>The prefix argument for the default namespace is the empty string, and not <c>null</c>.</remarks>
    void EndPrefixMapping(string prefix);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#startElement(java.lang.String, java.lang.String, java.lang.String, org.xml.sax.Attributes)">
    /// ContentHandler.startElement</see> on www.saxproject.org.</summary>
    /// <remarks><para>If there is no URI, then the <c>uri</c> argument will be the empty string
    /// and not <c>null</c>. This can be the case when namespace processing is turned off, or when
    /// the name is not in any namespace.</para>
    /// Differences to Java:
    /// <list type="bullet">
    ///   <item><c>qName</c> and <c>localName</c> will never be <c>null</c> or the empty string.</item>
    ///   <item><c>qName</c> and <c>localName</c> will only be different, if namespace processing
    ///     is turned on - the <c>namespaces</c> feature is true - and the name is in a namespace
    ///     and has a namespace prefix. Otherwise they will be identical.</item>
    ///   <item>xmlns attributes will be reported as attributes if the <c>namespace-prefixes</c>
    ///     feature is true, even if namespace processing is turned on.</item>
    /// </list></remarks>
    void StartElement(string uri, string localName, string qName, IAttributes atts);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#endElement(java.lang.String, java.lang.String, java.lang.String)">
    /// ContentHandler.endElement</see> on www.saxproject.org.</summary>
    void EndElement(string uri, string localName, string qName);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#characters(char[], int, int)">
    /// ContentHandler.characters</see> on www.saxproject.org.</summary>
    void Characters(char[] ch, int start, int length);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#ignorableWhitespace(char[], int, int)">
    /// ContentHandler.ignorableWhitespace</see> on www.saxproject.org.</summary>
    void IgnorableWhitespace(char[] ch, int start, int length);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#processingInstruction(java.lang.String, java.lang.String)">
    /// ContentHandler.processingInstruction</see> on www.saxproject.org.</summary>
    void ProcessingInstruction(string target, string data);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ContentHandler.html#skippedEntity(java.lang.String)">
    /// ContentHandler.skippedEntity</see> on www.saxproject.org.</summary>
    void SkippedEntity(string name);
  }

  /**<summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/DTDHandler.html">
   * DTDHandler</see> on www.saxproject.org.</summary>
   */
  [GUID(Constants.IidIDtdHandler)]
  public interface IDtdHandler
  {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/DTDHandler.html#notationDecl(java.lang.String, java.lang.String, java.lang.String)">
    /// DTDHandler.notationDecl</see> on www.saxproject.org.</summary>
    void NotationDecl(string name, string publicId, string systemId);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/DTDHandler.html#unparsedEntityDecl(java.lang.String, java.lang.String, java.lang.String, java.lang.String)">
    /// DTDHandler.unparsedEntityDecl</see> on www.saxproject.org.</summary>
    void UnparsedEntityDecl(string name, string publicId, string systemId, string notationName);
  }

  /**<summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/DeclHandler.html">
   * DeclHandler</see> on www.saxproject.org.</summary>
   * <remarks>This interface is optional, a SAX parser need not implement it.</remarks>
   */
  [GUID(Constants.IidIDeclHandler)]
  public interface IDeclHandler {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/DeclHandler.html#elementDecl(java.lang.String, java.lang.String)">
    /// DeclHandler.elementDecl</see> on www.saxproject.org.</summary>
    void ElementDecl(string name, string model);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/DeclHandler.html#attributeDecl(java.lang.String, java.lang.String, java.lang.String, java.lang.String, java.lang.String)">
    /// DeclHandler.attributeDecl</see> on www.saxproject.org.</summary>
    void AttributeDecl(string eName, string aName, string aType, string mode, string aValue);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/DeclHandler.html#internalEntityDecl(java.lang.String, java.lang.String)">
    /// DeclHandler.internalEntityDecl</see> on www.saxproject.org.</summary>
    void InternalEntityDecl(string name, string value);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/DeclHandler.html#externalEntityDecl(java.lang.String, java.lang.String, java.lang.String)">
    /// DeclHandler.externalEntityDecl</see> on www.saxproject.org.</summary>
    void ExternalEntityDecl(string name, string publicId, string systemId);
  }

  /**<summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html">
   * LexicalHandler</see> on www.saxproject.org.</summary>
   * <remarks>This interface is optional, a SAX parser need not implement it.</remarks>
   */
  [GUID(Constants.IidILexicalHandler)]
  public interface ILexicalHandler {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html#startDTD(java.lang.String, java.lang.String, java.lang.String)">
    /// LexicalHandler.startDTD</see> on www.saxproject.org.</summary>
    void StartDtd(string name, string publicId, string systemId);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html#endDTD()">
    /// LexicalHandler.endDTD</see> on www.saxproject.org.</summary>
    void EndDtd();
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html#startEntity(java.lang.String)">
    /// LexicalHandler.startEntity</see> on www.saxproject.org.</summary>
    void StartEntity(string name);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html#endEntity(java.lang.String)">
    /// LexicalHandler.endEntity</see> on www.saxproject.org.</summary>
    void EndEntity(string name);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html#startCDATA()">
    /// LexicalHandler.startCDATA</see> on www.saxproject.org.</summary>
    void StartCData();
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html#endCDATA()">
    /// LexicalHandler.endCDATA</see> on www.saxproject.org.</summary>
    void EndCData();
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/LexicalHandler.html#comment(char[], int, int)">
    /// LexicalHandler.comment</see> on www.saxproject.org.</summary>
    void Comment(char[] ch, int start, int length);
  }

  /**<summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/EntityResolver.html">
   * EntityResolver</see> and <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/EntityResolver2.html">
   * EntityResolver2</see> on www.saxproject.org.</summary>
   * <remarks>This interface combines features from the <c>EntityResolver</c> and <c>EntityResolver2</c>
   * interfaces specified in the original Java SAX API.</remarks>
   */
  [GUID(Constants.IidIEntityResolver)]
  public interface IEntityResolver {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/EntityResolver2.html#getExternalSubset(java.lang.String, java.lang.String)">
    /// EntityResolver2.getExternalSubset</see> on www.saxproject.org.</summary>
    /// <remarks>Optional method.</remarks>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    InputSource GetExternalSubset(string name, string baseUri);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/EntityResolver2.html#resolveEntity(java.lang.String, java.lang.String, java.lang.String, java.lang.String)">
    /// EntityResolver2.resolveEntity</see> on www.saxproject.org.</summary>
    /// <remarks>This replaces <c>IEntityResolver.ResolveEntity(string publicId, string systemId)</c> from
    /// SAX for .NET 1.0. For compatibility the <c>name</c> argument may be <c>null</c>.
    /// The <c>systemId</c> argument must be an absolute URI only if the <c>baseUri</c>
    /// argument is <c>null</c>.</remarks>
    InputSource ResolveEntity(string name, string publicId, string baseUri, string systemId);
  }

  /**<summary>Similar to ErrorHandler interface in SAX. See
   * <see href="http://www.saxproject.org/apidoc/org/xml/sax/ErrorHandler.html">
   * ErrorHandler</see> on www.saxproject.org.</summary>
   * <remarks><para>It is <b>not</b> prohibited to call back on this interface from any of the
   * call-back handlers, like <see cref="IContentHandler"/> and <see cref="ILexicalHandler"/>.
   * This allows, for example, that a schema validator implemented as <see cref="IXmlFilter"/>
   * can properly report validation errors through the <c>IErrorHandler</c> call-backs.</para>
   * Differences to Java:
   * <list type="bullet">
   *   <item>The arguments passed to the call-backs are not <see cref="SaxParseException"/>
   *     objects, but instances of (a sub-class of) <see cref="ParseError"/>. The reason for
   *     this is that using exception objects would require the creation of a new instance
   *     for each call-back, as an exception's standard properties can only be set in the
   *     constructor. This is quite inefficient when error call-backs happen frequently.</item>
   *   <item>To be re-usable, the <see cref="ParseError"/> arguments are only valid for the
   *     duration of the call-back.</item>
   * </list></remarks>
   */
  [GUID(Constants.IidIErrorHandler)]
  public interface IErrorHandler
  {
    /// <summary>Call-back for warnings. Parser can continue. See
    /// <see href="http://www.saxproject.org/apidoc/org/xml/sax/ErrorHandler.html#warning(org.xml.sax.SAXParseException)">
    /// ErrorHandler.warning</see> on www.saxproject.org.</summary>
    void Warning(ParseError error);
    /// <summary>Call-back for non-fatal errors. Parser can continue. See
    /// <see href="http://www.saxproject.org/apidoc/org/xml/sax/ErrorHandler.html#error(org.xml.sax.SAXParseException)">
    /// ErrorHandler.error</see> on www.saxproject.org.</summary>
    void Error(ParseError error);
    /// <summary>Call-back for fatal errors, like well-formedness violations.
    /// Parser cannot continue. See
    /// <see href="http://www.saxproject.org/apidoc/org/xml/sax/ErrorHandler.html#fatalError(org.xml.sax.SAXParseException)">
    /// ErrorHandler.fatalError</see> on www.saxproject.org.</summary>
    void FatalError(ParseError error);
  }

  /// <summary>Describes the kind of entity that is being parsed.</summary>
  /// <remarks>For document entities, the *declared* rather than the effective
  /// value of the standalone flag is reported.</remarks>
  public enum ParsedEntityType
  {
    /// <summary>Type of entity not known (at this time).</summary>
    Unknown,
    /// <summary>Document entity without specified value for the standalone flag.</summary>
    Document,
    /// <summary>Document entity with standalone="no".</summary>
    NotStandalone,
    /// <summary>Document entity with standalone="yes".</summary>
    Standalone,
    /// <summary>External general entity.</summary>
    General,
    /// <summary>External parameter entity.</summary>
    Parameter
  }

  /**<summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Locator.html">
   * Locator</see> and <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Locator2.html">
   * Locator2</see> on www.saxproject.org.</summary>
   * <remarks>
   * <list type="bullet">
   *   <item>This interface combines features from the <c>Locator</c> and <c>Locator2</c>
   *     interfaces specified in the original Java SAX API.</item>
   *   <item>Any call on this interface before <see cref="IContentHandler.StartDocument"/>
   *     or after <see cref="IContentHandler.EndDocument"/> (that is, when not parsing)
   *     will throw a <see cref="SaxException"/>.</item>
   *   <item>Because the <see cref="Org.System.Xml.Sax">definition of "parsing"</see> in SAX
   *     is not restricted to textual document parsing, certain properties of <c>ILocator</c>
   *     may not be applicable in all contexts.</item>
   * </list></remarks>
   */
  [GUID(Constants.IidILocator)]
  public interface ILocator
  {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Locator.html#getPublicId()">
    /// Locator.getPublicId</see> on www.saxproject.org.</summary>
    string PublicId { get; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Locator.html#getSystemId()">
    /// Locator.getSystemId</see> on www.saxproject.org.</summary>
    string SystemId { get; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Locator.html#getLineNumber()">
    /// Locator.getLineNumber</see> on www.saxproject.org.</summary>
    long LineNumber { get; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/Locator.html#getColumnNumber()">
    /// Locator.getColumnNumber</see> on www.saxproject.org.</summary>
    long ColumnNumber { get; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Locator2.html#getXMLVersion()">
    /// Locator2.getXMLVersion</see> on www.saxproject.org.</summary>
    /// <remarks>Optional method.</remarks>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    string XmlVersion { get; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Locator2.html#getEncoding()">
    /// Locator2.getEncoding</see> on www.saxproject.org.</summary>
    /// <remarks>Optional method.</remarks>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    string Encoding { get; }
    /// <summary>Indicates if the entity currently being parsed is the document entity -
    /// standalone or not - or an external parsed entity.</summary>
    /// <remarks>Optional property.</remarks>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    ParsedEntityType EntityType { get; }
  }

  /**<summary>Describes the parsing status of an <see cref="IXmlReader"/> instance.</summary> */
  public enum XmlReaderStatus
  {
    /// <summary>Parser is initialized - ready to parse (again).</summary>
    Ready,
    /// <summary>Parsing is under way.</summary>
    /// <remarks>Should only be detectable from within a call-back handler.</remarks>
    Parsing,
    /// <summary>Parsing of input source is suspended, but not completed yet.</summary>
    /// <remarks>Parsing can be resumed again - or aborted.</remarks>
    Suspended
  }

  /**<summary><c>IXmlReader</c> is the interface that an XML parser confomring to the SAX
   * API must implement. This interface allows an application to set and query features
   * and properties in the parser, to register event handlers for document processing,
   * and to initiate, suspend or resume a document parse.</summary>
   * <remarks>There are some differences to the Java specification.
   *   <list type="bullet">
   *     <item>Extension properties are retrieved or set using the <see cref="IProperty&lt;T>"/>
   *       interface, which avoids repeated lookup by name.</item>
   *     <item>Explicit properties exist for registering <see cref="IDeclHandler"/> and
   *       <see cref="ILexicalHandler"/> instances.</item>
   *     <item>The additional methods <see cref="IXmlReader.Suspend"/>,
   *       <see cref="IXmlReader.Resume"/> and <see cref="IXmlReader.Abort"/> allow for
   *       suspending, resuming or aborting the parse process. If an implementation supports
   *       these methods - as reported by the <c>reader-control</c> feature - it is only
   *       required to support their functionality for content related call-backs, but not
   *       for DTD related events. This capability allows one to turn a SAX parser into a
   *       Pull parser when a suitable implementation of <see cref="IContentHandler"/>
   *       is used that calls <see cref="IXmlReader.Suspend"/> whenever an event
   *       of interest occurs.</item>
   *   </list>
   * </remarks>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html">
   * XMLReader on www.saxproject.org</seealso>
   */
  [GUID(Constants.IidIXmlReader)]
  public interface IXmlReader
  {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#getFeature(java.lang.String)">
    /// XMLReader.getFeature</see> on www.saxproject.org.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Throws standard .NET exceptions.</item>
    /// </list></remarks>
    /// <exception cref="ArgumentException">Thrown when the feature name is not recognized.</exception>
    /// <exception cref="NotSupportedException">Thrown when the feature cannot be read.</exception>
    bool GetFeature(string name);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#setFeature(java.lang.String, boolean)">
    /// XMLReader.setFeature</see> on www.saxproject.org.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Throws standard .NET exceptions.</item>
    /// </list></remarks>
    /// <exception cref="ArgumentException">Thrown when the feature name is not recognized
    /// (<see cref="ArgumentException.ParamName"/> = <c>"name"</c>) or when the feature value
    /// is not accepted (<see cref="ArgumentException.ParamName"/> = <c>"value"</c>).</exception>
    /// <exception cref="NotSupportedException">Thrown when the feature cannot be set.</exception>
    void SetFeature(string name, bool value);
    /// <summary>Returns an <see cref="IProperty&lt;T>"/> interface reference for the property
    /// identified by <c>name</c>.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Throws standard .NET exceptions.</item>
    ///   <item>The actual property value is accessed through <see cref="IProperty&lt;T>.Value"/>.</item>
    /// </list></remarks>
    /// <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#getProperty(java.lang.String)">
    /// XMLReader.getProperty on www.saxproject.org</seealso>
    /// <exception cref="ArgumentException">Thrown when the property name is not recognized.</exception>
    IProperty<T> GetProperty<T>(string name);
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#setContentHandler(org.xml.sax.ContentHandler)">
    /// XMLReader.setContentHandler</see> on www.saxproject.org.</summary>
    IContentHandler ContentHandler { get; set; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#setDTDHandler(org.xml.sax.DTDHandler)">
    /// XMLReader.setDTDHandler</see> on www.saxproject.org.</summary>
    IDtdHandler DtdHandler { get; set; }
    /// <summary>Gets or sets the event handler registered for lexical information.</summary>
    /// <remarks>Optional property.</remarks>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    ILexicalHandler LexicalHandler { get; set; }
    /// <summary>Gets or sets the event handler registered for DTD declarations.</summary>
    /// <remarks>Optional property.</remarks>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    IDeclHandler DeclHandler { get; set; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#setEntityResolver(org.xml.sax.EntityResolver)">
    /// XMLReader.setEntityResolver</see> on www.saxproject.org.</summary>
    IEntityResolver EntityResolver { get; set; }
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#setErrorHandler(org.xml.sax.ErrorHandler)">
    /// XMLReader.setErrorHandler</see> on www.saxproject.org.</summary>
    IErrorHandler ErrorHandler { get; set; }
    /// <summary>Parses the XML document represented by the input source. See
    /// <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#parse(org.xml.sax.InputSource)">
    /// XMLReader.parse</see> on www.saxproject.org.</summary>
    /// <remarks>Differences to Java:
    /// <list type="bullet">
    ///   <item>Will not close the input source's stream or text reader at the end of parsing.</item>
    /// </list></remarks>
    void Parse(InputSource input);
    /// <summary>Parses the XML document represented by the system identifier. See
    /// <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#parse(java.lang.String)">
    /// XMLReader.parse</see> on www.saxproject.org.</summary>
    /// <remarks>This will <b>not</b> generate a call-back on <see cref="IEntityResolver.ResolveEntity"/>.</remarks>
    void Parse(string systemId);
    /// <summary>Causes parsing top stop in a resumable way.</summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Optional method.</item>
    ///   <item>It is only legal to call <c>Suspend</c> when <see cref="IXmlReader.Status"/>
    ///     has the value <c>Parsing</c>, and this should only be done from  within a handler
    ///     call-back.</item>
    ///   <item>On return the <see cref="IXmlReader.Status"/> property will have the value
    ///     <c>Suspended</c>.</item>
    ///   <item>An implementation may decide to suspend parsing not immediately, but rather
    ///     "as soon as possible". This can for instance mean that, after calling <c>Suspend</c>
    ///     from the <see cref="IContentHandler.StartElement"/> call-back of an empty element tag,
    ///     the <see cref="IContentHandler.EndElement"/> call-back may still follow.</item>
    /// </list></remarks>
    /// <exception cref="SaxException">Thrown when called outside of call-back handler.</exception>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    void Suspend();
    /// <summary>Causes parsing to stop with a fatal error.</summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Optional method.</item>
    ///   <item>It is legal to call <c>Abort</c> from within a handler call-back, that is,
    ///     when <see cref="IXmlReader.Status"/> has the value <c>Parsing</c>, or from anywhere
    ///     when parsing is suspended.</item>
    ///   <item>On return from <see cref="IXmlReader.Resume"/> or <see cref="IXmlReader.Parse"/>
    ///     the <see cref="IXmlReader.Status"/> property will have the value <c>Ready</c>.</item>
    ///   <item>An implementation may decide to abort parsing not immediately, but rather
    ///     "as soon as possible". This can for instance mean that, after calling <c>Abort</c>
    ///     from the <see cref="IContentHandler.StartElement"/> call-back of an empty element tag,
    ///     the <see cref="IContentHandler.EndElement"/> call-back may still follow.</item>
    /// </list></remarks>
    /// <exception cref="SaxException">Thrown when called illegally.</exception>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    void Abort();
    /// <summary>Resumes parsing as if <see cref="IXmlReader.Parse"/> continued with the
    ///   same input source it was initially called with.</summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Optional method.</item>
    ///   <item>It is only legal to call <c>Resume</c> when parsing is suspended, that is,
    ///     when <see cref="IXmlReader.Status"/> has the value <c>Suspended</c>.</item>
    ///   <item>The <see cref="IXmlReader.Status"/> property will have the value <c>Parsing</c>
    ///     immediately afterwards (in the next handler call-back), but on return from
    ///     <c>Resume</c> its value will either be <c>Ready</c> - when parsing is done -
    ///     or <c>Suspended</c> - when parsing was suspended again.</item>
    /// </list></remarks>
    /// <exception cref="SaxException">Thrown when called illegally.</exception>
    /// <exception cref="NotSupportedException">Thrown when not implemented.</exception>
    void Resume();
    /// <summary>Returns which of three states the parser is in: actively processing a document,
    /// suspended, or ready to start with another document.</summary>
    XmlReaderStatus Status { get; }
  }

  /**<summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLFilter.html">
   * XMLFilter</see> on www.saxproject.org.</summary>
   */
  [GUID(Constants.IidIXmlFilter)]
  public interface IXmlFilter: IXmlReader
  {
    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/XMLFilter.html#setParent(org.xml.sax.XMLReader)">
    /// XMLFilter.setParent</see> on www.saxproject.org.</summary>
    IXmlReader Parent { get; set; }
  }

  /**<summary>Base interface for <see cref="IXmlReader"/> property access.</summary>
   * <remarks>Does not exist in the Java SAX API. Using such an interface to access
   * a property value has the advantage that the overhead of lookup by name, and
   * casting to the correct type occurs only once, on initial retrieval.</remarks>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#getProperty(java.lang.String)">
   * XMLReader.getProperty on www.saxproject.org</seealso>
   */
  [GUID(Constants.IidIGenericProperty)]
  public interface IProperty<T>
  {
    /// <summary>Name of property - one of the property constants.</summary>
    string Name { get; }
    /// <summary>Value accessor.</summary>
    T Value { get; set; }
  }

  /**<summary>Interface for object properties.</summary>
   * <remarks>Retained for backwards compatibility.</remarks>
   */
  [GUID(Constants.IidIProperty), Obsolete]
  public interface IProperty: IProperty<object> { }

  /**<summary>Interface for boolean properties.</summary>
   * <remarks>Retained for backwards compatibility.</remarks>
   */
  [GUID(Constants.IidIBooleanProperty), Obsolete]
  public interface IBooleanProperty: IProperty<bool> { }

  /**<summary>Interface for integer properties.</summary>
   * <remarks>Retained for backwards compatibility.</remarks>
   */
  [GUID(Constants.IidIIntegerProperty), Obsolete]
  public interface IIntegerProperty: IProperty<int> {}

  /**<summary>Interface for string properties.</summary>
   * <remarks>Retained for backwards compatibility.</remarks>
   */
  [GUID(Constants.IidIStringProperty), Obsolete]
  public interface IStringProperty: IProperty<string> { }

  /**<summary>Abstract base class for all errors passed to any of the
   * <see cref="IErrorHandler"/> call-backs.</summary>
   * <remarks>Because the <see cref="Org.System.Xml.Sax">definition of "parsing"</see>
   * in SAX is not restricted to textual document parsing, certain properties of
   * <c>ParseError</c> may not be applicable in all contexts.</remarks>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/SAXParseException.html">
   * SAXParseException on www.saxproject.org</seealso>
   */
  public abstract class ParseError
  {
    /// <summary>Error message. Must not be <c>null</c>.</summary>
    public virtual string Message
    {
      get { return String.Empty; }
    }

    /// <summary>Identifies which well-formedness or validation constraint
    /// was violated. This may also refer to a custom error id message.
    /// Is <c>null</c> if such information is not available or applicable.</summary>
    /// <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/SAXParseException.html#getExceptionId()">
    /// SAXParseException.getExceptionId on www.saxproject.org</seealso>
    public virtual string ErrorId
    {
      get { return null; }
    }

    /// <summary>The public identifier of the entity where the error
    /// occurred, or <c>null</c> if none is available or applicable.</summary>
    public virtual string PublicId
    {
      get { return null; }
    }

    /// <summary>The system identifier of the entity where the error
    /// occurred, or <c>null</c> if none is available or applicable.</summary>
    public virtual string SystemId
    {
      get { return null; }
    }

    /// <summary>The line number of the end of the text where the error
    /// occurred, or <c>-1</c> if none is available or applicable.</summary>
    /// <remarks>This number is 1-based.</remarks>
    public virtual long LineNumber
    {
      get { return -1; }
    }

    /// <summary>The column number of the end of the text where the error
    /// occurred, or <c>-1</c> if none is available or applicable.</summary>
    /// <remarks>This number is 1-based.</remarks>
    public virtual long ColumnNumber
    {
      get { return -1; }
    }

    /// <summary>The underlying exception, or <c>null</c> if none exists.</summary>
    public virtual Exception BaseException
    {
      get { return null; }
    }

    /// <summary>Throws new <see cref="SaxParseException"/> based on this instance.</summary>
    public void Throw()
    {
      throw new SaxParseException(this);
    }
  }

  /**<summary>Base class for all SAX exceptions.</summary>
   * <remarks>The .NET <see cref="ApplicationException"/> class already implements
   * all of the (here missing) methods and properties, including the various
   * constructors, desribed in the corresponding Java docs for
   * <see href="http://www.saxproject.org/apidoc/org/xml/sax/SAXException.html">
   * SAXException</see>.</remarks>
   */
  public class SaxException: ApplicationException
  {
    public SaxException() { }

    public SaxException(string message): base(message) { }

    public SaxException(string message, Exception e): base(message, e) { }

    /* The .NET Exception class already implements all of the below,
     * including the various constructors desribed in the Java docs.
    public string GetMessage();
    public Exception GetException();
    public override string ToString();
    */
  }

  /**<summary>Base class for SAX parse exceptions.</summary>
   * <remarks>Inspect the optional Error property for details. In some
   * cases it might be possible to down-cast this property for accessing
   * additional - parser implementation specific - information. See
   * <see cref="IErrorHandler"/> and <see cref="ParseError"/>.</remarks>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/SAXParseException.html">
   * SAXParseException on www.saxproject.org</seealso>
   */
  public class SaxParseException: SaxException
  {
    private ParseError error;

    public SaxParseException() { }

    public SaxParseException(string message): base(message) { }

    public SaxParseException(string message, Exception e): base(message, e) { }

    public SaxParseException(ParseError error):
      base(error.Message, error.BaseException)
    {
      this.error = error;
    }

    /// <summary>The underlying ParseError instance, if any.</summary>
    public ParseError Error
    {
      get { return error; }
    }
  }

  /**<summary>Base class for input sources.</summary>
   * <remarks>Derive from this class to give specific access to input data.</remarks>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/InputSource.html">
   * InputSource on www.saxproject.org</seealso>
   */
  public class InputSource
  {
    private string publicId, encoding;
    private Uri systemId;

    public InputSource() { }

    public InputSource(string systemId)
    {
      SystemId = systemId;
    }

    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/InputSource.html#setEncoding(java.lang.String)">
    /// InputSource.get/setEncoding</see> on www.saxproject.org.</summary>
    public string Encoding
    {
      get { return encoding; }
      set { encoding = value; }
    }

    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/InputSource.html#setPublicId(java.lang.String)">
    /// InputSource.get/setPublicId</see> on www.saxproject.org.</summary>
    public string PublicId
    {
      get { return publicId; }
      set { publicId = value; }
    }

    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/InputSource.html#setSystemId(java.lang.String)">
    /// InputSource.get/setSystemId</see> on www.saxproject.org.</summary>
    public string SystemId
    {
      get {
        if (systemId == null)
          return null;
        else
          return systemId.AbsoluteUri;
      }
      set {
        if (value == null)
          systemId = null;
        else
          // if there is no exception then we have an absolute URI
          systemId = new Uri(value);
      }
    }
  }

  /**<summary>Input source with a generic Source property.</summary>
   * <remarks>The source could be a <c>Stream</c> or <c>TextReader</c>. It will not
   * be disposed after use - that responsibility is left to the application.</remarks>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/InputSource.html">
   * InputSource on www.saxproject.org</seealso>
   */
  public class InputSource<S>: InputSource
  {
    private S source;

    public InputSource() { }

    public InputSource(S source)
    {
      this.source = source;
    }

    public InputSource(S source, string systemId): base(systemId)
    {
      this.source = source;
    }

    /// <summary>Gives access to the input data.</summary>
    /// <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/InputSource.html#setByteStream(java.io.InputStream)">
    /// InputSource.get/setByteStream on www.saxproject.org</seealso>
    public S Source
    {
      get { return source; }
      set
      {
        if (value == null)
          throw new ArgumentNullException("Source");
        this.source = value;
      }
    }
  }
}

