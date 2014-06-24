// NO WARRANTY!  This code is in the Public Domain.
// Based on Java source at www.saxproject.org.
// Ported and written by Karl Waclawek (karl@waclawek.net).

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.IO;

namespace Org.System.Xml.Sax.Helpers
{
  /**<summary>Default implementation for <see cref="ParseError"/>.</summary>
   * <remarks>For some parsers a different implementation may be preferable.</remarks>
   */
  public class ParseErrorImpl: ParseError
  {
    private Exception baseException;
    private string errorId;
    private string message;
    private string publicId;
    private string systemId;
    private long lineNumber;
    private long columnNumber;

    /// <summary>Set error id.</summary>
    /// <remarks>Makes re-use of a <see cref="ParseError"/> instance possible,
    /// together with <see cref="Init"/> and <see cref="SetBaseException"/>.</remarks>
    protected void SetErrorId(string id)
    {
      errorId = id;
    }

    /// <summary>Set base exception.</summary>
    /// <remarks>Makes re-use of a <see cref="ParseError"/> instance possible,
    /// together with <see cref="Init"/> and <see cref="SetErrorId"/>.</remarks>
    protected void SetBaseException(Exception e)
    {
      baseException = e;
    }

    /// <summary>Initialize instance data.</summary>
    /// <remarks>Makes re-use of a <see cref="ParseError"/> instance possible,
    /// together with <see cref="SetErrorId"/> and <see cref="SetBaseException"/>.</remarks>
    protected void Init(
      string message,
      string publicId,
      string systemId,
      long lineNumber,
      long columnNumber)
    {
      this.message = message;
      this.publicId = publicId;
      this.systemId = systemId;
      this.lineNumber = lineNumber;
      this.columnNumber = columnNumber;
    }

    public ParseErrorImpl(string message)
    {
      Init(message, null, null, -1, -1);
    }

    public ParseErrorImpl(string message, Exception e)
    {
      Init(message, null, null, -1, -1);
      baseException = e;
    }

    public ParseErrorImpl(string message, ILocator locator, string id)
    {
      if (locator != null)
        Init(message, locator.PublicId, locator.SystemId,
             locator.LineNumber, locator.ColumnNumber);
      else
        Init(message, null, null, -1, -1);
      errorId = id;
    }

    public ParseErrorImpl(string message, ILocator locator, Exception e)
    {
      if (locator != null)
        Init(message, locator.PublicId, locator.SystemId,
             locator.LineNumber, locator.ColumnNumber);
      else
        Init(message, null, null, -1, -1);
      baseException = e;
    }

    public ParseErrorImpl(string message,
                          string publicId,
                          string systemId,
                          int lineNumber,
                          int columnNumber,
                          string id)
    {
      Init(message, publicId, systemId, lineNumber, columnNumber);
      errorId = id;
    }

    public ParseErrorImpl(string message,
                          string publicId,
                          string systemId,
                          int lineNumber,
                          int columnNumber,
                          Exception e)
    {
      Init(message, publicId, systemId, lineNumber, columnNumber);
      baseException = e;
    }

    public override string Message
    {
      get { return message; }
    }

    public override string ErrorId
    {
      get { return errorId; }
    }

    public override string PublicId
    {
      get { return publicId; }
    }

    public override string SystemId
    {
      get { return systemId; }
    }

    public override long LineNumber
    {
      get { return lineNumber; }
    }

    public override long ColumnNumber
    {
      get { return columnNumber; }
    }

    public override Exception BaseException
    {
      get { return baseException; }
    }
  }

  /**<summary>Call-back delegate, useful when implementing <see cref="IProperty&lt;T>"/>.</summary>
   * <remarks>One can re-use the same <see cref="IProperty&lt;T>"/> implementation class
   * without the need to subclass it for a specific target property. One simply
   * registers an <c>OnPropertyChange</c> delegate with the <see cref="IProperty&lt;T>"/>
   * instance which gets called whenever <see cref="IProperty&lt;T>.Value"/> changes.</remarks>
   */
  public delegate void OnPropertyChange<T>(IProperty<T> property, T newValue);

  /**<summary>Implementaton of <see cref="IProperty&lt;T>"/> interface which calls back through
   * a delegate on every change of the property value.</summary>
   */
  public abstract class PropertyImpl<T>: IProperty<T>
  {
    private T propValue;
    private OnPropertyChange<T> onChange;

    protected PropertyImpl(OnPropertyChange<T> onChange, T defaultValue)
    {
      this.onChange = onChange;
      this.propValue = defaultValue;
    }

    /// <summary>The Name property must be overriden in a derived class.</summary>
    /// <remarks>This allows one to save the space needed for a name field.</remarks>
    public abstract string Name { get; }

    public T Value
    {
      get { return propValue; }
      set
      {
        T newValue = value;
        if (onChange != null)
          onChange(this, newValue);
        propValue = newValue;
      }
    }
  }

  /**<summary>Default implementation of the <see cref ="IAttributes"/> interface.</summary>
   * <remarks>Differences to Java implementation: the <c>GetLocalName()</c>,
   * <c>GetQName()</c>, <c>GetType()</c>, <c>GetURI()</c> and <c>GetValue()</c>
   * methods throw an exception when no attribute matching the arguments is found.
   * In Java these methods return <c>null</c>, which is inconsistent since the same GetXXX()
   * methods in Java's Attributes2 and the SetXXX() methods in Java's AttributesImpl
   * class do throw exceptions in the same situation.</remarks>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/helpers/AttributesImpl.html">
   * AttributesImpl on www.saxproject.org</seealso>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Attributes2Impl.html">
   * Attributes2Impl on www.saxproject.org</seealso>
   */
  public class AttributesImpl: IAttributes, ICloneable
  {
    /// <summary>Holds all the fields for an attribute.</summary>
    protected struct Attribute
    {
      private string uri;
      private string qName;
      private string aType;
      private string aValue;

      public string Uri
      {
        get { return uri; }
        set {
          if (value == null)
            throw new ArgumentNullException("Uri");
          uri = value;
        }
      }

      public string QName
      {
        get { return qName; }
        set {
          if (value == null || value == String.Empty)
            throw new ArgumentException(Resources.GetString(RsId.NonEmptyStringRequired), "QName");
          qName = value;
        }
      }

      public string AType
      {
        get { return aType; }
        set {
          if (value == null || value == String.Empty)
            throw new ArgumentException(Resources.GetString(RsId.NonEmptyStringRequired), "AType");
          aType = value;
        }
      }

      public string AValue
      {
        get { return aValue; }
        set {
          if (value == null)
            throw new ArgumentNullException("AValue");
          aValue = value;
        }
      }

      public int PrefixLen;     // includes colon
      public bool IsSpecified;
    }

    private Attribute[] atts;
    private int attCount;
    private StringBuilder strBuilder;

    /// <summary>All attributes are stored in this struct array.</summary>
    protected Attribute[] Atts
    {
      get { return atts; }
    }

    /// <summary>String builder helper object.</summary>
    protected StringBuilder StrBuilder
    {
      get { return strBuilder; }
    }

    public AttributesImpl()
    {
      atts = new Attribute[4];  // default capacity = 4
    }

    public AttributesImpl(int capacity)
    {
      atts = new Attribute[capacity];
    }

    /// <summary>Returns clone of <c>AttributesImpl</c> instance.</summary>
    /// <remarks>Only <see cref="Attribute"/> fields are cloned, not the
    /// other fields like <see cref="Capacity"/>.</remarks>
    public AttributesImpl Clone()
    {
      AttributesImpl result = new AttributesImpl(attCount);
      // requires existing Attributes to be properly initialized
      Array.Copy(atts, 0, result.atts, 0, attCount);
      result.attCount = attCount;
      return result;
    }

    /// <summary>Capacity for holding <see cref="Attribute"/> instances.</summary>
    /// <remarks>Can be initialized to avoid costly re-allocations
    /// when new attributes are added. Its value must not be less
    /// than the value of the <see cref="Length"/> property.</remarks>
    public int Capacity
    {
      get { return atts.Length; }
      set {
        if (value < attCount) {
          string msg = Resources.GetString(RsId.CapacityTooSmall);
          throw new ArgumentException(msg, "Capacity");
        }
        Attribute[] tmpAtts = new Attribute[value];
        // requires existing Attributes to be properly initialized
        atts.CopyTo(tmpAtts, 0);
        atts = tmpAtts;
      }
    }

    /// <summary>Checks if index is in range, throws exception if not.</summary>
    protected void CheckIndex(int index)
    {
      bool isBad = index < 0 || index >= attCount;
      if (isBad) {
        string msg = Resources.GetString(RsId.AttIndexOutOfBounds);
        throw new IndexOutOfRangeException(msg);
      }
    }

    /// <summary>Helper routine for throwing an <see cref="ArgumentException"/>
    /// when an attribute is not found, with message loaded from resources.</summary>
    /// <param name="qName">Qualified name of attribute.</param>
    protected void NotFoundError(string qName)
    {
      string msg = Resources.GetString(RsId.AttributeNotFound);
      throw new ArgumentException(String.Format(msg, qName), "qName");
    }

    /// <summary>Helper routine for throwing an <see cref="ArgumentException"/>
    /// when an attribute is not found, with message loaded from resources.</summary>
    /// <param name="uri">URI of attribute's qualified name.</param>
    /// <param name="localName">Local part of attribute's qualified name.</param>
    protected void NotFoundError(string uri, string localName)
    {
      string msg = Resources.GetString(RsId.AttributeNotFoundNS);
      throw new ArgumentException(String.Format(msg, uri, localName), "uri, localName");
    }

    /// <summary>Helper routine for quickly setting the fields of an attribute.</summary>
    protected void InternalSetAttribute(
      ref Attribute att,
      string uri,
      string localName,
      string qName,
      string aType,
      string aValue,
      bool isSpecified)
    {
      att.Uri = uri;
      if (qName == null) {
        att.QName = localName;
        att.PrefixLen = 0;
      }
      else {
        att.QName = qName;
        int colPos = qName.IndexOf(Sax.Constants.XmlColon);
        if (colPos < 0)
          att.PrefixLen = 0;
        else
          att.PrefixLen = (colPos + 1);
      }
      att.AType = aType;
      att.AValue = aValue;
      att.IsSpecified = isSpecified;
    }

    /* ICloneable */

    /// <summary>See <see cref="Clone()"/>.</summary>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /* IAttributes */

    public int Length
    {
      get { return attCount; }
    }

    public string GetUri(int index)
    {
      CheckIndex(index);
      return atts[index].Uri;
    }

    public string GetLocalName(int index)
    {
      CheckIndex(index);
      return atts[index].QName.Substring(atts[index].PrefixLen);
    }

    public string GetQName(int index)
    {
      CheckIndex(index);
      return atts[index].QName;
    }

    public string GetType(int index)
    {
      CheckIndex(index);
      return atts[index].AType;
    }

    public string GetType(string uri, string localName)
    {
      int index = GetIndex(uri, localName);
      if (index < 0)
        NotFoundError(uri, localName);
      return atts[index].AType;
    }

    public string GetType(string qName)
    {
      int index = GetIndex(qName);
      if (index < 0)
        NotFoundError(qName);
      return atts[index].AType;
    }

    public string GetValue(int index)
    {
      CheckIndex(index);
      return atts[index].AValue;
    }

    public string GetValue(string uri, string localName)
    {
      int index = GetIndex(uri, localName);
      if (index < 0)
        NotFoundError(uri, localName);
      return atts[index].AValue;
    }

    public string GetValue(string qName)
    {
      int index = GetIndex(qName);
      if (index < 0)
        NotFoundError(qName);
      return atts[index].AValue;
    }

    public int GetIndex(string uri, string localName)
    {
      int result = attCount - 1;
      while (result >= 0) {
        if (String.Equals(uri, atts[result].Uri)) {
          string qName = atts[result].QName;
          bool equal = !(localName == null ^ qName == null);
          if (equal && localName != null)
            equal = String.CompareOrdinal(
              localName, 0, qName, atts[result].PrefixLen, Int32.MaxValue) == 0;
          if (equal)
            break;
        }
        result--;
      }
      return result;
    }

    public int GetIndex(string qName)
    {
      int result = attCount - 1;
      while (result >= 0) {
        if (qName != null && qName.Equals(atts[result].QName))
          break;
        result--;
      }
      return result;
    }

    public bool IsSpecified(int index)
    {
      CheckIndex(index);
      return atts[index].IsSpecified;
    }

    public bool IsSpecified(string qName)
    {
      int index = GetIndex(qName);
      if (index < 0)
        NotFoundError(qName);
      return atts[index].IsSpecified;
    }

    public bool IsSpecified(string uri, string localName)
    {
      int index = GetIndex(uri, localName);
      if (index < 0)
        NotFoundError(uri, localName);
      return atts[index].IsSpecified;
    }

    /* modifier methods */

    /// <summary>Add an attribute by specifying all its properties.</summary>
    /// <remarks>If there is no namespace, pass the empty string for
    /// the <c>uri</c> argument, and not <c>null</c>.</remarks>
    /// <returns>Index of new attribute.</returns>
    public int AddAttribute(
      string uri,
      string localName,
      string qName,
      string aType,
      string aValue,
      bool isSpecified)
    {
      if (attCount >= Capacity) {
        int newSize = attCount + 4 + (attCount >> 1);
        newSize = (newSize >> 2) << 2;  // align to 4 byte boundary
        Capacity = newSize;
      }
      InternalSetAttribute(
        ref atts[attCount],
        uri,
        localName,
        qName,
        aType,
        aValue,
        isSpecified);
      return attCount++;
    }

    /// <summary>Add an attribute taken from an existing set of attributes.</summary>
    /// <returns>Index of added attribute.</returns>
    public virtual int AddAttribute(IAttributes atts, int index)
    {
      if (atts == null)
        throw new ArgumentNullException("atts");
      return AddAttribute(
        atts.GetUri(index),
        atts.GetLocalName(index),
        atts.GetQName(index),
        atts.GetType(index),
        atts.GetValue(index),
        atts.IsSpecified(index));
    }

    /// <summary>Remove all attributes, but don't shrink capacity.</summary>
    public virtual void Clear()
    {
      attCount = 0;
    }

    /// <summary>Remove attribute at index.</summary>
    public void RemoveAttribute(int index)
    {
      CheckIndex(index);
      int lastIndx = --attCount;
      for (; index < lastIndx; index++)
        atts[index] = atts[index + 1];
    }

    /// <summary>Set attribute properties at index.</summary>
    public void SetAttribute(
      int index,
      string uri,
      string localName,
      string qName,
      string aType,
      string aValue,
      bool isSpecified)
    {
      CheckIndex(index);
      InternalSetAttribute(
        ref atts[index],
        uri,
        localName,
        qName,
        aType,
        aValue,
        isSpecified);
    }

    /// <summary>Copy a whole set of attributes.</summary>
    public virtual void SetAttributes(IAttributes atts)
    {
      if (atts == null)
        throw new ArgumentNullException("atts");
      Clear();
      int attLen = atts.Length;
      if (Capacity < attLen)
        Capacity = attLen;
      for (int attIndx = 0; attIndx < attLen; attIndx++) {
        InternalSetAttribute(
          ref this.atts[attIndx],
          atts.GetUri(attIndx),
          atts.GetLocalName(attIndx),
          atts.GetQName(attIndx),
          atts.GetType(attIndx),
          atts.GetValue(attIndx),
          atts.IsSpecified(attIndx));
      }
    }

    /// <summary>Set local name of attribute at index.</summary>
    public void SetLocalName(int index, string localName)
    {
      CheckIndex(index);
      if (localName == null || localName == String.Empty)
        throw new ArgumentException(Resources.GetString(RsId.NonEmptyStringRequired), "localName");
      int prefixLen = atts[index].PrefixLen;
      if (strBuilder == null)
        strBuilder = new StringBuilder(atts[index].QName, 0, prefixLen,
          localName.Length + prefixLen);
      else {
        strBuilder.Length = 0;
        strBuilder.Append(atts[index].QName, 0, prefixLen);
      }
      strBuilder.Append(localName);
      atts[index].QName = strBuilder.ToString();
    }

    /// <summary>Set qualified name of attribute at index.</summary>
    public void SetQName(int index, string qName)
    {
      CheckIndex(index);
      atts[index].QName = qName;
    }

    /// <summary>Set type of attribute at index.</summary>
    public void SetType(int index, string aType)
    {
      CheckIndex(index);
      atts[index].AType = aType;
    }

    /// <summary>Set namespace URI of attribute at index.</summary>
    /// <remarks>For removing the namespace, pass the empty string
    /// for the <c>uri</c> argument, and not <c>null</c>.</remarks>
    public void SetURI(int index, string uri)
    {
      CheckIndex(index);
      atts[index].Uri = uri;
    }

    /// <summary>Set value of attribute at index.</summary>
    public void SetValue(int index, string aValue)
    {
      CheckIndex(index);
      atts[index].AValue = aValue;
    }

    /// <summary>Set if attribute at index is specified (not defaulted).</summary>
    public void SetIsSpecified(int index, bool isSpecified)
    {
      CheckIndex(index);
      atts[index].IsSpecified = isSpecified;
    }
  }

  /**<summary>Default implementation of the <see cref="ILocator"/> interface.</summary>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/helpers/LocatorImpl.html">
   * LocatorImpl on www.saxproject.org</seealso>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/ext/Locator2Impl.html">
   * Locator2Impl on www.saxproject.org</seealso>
   */
  public class LocatorImpl: ILocator
  {
    private string publicId;
    private string systemId;
    private long lineNumber;
    private long columnNumber;
    private string xmlVersion;
    private string encoding;
    private ParsedEntityType entityType;

    public LocatorImpl() {}

    public LocatorImpl(ILocator locator)
    {
      publicId = locator.PublicId;
      systemId = locator.SystemId;
      lineNumber = locator.LineNumber;
      columnNumber = locator.ColumnNumber;
      xmlVersion = locator.XmlVersion;
      encoding = locator.Encoding;
      entityType = locator.EntityType;
    }

    public string PublicId
    {
      get { return publicId; }
      set { publicId = value; }
    }

    public string SystemId
    {
      get { return systemId; }
      set { systemId = value; }
    }

    public long LineNumber
    {
      get { return lineNumber; }
      set { lineNumber = value; }
    }

    public long ColumnNumber
    {
      get { return columnNumber; }
      set { columnNumber = value; }
    }

    public string XmlVersion
    {
      get { return xmlVersion; }
      set { xmlVersion = value; }
    }

    public string Encoding
    {
      get { return encoding; }
      set { encoding = value; }
    }

    public ParsedEntityType EntityType
    {
      get { return entityType; }
      set { entityType = value; }
    }
  }

  /**<summary>No-op implementation of SAX interfaces, to be derived from.</summary>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/helpers/DefaultHandler.html">
   * DefaultHandler on www.saxproject.org</seealso>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/ext/DefaultHandler2.html">
   * DefaultHandler2 on www.saxproject.org</seealso>
   */
  public class DefaultHandler:
    IContentHandler,
    IDtdHandler,
    ILexicalHandler,
    IDeclHandler,
    IEntityResolver,
    IErrorHandler
  {
    /* IContentHandler */

    public virtual void SetDocumentLocator(ILocator locator)
    {
      // no op
    }

    public virtual void StartDocument()
    {
      // no op
    }

    public virtual void EndDocument()
    {
      // no op
    }

    public virtual void StartPrefixMapping(string prefix, string uri)
    {
      // no op
    }

    public virtual void EndPrefixMapping(string prefix)
    {
      // no op
    }

    public virtual void StartElement(
      string uri,
      string localName,
      string qName,
      IAttributes atts)
    {
      // no op
    }

    public virtual void EndElement(string uri, string localName, string qName)
    {
      // no op
    }

    public virtual void Characters(char[] ch, int start, int length)
    {
      // no op
    }

    public virtual void IgnorableWhitespace(char[] ch, int start, int length)
    {
      // no op
    }

    public virtual void ProcessingInstruction(string target, string data)
    {
      // no op
    }

    public virtual void SkippedEntity(string name)
    {
      // no op
    }

    /* IDtdHandler */

    public virtual void NotationDecl(string name, string publicId, string systemId)
    {
      // no op
    }

    public virtual void UnparsedEntityDecl(
      string name,
      string publicId,
      string systemId,
      string notationName)
    {
      // no op
    }

    /* ILexicalHandler */

    public virtual void StartDtd(string name, string publicId, string systemId)
    {
      // no op
    }

    public virtual void EndDtd()
    {
      // no op
    }

    public virtual void StartEntity(string name)
    {
      // no op
    }

    public virtual void EndEntity(string name)
    {
      // no op
    }

    public virtual void StartCData()
    {
      // no op
    }

    public virtual void EndCData()
    {
      // no op
    }

    public virtual void Comment(char[] ch, int start, int length)
    {
      // no op
    }

    /* IDeclHandler */

    public virtual void ElementDecl(string name, string model)
    {
      // no op
    }

    public virtual void AttributeDecl(
      string eName, string aName, string aType, string mode, string aValue)
    {
      // no op
    }

    public virtual void InternalEntityDecl(string name, string value)
    {
      // no op
    }

    public virtual void ExternalEntityDecl(
      string name, string publicId, string systemId)
    {
      // no op
    }

    /* IEntityResolver */

    public virtual InputSource GetExternalSubset(string name, string baseURI)
    {
      return null;
    }

    public virtual InputSource ResolveEntity(
      string name, string publicId, string baseURI, string systemId)
    {
      return null;
    }

    /* IErrorHandler */

    public virtual void Warning(ParseError error)
    {
      // no op
    }

    public virtual void Error(ParseError error)
    {
      // no op
    }

    public virtual void FatalError(ParseError error)
    {
      error.Throw();
    }
  }

  /**<summary>Base class for deriving an XML filter.</summary>
   * <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/helpers/XMLFilterImpl.html">
   * JavaDoc on www.saxproject.org</seealso>
   */
  public class XmlFilterImpl:
    IXmlFilter,
    IContentHandler,
    IDtdHandler,
    ILexicalHandler,
    IDeclHandler,
    IEntityResolver,
    IErrorHandler
  {
    private IXmlReader parent;
    private ILocator locator;
    private IContentHandler contentHandler;
    private IDtdHandler dtdHandler;
    private ILexicalHandler lexicalHandler;
    private IDeclHandler declHandler;
    private IEntityResolver resolver;
    private IErrorHandler errorHandler;

    public XmlFilterImpl() {}

    public XmlFilterImpl(IXmlReader parent)
    {
      this.parent = parent;
    }

    protected void CheckParent()
    {
      if (parent == null)
        throw new SaxException(Resources.GetString(RsId.NoFilterParent));
    }

    protected virtual void SetupParse()
    {
      CheckParent();
      parent.ContentHandler = this;
      parent.DtdHandler = this;
      parent.LexicalHandler = this;
      parent.DeclHandler = this;
      parent.EntityResolver = this;
      parent.ErrorHandler = this;
    }

    /* IXmlReader */

    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/helpers/XMLFilterImpl.html#getFeature(java.lang.String)">
    /// getFeature(java.lang.String)</see> on www.saxproject.org.</summary>
    /// <remarks>Difference to Java: Will throw <see cref="SaxException"/>
    /// if parent is <c>null</c>.</remarks>
    public bool GetFeature(string name)
    {
      CheckParent();
      return parent.GetFeature(name);
    }

    /// <summary>See <see href="http://www.saxproject.org/apidoc/org/xml/sax/helpers/XMLFilterImpl.html#setFeature(java.lang.String, boolean)">
    /// setFeature(java.lang.String, boolean)</see> on www.saxproject.org.</summary>
    /// <remarks>Difference to Java: Will throw <see cref="SaxException"/>
    /// if parent is <c>null</c>.</remarks>
    public void SetFeature(string name, bool value)
    {
      CheckParent();
      parent.SetFeature(name, value);
    }

    /// <summary>Returns an <see cref="IProperty&lt;T>"/> interface for the property
    /// identified by <c>name</c>.</summary>
    /// <remarks>Difference to Java: Will throw <see cref="SaxException"/>
    /// if parent is <c>null</c>.</remarks>
    /// <seealso href="http://www.saxproject.org/apidoc/org/xml/sax/XMLReader.html#getProperty(java.lang.String)">
    /// getProperty(java.lang.String) on www.saxproject.org</seealso>
    public IProperty<T> GetProperty<T>(string name)
    {
      CheckParent();
      return parent.GetProperty<T>(name);
    }

    public IContentHandler ContentHandler
    {
      get { return contentHandler; }
      set { contentHandler = value; }
    }

    public IDtdHandler DtdHandler
    {
      get { return dtdHandler; }
      set { dtdHandler = value; }
    }

    public ILexicalHandler LexicalHandler
    {
      get { return lexicalHandler; }
      set { lexicalHandler = value; }
    }

    public IDeclHandler DeclHandler
    {
      get { return declHandler; }
      set { declHandler = value; }
    }

    public IEntityResolver EntityResolver
    {
      get { return resolver; }
      set { resolver = value; }
    }

    public IErrorHandler ErrorHandler
    {
      get { return errorHandler; }
      set { errorHandler = value; }
    }

    public void Parse(InputSource input)
    {
      SetupParse();
      parent.Parse(input);
    }

    public void Parse(string systemId)
    {
      Parse(new InputSource(systemId));
    }

    public void Suspend()
    {
      CheckParent();
      parent.Suspend();
    }

    public void Abort()
    {
      CheckParent();
      parent.Abort();
    }

    public void Resume()
    {
      CheckParent();
      parent.Resume();
    }

    public XmlReaderStatus Status
    {
      get {
        CheckParent();
        return parent.Status;
      }
    }

    /* IXmlFilter */

    public IXmlReader Parent
    {
      get { return parent; }
      set { parent = value; }
    }

    /* IContentHandler */

    public virtual void SetDocumentLocator(ILocator locator)
    {
      this.locator = locator;
      if (contentHandler != null)
        contentHandler.SetDocumentLocator(locator);
    }

    public virtual void StartDocument()
    {
      if (contentHandler != null)
        contentHandler.StartDocument();
    }

    public virtual void EndDocument()
    {
      if (contentHandler != null)
        contentHandler.EndDocument();
    }

    public virtual void StartPrefixMapping(string prefix, string uri)
    {
      if (contentHandler != null)
        contentHandler.StartPrefixMapping(prefix, uri);
    }

    public virtual void EndPrefixMapping(string prefix)
    {
      if (contentHandler != null)
        contentHandler.EndPrefixMapping(prefix);
    }

    public virtual void StartElement(
      string uri,
      string localName,
      string qName,
      IAttributes atts)
    {
      if (contentHandler != null)
        contentHandler.StartElement(uri, localName, qName, atts);
    }

    public virtual void EndElement(string uri, string localName, string qName)
    {
      if (contentHandler != null)
        contentHandler.EndElement(uri, localName, qName);
    }

    public virtual void Characters(char[] ch, int start, int length)
    {
      if (contentHandler != null)
        contentHandler.Characters(ch, start, length);
    }

    public virtual void IgnorableWhitespace(char[] ch, int start, int length)
    {
      if (contentHandler != null)
        contentHandler.IgnorableWhitespace(ch, start, length);
    }

    public virtual void ProcessingInstruction(string target, string data)
    {
      if (contentHandler != null)
        contentHandler.ProcessingInstruction(target, data);
    }

    public virtual void SkippedEntity(string name)
    {
      if (contentHandler != null)
        contentHandler.SkippedEntity(name);
    }

    /* IDtdHandler */

    public virtual void NotationDecl(string name, string publicId, string systemId)
    {
      if (dtdHandler != null)
        dtdHandler.NotationDecl(name, publicId, systemId);
    }

    public virtual void UnparsedEntityDecl(
      string name,
      string publicId,
      string systemId,
      string notationName)
    {
      if (dtdHandler != null)
        dtdHandler.UnparsedEntityDecl(name, publicId, systemId, notationName);
    }

    /* ILexicalHandler */

    public virtual void StartDtd(string name, string publicId, string systemId)
    {
      if (lexicalHandler != null)
        lexicalHandler.StartDtd(name, publicId, systemId);
    }

    public virtual void EndDtd()
    {
      if (lexicalHandler != null)
        lexicalHandler.EndDtd();
    }

    public virtual void StartEntity(string name)
    {
      if (lexicalHandler != null)
        lexicalHandler.StartEntity(name);
    }

    public virtual void EndEntity(string name)
    {
      if (lexicalHandler != null)
        lexicalHandler.EndEntity(name);
    }

    public virtual void StartCData()
    {
      if (lexicalHandler != null)
        lexicalHandler.StartCData();
    }

    public virtual void EndCData()
    {
      if (lexicalHandler != null)
        lexicalHandler.EndCData();
    }

    public virtual void Comment(char[] ch, int start, int length)
    {
      if (lexicalHandler != null)
        lexicalHandler.Comment(ch, start, length);
    }

    /* IDeclHandler */

    public virtual void ElementDecl(string name, string model)
    {
      if (declHandler != null)
        declHandler.ElementDecl(name, model);
    }

    public virtual void AttributeDecl(
      string eName,
      string aName,
      string aType,
      string mode,
      string aValue)
    {
      if (declHandler != null)
        declHandler.AttributeDecl(eName, aName, aType, mode, aValue);
    }

    public virtual void InternalEntityDecl(string name, string value)
    {
      if (declHandler != null)
        declHandler.InternalEntityDecl(name, value);
    }

    public virtual void ExternalEntityDecl(string name, string publicId, string systemId)
    {
      if (declHandler != null)
        declHandler.ExternalEntityDecl(name, publicId, systemId);
    }

    /* IEntityResolver */

    public virtual InputSource GetExternalSubset(string name, string baseUri)
    {
      if (resolver != null)
        return resolver.GetExternalSubset(name, baseUri);
      else
        return null;
    }

    public virtual InputSource ResolveEntity(string name, string publicId, string baseUri, string systemId)
    {
      if (resolver != null)
        return resolver.ResolveEntity(name, publicId, baseUri, systemId);
      else
        return null;
    }

    /* IErrorHandler */

    public virtual void Warning(ParseError error)
    {
      if (errorHandler != null)
        errorHandler.Warning(error);
    }

    public virtual void Error(ParseError error)
    {
      if (errorHandler != null)
        errorHandler.Error(error);
    }

    public virtual void FatalError(ParseError error)
    {
      if (errorHandler != null)
        errorHandler.FatalError(error);
    }
  }

  /**<summary>Factory class for creating new <see cref="IXmlReader"/> instances.</summary>
   * <remarks>A default implementation of <see cref="IXmlReader"/> can be registered
   * in the system configuration file "machine.config", under the section appSettings.
   * The keys to be registered are "Org.System.Xml.Sax.ReaderClass" and
   * "Org.System.Xml.Sax.ReaderAssembly". The class name must be fully qualified,
   * the assembly name can be a partial name.</remarks>
   */
  public static class SaxReaderFactory
  {
    private static bool InterfaceFilter(Type typeObj, Object criteriaObj)
    {
      Type criteriaType = ((Type)criteriaObj);
      return typeObj.IsSubclassOf(criteriaType) || typeObj == criteriaType;
    }

    /// <summary>Creates an instance of <c>readerType</c> if it has a constructor
    /// matching the runtime types in the <c>args</c> array of parameters.</summary>
    private static IXmlReader CreateInstance(Type readerType, Object[] args)
    {
      IXmlReader result = null;
      BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
      Type[] argTypes;
      if (args != null)
        argTypes = new Type[args.Length];
      else
        argTypes = Type.EmptyTypes;
      for (int indx = 0; indx < argTypes.Length; indx++)
        argTypes[indx] = args[indx].GetType();
      ConstructorInfo cInfo = readerType.GetConstructor(flags, null, argTypes, null);
      if (cInfo != null)
        result = cInfo.Invoke(args) as IXmlReader;
      return result;
    }

    /// <summary>Returns the first class-type it can find in the
    /// <see cref="Assembly"/> argument that implements <see cref="IXmlReader"/>.
    /// Returns <c>null</c> if there is no such class.</summary>
    /// <remarks>This will not find classes that have unbound generic parameters.</remarks>
    private static Type FindReaderClass(Assembly assem)
    {
      if (assem == null)
        throw new ArgumentNullException("assem");
      Type readerType = typeof(IXmlReader);
      TypeFilter filter = new TypeFilter(InterfaceFilter);

      Type[] types = assem.GetExportedTypes();
      for (int indx = 0; indx < types.Length; indx++) {
        Type type = types[indx];
        if (type.IsClass) {
          Type[] intfs = type.FindInterfaces(filter, readerType);
          if (intfs.Length > 0)
            return type;
        }
      }
      return null;
    }

    private const string sax = "Org.System.Xml.Sax";

    /// <summary>Key name for registering the default parser assembly in
    /// the machine.config file.</summary>
    public const string ReaderAssembly = sax + ".ReaderAssembly";

    /// <summary>Key name for registering the default parser class in
    /// the machine.config file.</summary>
    public const string ReaderClass = sax + ".ReaderClass";


    /// <summary>Creates a new instance of <see cref="IXmlReader"/> based on the
    /// assembly and constructor arguments that are passed as parameters.</summary>
    /// <remarks>Searches the <see cref="Assembly"/> argument for classes that
    /// implement <see cref="IXmlReader"/> and have a constructor matching
    /// the types of the parameters in the <c>args</c> array. Creates a new
    /// instance of the first class it finds.</remarks>
    /// <returns><see cref="IXmlReader"/> instance.</returns>
    public static IXmlReader CreateReader(Assembly assem, Object[] args)
    {
      IXmlReader result = null;
      Type readerClass = FindReaderClass(assem);
      if (readerClass != null)
        result = CreateInstance(readerClass, args);
      if (result != null)
        return result;
      else {
        string msg = Resources.GetString(RsId.NoXmlReaderInAssembly);
        throw new SaxException(String.Format(msg, null, assem.GetName().Name));
      }
    }

    /// <summary>Creates a new instance of <see cref="IXmlReader"/> based on the assembly,
    /// class name and constructor arguments that are passed as parameters.</summary>
    /// <remarks>The types of the objects in the <c>args</c> array must match
    /// a constructor signature of the class.</remarks>
    /// <returns><see cref="IXmlReader"/> instance.</returns>
    public static IXmlReader CreateReader(Assembly assem, string className, Object[] args)
    {
      if (assem == null)
        throw new ArgumentNullException("assem");
      IXmlReader result = null;
      Type readerType = assem.GetType(className, false);
      if (readerType != null)
        result = CreateInstance(readerType, args);
      if (result != null)
        return result;
      else {
        string msg = Resources.GetString(RsId.NoXmlReaderInAssembly);
        msg = String.Format(msg, className, assem.GetName().Name);
        throw new SaxException(msg);
      }
    }

    /// <summary>Creates a new instance of <see cref="IXmlReader"/> based on
    /// the constructor arguments that are passed as parameters.</summary>
    /// <remarks>The assembly and class are determined by first checking the
    /// machine configuration file's appSettings section if a default parser is
    /// specified. If that fails, the loaded assemblies are searched for a class
    /// implementing <see cref="IXmlReader"/>. The types of the objects in the
    /// <c>args</c> array must match a constructor signature of the class.</remarks>
    /// <returns><see cref="IXmlReader"/> instance.</returns>
    public static IXmlReader CreateReader(Object[] args)
    {
      try {
        AppSettingsReader confReader = new AppSettingsReader();
        string assemblyName =
                 (string)confReader.GetValue(ReaderAssembly, typeof(string));
        Assembly assem;
        if (File.Exists(assemblyName))
          assem = Assembly.LoadFrom(assemblyName);
        else
          assem = Assembly.Load(assemblyName);
        string className =
                    (string)confReader.GetValue(ReaderClass, typeof(string));
        if (className == null || className == String.Empty)
          return CreateReader(assem, args);
        else
          return CreateReader(assem, className, args);
      }
      catch {
        // ignore exception, we want to check loaded assemblies
      }

      AppDomain domain = AppDomain.CurrentDomain;
      Assembly[] assems = domain.GetAssemblies();
      // ignore the XmlFilterImpl class in this assembly
      Type xmlFilterType = typeof(XmlFilterImpl);
      foreach (Assembly assem in assems) {
        IXmlReader reader = null;
        Type readerType = FindReaderClass(assem);
        if (readerType != null && readerType != xmlFilterType && !readerType.IsSubclassOf(xmlFilterType))
          reader = CreateInstance(readerType, args);
        if (reader != null)
          return reader;
      }
      string msg = Resources.GetString(RsId.NoDefaultXmlReader);
      throw new SaxException(msg);
    }
  }

}

