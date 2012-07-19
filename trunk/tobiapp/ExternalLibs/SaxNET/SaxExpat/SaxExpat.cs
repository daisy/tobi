/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Text;
using System.IO;
using System.Net;
using System.Resources;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Org.System.Xml.Sax;
using Org.System.Xml.Sax.Helpers;
using Kds.Xml.Sax;
using Kds.Xml.Sax.Helpers;
using Kds.Text;

namespace Kds.Xml.Expat
{
  using Helpers = Org.System.Xml.Sax.Helpers;
  using SaxConsts = Org.System.Xml.Sax.Constants;
  using SaxRsId = Org.System.Xml.Sax.RsId;
  using KdsConsts = Kds.Xml.Sax.Constants;
  using KdsRsId = Kds.Xml.Sax.RsId;
  using SaxRes = Org.System.Xml.Sax.Resources;

  /**<summary>Identifies localized string constants.</summary> */
  public enum RsId
  {
    CannotGetInputContext,
    NoPositionForTextReader,
    UnexpectedParserResult,
    UnparsedEntityDeclError,
    ExternalEntityDeclError,
    NotationDeclError,
    InitParseError,
    StreamReaderInputSourceOnly,
    NamespacePrefixesDepends,
    AccessingBaseUri
  }

  /**<summary>Defines constants for the <see cref="Kds.Xml.Expat"/> namespace.</summary> */
  public class Constants
  {
    private static ResourceManager rm;

    private Constants(){}

    /// <summary>Character constant.</summary>
    public const char
      NullChar = (char)0,
      Colon = ':',
      Equal = '=';

    public const string
      ForeignDtdId = "<foreign_dtd>";  // is an invalid URI

    public const int
      UriTimeout = 6000;  // WebRequest timeout

    /// <summary>Returns localized string constants.</summary>
    public static string GetString(RsId id)
    {
      string name = Enum.GetName(typeof(RsId), id);
      return rm.GetString(name);
    }

    static Constants()
    {
        rm = new ResourceManager("Org.System.Xml.Sax.SaxExpat.SaxExpat",
            typeof(Constants).Assembly
            //Assembly.GetExecutingAssembly()
            );
    }
  }

  /**<summary>Expat specific <see cref="IAttributes"/> implementation.</summary> */
  internal unsafe class ExpatAttributes: IAttributes
  {
    private StringTable strTable;
    private AttributeDecls attDecls;
    private string elmName;
    private char** atts;
    private int specAttCount;
    private int totalAttCount;

    private void CheckIndex(int index)
    {
      if ((index < 0) || (index >= totalAttCount)) {
        string msg = SaxRes.GetString(SaxRsId.AttIndexOutOfBounds);
        throw new IndexOutOfRangeException(msg);
      }
    }

    private void NotFoundError(string qName)
    {
      string msg = SaxRes.GetString(SaxRsId.AttributeNotFound);
      throw new ArgumentException(String.Format(msg, qName), "qName");
    }

    private void NotFoundError(string uri, string localName)
    {
      string msg;
      if (uri == null || uri.Length == 0) {
        msg = SaxRes.GetString(SaxRsId.AttributeNotFound);
        msg = String.Format(msg, localName);
      }
      else {
        msg = SaxRes.GetString(SaxRsId.AttributeNotFoundNS);
        msg = String.Format(msg, uri, localName);
      }
      throw new ArgumentException(msg, "{uri}localName");
    }

    public ExpatAttributes(StringTable strTable)
    {
      this.strTable = strTable;
      attDecls = new AttributeDecls();
      elmName = null;
      atts = null;
      specAttCount = 0;
      totalAttCount = 0;
    }

    /* IAttributes */

    public int Length
    {
      get { return totalAttCount; }
    }

    public string GetUri(int index)
    {
      CheckIndex(index);
      string uri = ExpatUtils.ParseNameUri(atts[index << 1], strTable);
      return uri;
    }

    public string GetLocalName(int index)
    {
      CheckIndex(index);
      string locName = ExpatUtils.ParseNameLocal(atts[index << 1], strTable);
      return locName;
    }

    public string GetQName(int index)
    {
      CheckIndex(index);
      string qName = ExpatUtils.ParseNameQual(atts[index << 1], strTable);
      return qName;
    }

    /// <overloads>
    ///   <summary>
    ///     Returns declared type of attribute, or 'UNDECLARED' if no
    ///     declaration was read.
    ///   </summary>
    ///   <remarks>
    ///     Enumerated attributes are reported as type 'ENUMERATION'
    ///     according to the InfoSet spec, instead of 'NMTOKEN' as
    ///     required by the Java SAX2 specs.
    ///   </remarks>
    /// </overloads>
    public string GetType(int index)
    {
      string qName = GetQName(index);
      /* attDecls.GetEntry() is based on matching QNames, i.e. the declarations in
       * the DTD must have the same prefixes and local names as the element and
       * attribute names encountered in the document, since DTDs are not
       * namespace aware. Matching Uri + local name will not work.
       */
      AttDeclEntry entry = attDecls.GetEntry(elmName, qName);
      if (entry.Exists) {
        string attType = attDecls.GetAttType(entry);
        // check for enumerated type or notation - must have left parenthesis
        int parIndx = attType.IndexOf(KdsConsts.LeftPar);
        if (parIndx == -1)  // not found - regular type
          return attType;
        else if (parIndx == 0)  // first position: we have an enumerated type
          // SAX spec says: report as NMTOKEN
          // return Constants.xmlNmToken;
          // XML Infoset spec says: report as ENUMERATION
          return KdsConsts.XmlEnumeration;
        else  // left parenthesis is not first character: we have a notation
          return KdsConsts.XmlNotation;
      }
      else
        return KdsConsts.XmlUndeclared; // default
    }

    public string GetValue(int index)
    {
      CheckIndex(index);
      //kw should we intern the attribute value in strTable?
      char* valPtr = atts[(index << 1) + 1];
      if (valPtr == null)
        return null;
      int valLen = StringUtils.StrLen(valPtr);
      string value = new string(valPtr, 0, valLen);
      return value;
    }

    public int GetIndex(string uri, string localName)
    {
      int indx = totalAttCount - 1;
      string uriNm, locNm;
      while (indx >= 0) {
        ExpatUtils.ParseNameUriLocal(
          atts[indx << 1], strTable, out uriNm, out locNm);
        if (locNm == localName && uriNm == uri)
          break;
        indx--;
      }
      return indx;
    }

    public int GetIndex(string qName)
    {
      int indx = totalAttCount - 1;
      string qNm;
      while (indx >= 0) {
        qNm = ExpatUtils.ParseNameQual(atts[indx << 1], strTable);
        if (qNm == qName)
          break;
        indx--;
      }
      return indx;
    }

    public string GetType(string uri, string localName)
    {
      int indx = GetIndex(uri, localName);
      if (indx < 0)
        NotFoundError(uri, localName);
      return GetType(indx);
    }

    public string GetType(string qName)
    {
      int indx = GetIndex(qName);
      if (indx < 0)
        NotFoundError(qName);
      return GetType(indx);
    }

    public string GetValue(string uri, string localName)
    {
      int indx = GetIndex(uri, localName);
      if (indx < 0)
        NotFoundError(uri, localName);
      return GetValue(indx);
    }

    public string GetValue(string qName)
    {
      int indx = GetIndex(qName);
      if (indx < 0)
        NotFoundError(qName);
      return GetValue(indx);
    }

    public bool IsSpecified(int index)
    {
      CheckIndex(index);
      return (index < specAttCount);
    }

    public bool IsSpecified(string qName)
    {
      int indx = GetIndex(qName);
      if (indx < 0)
        NotFoundError(qName);
      return (indx < specAttCount);
    }

    public bool IsSpecified(string uri, string localName)
    {
      int indx = GetIndex(uri, localName);
      if (indx < 0)
        NotFoundError(uri, localName);
      return (indx < specAttCount);
    }

    /* end of IAttributes */

    public void GetName(int index,
                        out string uri,
                        out string localName,
                        out string qName)
    {
      CheckIndex(index);
      ExpatUtils.ParseNameSax(
              atts[index << 1], strTable, out uri, out localName, out qName);
    }


    // arguments only valid for duration of startElementHandler call-back
    internal void Initialize(string elmName, char** atts, int specAttCount)
    {
      this.elmName = elmName;
      this.atts = atts;
      this.specAttCount = specAttCount >> 1;
      int indx = specAttCount;
      while (atts[indx] != null) indx++;
      this.totalAttCount = this.specAttCount + ((indx - specAttCount) >> 1);
    }

    internal AttributeDecls AttDecls
    {
      get { return attDecls; }
    }
  }

  /**<summary>Expat specific subclass of <see cref="InputSource&lt;Stream>"/>.</summary>
   * <remarks>Used to differentiate between internally created and
   * and application provided input sources.</remarks>
   */
  internal class ExpatStreamSource: InputSource<Stream>
  {
    public ExpatStreamSource(Stream byteStream): base(byteStream) { }
  }

  /**<summary>Struct for internal use in <see cref="ExpatReader"/>. Groups
   * all fields that are used specifically during the parsing process.</summary>
   */
  internal unsafe struct DocParseData
  {
    private ExpatParseError error;
    private ExpatLocator locator;
    private StringBuilder contentBuilder;
    private char[] charBuffer;

    public bool Started;
    public int Standalone;
    public bool HasDoctypeDecl;
    public InputSource ForeignDtdSource;
    public XMLContent* ContentModel;

    public DocParseData(SaxExpatParser parser)
    {
      error = new ExpatParseError();
      locator = new ExpatLocator(parser);
      contentBuilder = new StringBuilder();
      charBuffer = new char[16];
      Started = false;
      Standalone = int.MaxValue;
      HasDoctypeDecl = false;
      ForeignDtdSource = null;
      ContentModel = null;
    }

    public void Reset()
    {
      Started = false;
      Standalone = int.MaxValue;
      ContentBuilder.Length = 0;
      HasDoctypeDecl = false;
      ForeignDtdSource = null;
      ContentModel = null;
    }

    public ExpatParseError Error(string message)
    {
      error.Init(message, Locator);
      return error;
    }

    public ExpatParseError Error(string message, int code)
    {
      error.Init(message, Locator, code);
      return error;
    }

    public ExpatParseError Error(string message, Exception e)
    {
      error.Init(message, Locator, e);
      return error;
    }

    public ExpatLocator Locator
    {
      get { return locator; }
    }

    public StringBuilder ContentBuilder
    {
      get { return contentBuilder; }
    }

    public char[] GetCharBuffer(int len)
    {
      if (charBuffer.Length < len)
        charBuffer = new char[len];
      return charBuffer;
    }
  }

  /**<summary>Expat specific <see cref="ParseError"/> implementation.</summary> */
  public class ExpatParseError: ParseError
  {
    private string message;
    private ExpatLocator locator;
    private int errorCode;
    private Exception baseException;

    private string ErrorUri(int expatCode)
    {
      return null;  // TO DO : map expat codes to exception uris
    }

    public void Init(string message, ExpatLocator locator)
    {
      this.message = message;
      this.locator = locator;
      this.errorCode = 0;
      this.baseException = null;
    }

    public void Init(string message, ExpatLocator locator, int errorCode)
    {
      this.message = message;
      this.locator = locator;
      this.errorCode = errorCode;
      this.baseException = null;
    }

    public void Init(string message, ExpatLocator locator, Exception e)
    {
      this.message = message;
      this.locator = locator;
      this.errorCode = 0;
      this.baseException = e;
    }

    public override string Message
    {
      get { return message; }
    }

    public override string ErrorId
    {
      get { return ErrorUri(errorCode); }
    }

     public override string PublicId
     {
       get { return locator.PublicId; }
     }

    public override string SystemId
    {
       get { return locator.SystemId; }
     }

    public override long LineNumber
    {
       get { return locator.LineNumber; }
     }

    public override long ColumnNumber
    {
       get { return locator.ColumnNumber; }
     }

     public override Exception BaseException
     {
       get { return baseException; }
     }
  }

  /**<summary>Expat specific <see cref="ILocator"/> implementation.</summary>
   * <remarks>The Expat specific <see cref="ParsePosition"/> method
   * is accessible when casting to the actual implementation class.</remarks>
   */
  public class ExpatLocator: ILocator
  {
    internal SaxExpatParser parser;

    internal ExpatLocator(SaxExpatParser parser)
    {
      Debug.Assert(parser != null);
      this.parser = parser;
    }

    /// <summary>Gives access to the parse position in the current input stream.</summary>
    /// <remarks>Since the current input stream changes when an external
    /// entity is parsed, one cannot access the parent document's parse
    /// position (or column/line number) unless one saves it in the
    /// <see cref="ILexicalHandler.StartEntity"/> callback</remarks>
    public unsafe long ParsePosition
    {
      get {
        SaxEntityContext entSC = parser.EntityContext;
        if (entSC.Reader != null) {
          string msg = Constants.GetString(RsId.NoPositionForTextReader);
          throw new SaxException(msg);
        }
        int offset = 0;
        int bufSize = 0;
        if (parser.GetInputContext(ref offset, ref bufSize) == null) {
          string msg = Constants.GetString(RsId.CannotGetInputContext);
          throw new SaxException(msg);
        }
        else  // InStream must be != null
          return entSC.Stream.Position - bufSize + offset;
      }
    }

    /* ILocator */

    public string PublicId
    {
      get { return parser.EntityContext.PublicId; }
    }

    // originates from InputSource.SystemId - is an absolute URI
    public string SystemId
    {
      get { return parser.EntityContext.SystemId; }
    }

    public unsafe long LineNumber
    {
      get { return parser.CurrentLineNumber; }
    }

    public unsafe long ColumnNumber
    {
      get { return parser.CurrentColumnNumber; }
    }

    public string XmlVersion
    {
      get { return parser.EntityContext.Version; }
    }

    public string Encoding
    {
      get { return parser.EntityContext.Encoding; }
    }

    public ParsedEntityType EntityType
    {
      get {
        SaxEntityContext entSC = parser.EntityContext;
        if (entSC.Parent == null) {
          switch (parser.UserData.parseData.Standalone) {
            case -1: return ParsedEntityType.Document;
            case 0: return ParsedEntityType.NotStandalone;
            case 1: return ParsedEntityType.Standalone;
            default: return ParsedEntityType.Unknown;
          }
        }
        else if (entSC.IsParameterEntity)
          return ParsedEntityType.Parameter;
        else
          return ParsedEntityType.General;
      }
    }
  }

  /**<summary>Wrapper class that exposes the (unmanaged) Expat XML parser
   * through the <see cref="IXmlReader"/> interface.</summary>
   * <remarks><list type="bullet">
   * <item>Works only with Expat 1.95.7 and later. To enable the features
   *   of a later version, specific conditional compile symbols must be defined.
   *   Currently the symbols EXPAT_1_95_8_UP and EXPAT_2_0_UP are recognized.
   *   EXPAT_1_95_8_UP enables the suspend/resume functionality introduced with
   *   Expat 1.95.8. EXPAT_2_0_UP enables 64-bit values for line- and column numbers.
   *   This requires that Expat 2.0 is compiled with XML_LARGE_SIZE defined.
   * </item>
   * <item>By casting the usual <see cref="IXmlReader"/> interface reference to
   *   <c>ExpatReader</c> one can gain access to some Expat specific functionality,
   *   and also to the currently active Expat parser instance, so that the
   *   functions exported by the Dll can be called directly (see Expat.cs).</item>
   * <item>SAX Features and Properties: <c>ExpatReader</c> also exposes Features
   *   and Properties directly as public properties for more efficient access.
   *   </item>
   * <item>Input sources: <c>ExpatReader</c> can currently only use <see cref="InputSource"/>,
   *   <see cref="InputSource&lt;Stream>"/> and <see cref="InputSource&lt;TextReader>"/> instances.</item>
   * <item>No Validation: <c>ExpatReader</c> processes and reports all DTD declarations
   *   but does not validate against them. However, attribute values are defaulted
   *   and entity references are replaced.</item>
   * <item><see cref="IContentHandler.IgnorableWhitespace"/> will not be called
   *   because the parser is non-validating. Such white space will be reported as
   *   character data.</item>
   * <item>The <see cref="IXmlReader.Suspend"/> function will not work while an
   *   external parameter entity is being parsed. It will silently fail, that is,
   *   one should check the <see cref="IXmlReader.Status"/> property immediately
   *   afterwards. Other events where it will fail silently are 
   *   <see cref="IContentHandler.EndDocument"/> and <see cref="ILexicalHandler.EndEntity"/>,
   *   even if correctly called when <see cref="IXmlReader.Status"/> =
   *   <see cref="XmlReaderStatus.Parsing"/>.</item>
   * <item>"http://xml.org/sax/features/namespace-prefixes" feature:
   *   When this feature is turned on, xmlns attributes (namespace declarations)
   *   are to be reported like regular attributes, in addition to being reported
   *   through the <see cref="IContentHandler.StartPrefixMapping"/> or
   *   <see cref="IContentHandler.EndPrefixMapping"/> events. However, the underlying
   *   Expat parser never reports xmlns attributes when namespace processing is turned
   *   on, so this feature will have the value <c>false</c> in this case. Conversely,
   *   it will assume the value <c>true</c> when namespace processing is turned off.
   *   This is automatically enforced when setting the namespaces feature.
   *   Trying to set the namespace-prefixes feature to a value conflicting with
   *   the namespaces feature will throw an exception.</item>
   * <item><see cref="IContentHandler.StartDocument"/> and
   *   <see cref="IContentHandler.EndDocument"/> call-backs: 
   *   Once <see cref="IContentHandler.StartDocument"/> was called, 
   *   <see cref="IContentHandler.EndDocument"/> will be called
   *   as well, even after a fatal error, or when an exception was raised,
   *   or when parsing was aborted by calling <see cref="IXmlReader.Abort"/>.
   *   If <see cref="IContentHandler.StartDocument"/> was not called, 
   *   <see cref="IContentHandler.EndDocument"/> will not be called either.
   *   This is independent of whether a content handler was assigned or not, so
   *   it should maybe read: "Once StartDocument() would have been called, ...".
   *   </item>
   * <item>This is how entities are reported:
   *   <list type="table">
   *     <listheader>We use these shorthand terms:</listheader>
   *     <item>ExternalGeneral = external-general-entities feature.</item>
   *     <item>LexicalParameter = lexical-handler/parameter-entities feature.</item>
   *     <item>SkipInternal = skip-internal-entities feature.</item>
   *   </list>
   *   <list type="table">
   *     <listheader>External General Entities:</listheader>
   *     <item>
   *       <term>ExternalGeneral = True.</term>
   *       <description><see cref="ILexicalHandler.StartEntity"/> and
   *       <see cref="ILexicalHandler.EndEntity"/> are called.</description>
   *     </item>
   *     <item>
   *       <term>ExternalGeneral = False.</term>
   *       <description><see cref="IContentHandler.SkippedEntity"/> is called.</description>
   *     </item>
   *   </list>
   *   <list type="table">
   *     <listheader>
   *       External Parameter Entities - LexicalParameter = True:
   *     </listheader>
   *     <item>
   *       <term>ExternalParameter = True.</term>
   *       <description><see cref="ILexicalHandler.StartEntity"/> and
   *       <see cref="ILexicalHandler.EndEntity"/> are called.</description>
   *     </item>
   *     <item>
   *       <term>ExternalParameter = False.</term>
   *       <description><see cref="IContentHandler.SkippedEntity"/> is called.</description>
   *     </item>
   *   </list>
   *   <list type="table">
   *     <listheader>
   *       External Parameter Entities - LexicalParameter = False:
   *     </listheader>
   *     <item>No calls, external parameter entities are silently ignored.</item>
   *   </list>
   *   <list type="table">
   *     <listheader>
   *       Internal Entities - LexicalParameter = True:
   *     </listheader>
   *     <item>
   *       <term>SkipInternal = True.</term>
   *       <description><see cref="IContentHandler.SkippedEntity"/> is called.</description>
   *     </item>
   *     <item>
   *       <term>SkipInternal = False.</term>
   *       <description>No calls, entities silently expanded.</description>
   *     </item>
   *   </list>
   *   <list type="table">
   *     <listheader>
   *       Internal Entities - LexicalParameter = False:
   *     </listheader>
   *     <item>
   *       Parameter entities are not parsed, i.e. silently ignored.
   *     </item>
   *   </list>
   * </item>
   * <item><see cref="ILexicalHandler"/>, internal entities:
   *   The <see cref="ILexicalHandler.StartEntity"/> and
   *   <see cref="ILexicalHandler.EndEntity"/> methods are only called for
   *   external entities, internal entities are not reported (except when
   *   skipped). There is the XML_GetCurrentByteCount() function in the Expat
   *   library which returns 0 if the current call-back is triggered from inside
   *   an internal entity - which could be useful.</item>
   * </list></remarks>
   */
  public class ExpatReader: IXmlReader, IDisposable
  {
    private StringTable strTable;

    SaxExpatParser parser;

    internal IEntityResolver entityResolver;
    private IContentHandler contentHandler;
    private ILexicalHandler lexicalHandler;
    private IDtdHandler dtdHandler;
    private IDeclHandler declHandler;
    private IErrorHandler errorHandler;

    private DefaultHandlerProperty defHandlerProp;

    private ExpatAttributes attributes;
    private ParameterEntityDecls paramEntityDecls;

    private bool
      namespaces,
      namespacePrefixes,
      externalGeneral,
      externalParameter,
      parameterEntities,
      standaloneError,
      parseUnlessStandalone,
      resolveDtdUris;

    private string foreignDoctypeName;

    // holds data used while parsing
    internal DocParseData parseData;

    #region Construction and Cleanup

    [CLSCompliant(false)]
    public ExpatReader(StringTable strTable)
    {
      StrTable = strTable;

      defHandlerProp = new DefaultHandlerProperty(null, OnDefHandlerChange);

      paramEntityDecls = new ParameterEntityDecls();
      attributes = new ExpatAttributes(strTable);

      namespaces = true;
      namespacePrefixes = false;
      parameterEntities = true;
      parseUnlessStandalone = false;
      resolveDtdUris = true;

      parser = new SaxExpatParser(null, namespaces, this);
      parseData = new DocParseData(parser);

      SetParamEntitiesForParsing(parameterEntities);
    }

    public ExpatReader(): this(new StringTable()) { }

    // call at end of each parse process
    protected virtual void Cleanup()
    {
      attributes.AttDecls.Clear();
      paramEntityDecls.Clear();
      parseData.Reset();
    }

    #endregion

    #region IDisposable

    // A derived class should not be able to override this method.
    public void Dispose()
    {
      Cleanup();
      parser.Dispose();
    }

    #endregion

    public bool Parsing
    {
      get { return parser.Parsing; }
    }

    #region Helpers

    private void OnDefHandlerChange(IProperty<IDefaultHandler> property, IDefaultHandler newValue)
    {
      SetDefHandlerForParsing(newValue != null, SkipInternal);
    }

    protected void CheckParsing(RsId whatId)
    {
      if (!Parsing) {
        string msg = KdsConsts.GetString(KdsRsId.IllegalWhenNotParsing);
        string what = Constants.GetString(whatId);
        throw new SaxException(String.Format(msg, what));
      }
    }

    private unsafe void SetLexicalHandlerForParsing(bool doSet)
    {
      if (doSet) {
        parser.CommentHandler = CommentHandlerImpl;
        parser.StartCdataSectionHandler = StartCdataSectionHandlerImpl;
        parser.EndCdataSectionHandler = EndCdataSectionHandlerImpl;
        /* always on - needed to store the DTD declaration
         * parser.StartDoctypeDeclHandler = StartDoctypeDeclHandlerImpl;
         * parser.EndDoctypeDeclHandler = EndDoctypeDeclHandlerImpl;
         */
      }
      else {
        parser.CommentHandler = null;
        parser.StartCdataSectionHandler = null;
        parser.EndCdataSectionHandler = null;
        /* parser.StartDoctypeDeclHandler = null;
         * parser.EndDoctypeDeclHandler = null;
         */
      }
    }

    private unsafe void SetDeclHandlerForParsing(bool doSet)
    {
      if (doSet) {
        parser.ElementDeclHandler = ElementDeclHandlerImpl;
        parser.AttlistDeclHandler = AttlistDeclHandlerImpl;
        /* always on
         * parser.EntityDeclHandler = EntityDeclHandlerImpl;
         */
      }
      else {
        parser.ElementDeclHandler = null;
        parser.AttlistDeclHandler = null;
        /* clear only if nobody else needs it
         * if (dtdHandler == null)
         *   parser.EntityDeclHandler = null;
         */
      }
    }

    private unsafe void SetDefHandlerForParsing(bool doSet, bool skipInternal)
    {
      if (doSet)
        parser.SetDefaultHandler(DefaultHandlerImpl, skipInternal);
      else
        parser.SetDefaultHandler(null, skipInternal);
    }

    private unsafe void SetContentHandlerForParsing(bool doSet)
    {
      if (doSet) {
        parser.StartElementHandler = StartElementHandlerImpl;
        parser.EndElementHandler = EndElementHandlerImpl;
        parser.CharacterDataHandler = CharacterDataHandlerImpl;
        parser.StartNamespaceDeclHandler = StartNamespaceDeclHandlerImpl;
        parser.EndNamespaceDeclHandler = EndNamespaceDeclHandlerImpl;
        parser.ProcessingInstructionHandler = ProcessingInstructionHandlerImpl;
        parser.SkippedEntityHandler = SkippedEntityHandlerImpl;
      }
      else {
        parser.StartElementHandler = null;
        parser.EndElementHandler = null;
        parser.CharacterDataHandler = null;
        parser.StartNamespaceDeclHandler = null;
        parser.EndNamespaceDeclHandler = null;
        parser.ProcessingInstructionHandler = null;
        parser.SkippedEntityHandler = null;
      }
    }

    private unsafe void SetDtdHandlerForParsing(bool doSet)
    {
      if (doSet) {
        parser.NotationDeclHandler = NotationDeclHandlerImpl;
        /* always on
         * parser.EntityDeclHandler = EntityDeclHandlerImpl;
         */
      }
      else {
        parser.NotationDeclHandler = null;
        /* clear only if no one else needs it
         * if (declHandlerProp.Value == null)
         *   parser.EntityDeclHandler = null;
         */
      }
    }

    private unsafe void SetNamespacesForParsing(bool doSet)
    {
      parser.Reset(parser.Encoding, doSet, doSet);
    }

    private unsafe void SetParamEntitiesForParsing(bool doSet)
    {
      XMLParamEntityParsing parEntParsing;
      if (doSet) {
        if (ParseUnlessStandalone)
          parEntParsing = XMLParamEntityParsing.UNLESS_STANDALONE;
        else
          parEntParsing = XMLParamEntityParsing.ALWAYS;
      }
      else
        parEntParsing = XMLParamEntityParsing.NEVER;

      if (!parser.SetParamEntityParsing(parEntParsing)) {
        string msg = SaxRes.GetString(SaxRsId.FeatureNotSupported);
        msg = String.Format(msg, SaxConsts.LexicalParameterFeature, doSet);
        CallErrorHandler(ErrorLevel.Error, msg);
      }
    }

    private unsafe void ProcessExpatError(XMLError errorCode)
    {
      ErrorLevel level;
      if (LibExpat.ErrorInSet(errorCode, XMLErrorSet.OPERATIONAL))
        level = ErrorLevel.Error;
      else  // well-formedness errors are always fatal
        level = ErrorLevel.Fatal;
      string errorMsg = LibExpat.XMLErrorString(errorCode);
      CallErrorHandler(level, errorMsg, (int)errorCode);
    }

    private unsafe ParseStatus StartParsing(InputSource source, bool isStreamSource)
    {
      Cleanup();
      // set up and initialize parsing environment for document entity
      bool sourceOwned = false;
      if (isStreamSource)
        sourceOwned = source is ExpatStreamSource;
      parser.Reset(source.Encoding, Namespaces, Namespaces);
      parser.EntityContext.Init(null, false, source, isStreamSource, sourceOwned);
      if (EntityResolver != null)
        parser.UseForeignDtd = true;

      /* set handlers that we always need */
      parser.XmlDeclHandler = XmlDeclHandlerImpl;
      // Need the next two handlers because storing parameter entity
      // declarations requires them, and because the ContentHandler or
      // LexicalHandler could be turned on at runtime.
      parser.EntityDeclHandler = EntityDeclHandlerImpl;
      parser.StartDoctypeDeclHandler = StartDoctypeDeclHandlerImpl;
      parser.EndDoctypeDeclHandler = EndDoctypeDeclHandlerImpl;
      parser.NotStandaloneHandler = NotStandaloneHandlerImpl;

      if (contentHandler != null) {
        contentHandler.SetDocumentLocator(parseData.Locator);
        parseData.Started = true;
        contentHandler.StartDocument();
      }
      else
        parseData.Started = true;

      return ContinueEntityParsing();
    }

    private void FinishParsing()
    {
      try {
        if (parseData.Started) {
          parseData.Started = false;
          if (contentHandler != null)
            contentHandler.EndDocument();
        }
      }
      finally {
        Cleanup();
      }
    }

    // will not touch non-URIs; requires sysId != null - not checked
    private string ResolveSystemId(string sysId, RsId msgId, string msgArg) 
    {
      string result = sysId;
      try {
        Uri sysIdUri;
        Uri baseUri = parser.EntityContext.BaseUri;
        if (baseUri == null)
          // we will get an exception if sysId is not an absolute URI
          sysIdUri = new Uri(sysId);
        else
          sysIdUri = new Uri(baseUri, sysId);
        result = sysIdUri.AbsoluteUri;
      }
      catch (UriFormatException) {
        // sysId is not an URI, so we don't touch it
      }
      catch (Exception e) {
        string msg = Constants.GetString(msgId);
        msg = String.Format(msg, msgArg);
        CallErrorHandler(ErrorLevel.Error, msg, e);
      }
      return result;
    }

    // Precondition: parser != null, initialized correctly
    private unsafe ParseStatus ContinueEntityParsing()
    {
      SaxEntityContext entContext = parser.EntityContext;
      // exactly one of these two must be != null
      TextReader reader = entContext.Reader;
      Stream stream = entContext.Stream;
      Debug.Assert(reader != null || stream != null);

      ReadBuffer read;
      if (stream != null)
        read = new StreamBufferReader(stream).Read;
      else
        read = new TextBufferReader(reader).Read;
      ParseStatus status = parser.Parse(read);
      if (status == ParseStatus.FatalError)
        ProcessExpatError(entContext.Error);
      return status;
    }

    private InputSource DefaultResolveExternalEntity(
      Uri baseUri,
      string systemId,
      out Exception error)
    {
      InputSource result = null;
      error = null;

      try {
        // must absolutize systemId if it is an URI
        Uri sysIdUri = null;
        Stream entityStream = null;
        try {
          if (baseUri == null)
            sysIdUri = new Uri(systemId);
          else
            sysIdUri = new Uri(baseUri, systemId);
        }
        catch (UriFormatException) {
          // not an URI, we assume it is a file name
          entityStream =
                new FileStream(systemId, FileMode.Open, FileAccess.Read);
        }
        if (entityStream == null) {
          WebRequest request = WebRequest.Create(sysIdUri);
          try {
            request.Timeout = Constants.UriTimeout;
          }
          catch (NotSupportedException) { }  // ignore if not supported
          WebResponse response = request.GetResponse();
          entityStream = response.GetResponseStream();
        }
        result = new ExpatStreamSource(entityStream);
        if (sysIdUri != null)
          result.SystemId = sysIdUri.AbsoluteUri;
        else
          result.SystemId = systemId;
      }
      catch (Exception e) {
        error = e;
      }
      return result;
    }

    #endregion

    #region Internal Properties and Methods

    internal ExpatAttributes Attributes
    {
      get { return attributes; }
    }

    internal ParameterEntityDecls ParamEntityDecls
    {
      get { return paramEntityDecls; }
    }

    internal void CallErrorHandler(ErrorLevel level, string message)
    {
      if (ErrorHandler != null) {
        switch (level) {
          case ErrorLevel.Warning:
            ErrorHandler.Warning(parseData.Error(message));
            break;
          case ErrorLevel.Error:
            ErrorHandler.Error(parseData.Error(message));
            break;
          case ErrorLevel.Fatal:
            ErrorHandler.FatalError(parseData.Error(message));
            break;
        }
      }
      else if (level != ErrorLevel.Warning)
        throw new SaxParseException(message);
    }

    internal void CallErrorHandler(ErrorLevel level, string message, Exception e)
    {
      if (ErrorHandler != null) {
        switch (level) {
          case ErrorLevel.Warning:
            ErrorHandler.Warning(parseData.Error(message, e));
            break;
          case ErrorLevel.Error:
            ErrorHandler.Error(parseData.Error(message, e));
            break;
          case ErrorLevel.Fatal:
            ErrorHandler.FatalError(parseData.Error(message, e));
            break;
        }
      }
      else if (level != ErrorLevel.Warning)
        throw new SaxParseException(message, e);
    }

    internal void CallErrorHandler(ErrorLevel level, string message, int code)
    {
      if (ErrorHandler != null) {
        switch (level) {
          case ErrorLevel.Warning:
            ErrorHandler.Warning(parseData.Error(message, code));
            break;
          case ErrorLevel.Error:
            ErrorHandler.Error(parseData.Error(message, code));
            break;
          case ErrorLevel.Fatal:
            ErrorHandler.FatalError(parseData.Error(message, code));
            break;
        }
      }
      else if (level != ErrorLevel.Warning)
        throw new SaxParseException(parseData.Error(message, code));
    }

    internal InputSource ResolveForeignDtd(string name)
    {
      InputSource result = null;
      if (EntityResolver != null) {
        // parser.EntityContext.BaseUri is guaranteed to be absolute
        Uri baseUri = parser.EntityContext.BaseUri;
        string bsUri = baseUri == null ? null : baseUri.AbsoluteUri;
        result = EntityResolver.GetExternalSubset(name, bsUri);
        // result == null is not an error, but an application choice
      }
      return result;
    }

    internal InputSource ResolveExternalEntity(
      string name,
      string publicId,
      Uri baseUri,
      string systemId,
      out string msg,
      out Exception error)
    {
      InputSource result = null;
      msg = null;
      error = null;

      // try entity resolver first, if one is assigned
      if (EntityResolver != null) {
        string bsUri = baseUri == null ? null : baseUri.AbsoluteUri;
        // systemId does not need to be absolutized, as we pass the base URI
        result = EntityResolver.ResolveEntity(name, publicId, bsUri, systemId);
      }
      if (result != null)
        return result;
      // still unresolved - use internal (default) entity resolver
      result = DefaultResolveExternalEntity(baseUri, systemId, out error);
      if (result == null) {
        string tmpMsg = KdsConsts.GetString(KdsRsId.CannotResolveEntity);
        msg = String.Format(tmpMsg, publicId, systemId);
      }
      return result;
    }

    #endregion

    #region IXmlReader

    // must be in sync with GetFeature()
    private readonly string[] featureNames = new string[] {
      SaxConsts.NamespacesFeature,
      SaxConsts.NamespacePrefixesFeature,
      SaxConsts.ValidationFeature,
      SaxConsts.ExternalGeneralFeature,
      SaxConsts.ExternalParameterFeature,
      SaxConsts.ResolveDtdUrisFeature,
      SaxConsts.LexicalParameterFeature,
      SaxConsts.XmlNsUrisFeature,
      SaxConsts.Xml11Feature,
      SaxConsts.UnicodeNormCheckFeature,
      SaxConsts.XmlDeclFeature,
      SaxConsts.UseExternalSubsetFeature,
      SaxConsts.ReaderControlFeature,
      KdsConsts.SkipInternalFeature,
      KdsConsts.ParseUnlessStandaloneFeature,
      KdsConsts.ParameterEntitiesFeature,
      KdsConsts.StandaloneErrorFeature
    };

    private int FeatureIndex(string feature)
    {
      int result = Array.IndexOf(featureNames, feature);
      if (result < featureNames.GetLowerBound(0)) {
        string msg = SaxRes.GetString(SaxRsId.FeatureNotRecognized);
        throw new ArgumentException(String.Format(msg, feature), "name");
      }
      return result;
    }

    /// <summary>Returns if named feature is turned on or not.</summary>
    /// <remarks>Supported features are:
    ///
    /// <list type="table">
    ///   <listheader>
    ///     Standard features - prefixed with "http://xml.org/sax/features/"
    ///     <term>Name</term>
    ///     <description>Properties</description>
    ///   </listheader>
    ///   <item>
    ///     <term>namespaces</term>
    ///     <description>
    ///       Access: read/write, default true.<br/>
    ///       Should be set *before* parsing is started. If set later, it will
    ///       only affect the reporting of QNames, that is, if namespaces was
    ///       true at the beginning, then QName/Prefix reporting can be turned
    ///       off during parsing, but namespace processing will stay active.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>namespace-prefixes</term>
    ///     <description>
    ///       Access: read-only, value depends on namespaces feature.<br/>
    ///       The underlying Expat parser never reports xmlns attributes when
    ///       namespace processing is turned on, so this feature is set to false
    ///       in this case even though prefixes (in QNames) would still be reported.
    ///       Conversely, when namespace processing is turned off then this feature
    ///       is automatically set to true.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>validation</term>
    ///     <description>
    ///       Access: read/write, fixed false.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>external-general-entities</term>
    ///     <description>
    ///       Access: read/write, default false.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>external-parameter-entities</term>
    ///     <description>
    ///       Access: read/write, default false.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>resolve-dtd-uris</term>
    ///     <description>
    ///       Access: read/write, default false.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>lexical-handler/parameter-entities</term>
    ///     <description>
    ///       Access: read/write, default true.<br/>
    ///       With limitations - See remarks regarding internal entities
    ///       and <see cref="ILexicalHandler"/> on <see cref="ExpatReader"/>.
    ///       It is not certain that the way this feature is implemented
    ///       conforms to SAX2, but it works the same way as in MSXML4.
    ///       It seems that if it is turned off then parameter entities should
    ///       still be reported if skipped, but this is not done here since
    ///       internally this feature is equivalent to the parameter-entities
    ///       extension feature which turns parameter entity parsing on or off.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>xmlns-uris</term>
    ///     <description>
    ///       Access: read/write, fixed false.<br/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>xml-1.1</term>
    ///     <description>
    ///       Access: read-only, fixed false.<br/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>unicode-normalization-checking</term>
    ///     <description>
    ///       Access: read/write, fixed false.<br/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>xml-declaration</term>
    ///     <description>
    ///       Access: read-only, fixed true.<br/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>use-external-subset</term>
    ///     <description>
    ///       Access: read-only, fixed true.<br/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>reader-control</term>
    ///     <description>
    ///       Access: read-only, default depends on compile options.<br/>
    ///       Returns if the Suspend()/Resume() functionality is implemented.
    ///       This is true when compiled with EXPAT_1_95_8_UP defined
    ///       and version 1.95.8 (or later) of the Expat library is used.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <list type="table">
    ///   <listheader>
    ///     Extension features - prefixed with "http://kd-soft.net/sax/features/"
    ///     <term>Name</term>
    ///     <description>Properties</description>
    ///   </listheader>
    ///   <item>
    ///     <term>parameter-entities</term>
    ///     <description>
    ///       Access: read/write, default true.<br/>
    ///       Turns parameter entity parsing on or off. This affects all of the
    ///       external subset, as well as parameter entities in the internal
    ///       subset. The rest of the internal subset is still being processed.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>skip-internal-entities</term>
    ///     <description>
    ///       Access: read/write, default false.<br/>
    ///       Skip or (silently) expand internal entities (parameter and general).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>parse-unless-standalone</term>
    ///     <description>
    ///       Access: read/write, default false.<br/>
    ///       When turned on external parameter entities will not be parsed
    ///       if the XML declaration specifies standalone = "yes".
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>standalone-error</term>
    ///     <description>
    ///       Access: read/write, default false.<br/>
    ///       Determines if the parser returns an error, when it encounters
    ///       an external subset or a reference to a parameter entity, but
    ///       the document has standalone="yes" in the XML declaration.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    ///   Note: "read/write, fixed false" means that the feature can be set,
    ///   but only to the value false, otherwise an exception will be thrown.
    /// </para>
    /// </remarks>
    public bool GetFeature(string name)
    {
      switch (FeatureIndex(name)) {
        case 0:  // http://xml.org/sax/features/namespaces
          return Namespaces;
        case 1:  // http://xml.org/sax/features/namespace-prefixes
          return NamespacePrefixes;
        case 2:  // http://xml.org/sax/features/validation
          return Validation;
        case 3:  // http://xml.org/sax/features/external-general-entities
          return ExternalGeneral;
        case 4:  // http://xml.org/sax/features/external-parameter-entities
          return ExternalParameter;
        case 5:  // http://xml.org/sax/features/resolve-dtd-uris
          return ResolveDtdUris;
        case 6:  // http://xml.org/sax/features/lexical-handler/parameter-entities
          return ParameterEntities;
        case 7:  // http://xml.org/sax/features/xmlns-uris
          return XmlNsUris;
        case 8:  // http://xml.org/sax/features/xml-1.1
          return Xml11;
        case 9:  // http://xml.org/sax/features/unicode-normalization-checking
          return UnicodeNormCheck;
        case 10: // http://xml.org/sax/features/xml-declaration
          return XmlDecl;
        case 11: // http://xml.org/sax/features/use-external-subset
          return UseExternalSubset;
        case 12: // http://xml.org/sax/features/reader-control
          return ReaderControl;
        case 13: // http://kd-soft.net/sax/features/skip-internal-entities
          return SkipInternal;
        case 14: // http://kd-soft.net/sax/features/parse-unless-standalone
          return ParseUnlessStandalone;
        case 15: // http://kd-soft.net/sax/features/parameter-entities
          return ParameterEntities;
        case 16: // http://kd-soft.net/sax/features/standalone-error
          return StandaloneError;
        default:
          string msg = SaxRes.GetString(SaxRsId.FeatureNotSupported);
          throw new NotSupportedException(String.Format(msg, name));
      }
    }

    /// <summary>Turns named feature on or off.</summary>
    /// <remarks>See remarks on <see cref="GetFeature"/>.</remarks>
    public void SetFeature(string name, bool value)
    {
      switch (FeatureIndex(name)) {
        case 0:  // http://xml.org/sax/features/namespaces
          Namespaces = value;
          return;
        case 1:  // http://xml.org/sax/features/namespace-prefixes
          NamespacePrefixes = value;
          return;
        case 2:  // http://xml.org/sax/features/validation
          Validation = value;
          return;
        case 3:  // http://xml.org/sax/features/external-general-entities
          ExternalGeneral = value;
          return;
        case 4:  // http://xml.org/sax/features/external-parameter-entities
          ExternalParameter = value;
          return;
        case 5:  // http://xml.org/sax/features/resolve-dtd-uris
          ResolveDtdUris = value;
          return;
        case 6:  // http://xml.org/sax/features/lexical-handler/parameter-entities
          ParameterEntities = value;
          return;
        case 7:  // http://xml.org/sax/features/xmlns-uris
          XmlNsUris = value;
          return;
        case 8:  // http://xml.org/sax/features/xml-1.1
          break;
        case 9:  // http://xml.org/sax/features/unicode-normalization-checking
          UnicodeNormCheck = value;
          return;
        case 10: // http://xml.org/sax/features/xml-declaration
          break;
        case 11: // http://xml.org/sax/features/use-external-subset
          break;
        case 12: // http://xml.org/sax/features/reader-control
          break;
        case 13: // http://kd-soft.net/sax/features/skip-internal-entities
          SkipInternal = value;
          return;
        case 14: // http://kd-soft.net/sax/features/parse-unless-standalone
          ParseUnlessStandalone = value;
          return;
        case 15: // http://kd-soft.net/sax/features/parameter-entities
          ParameterEntities = value;
          return;
        case 16: // http://kd-soft.net/sax/features/standalone-error
          StandaloneError = value;
          return;
        default:
          break;
      }
      string msg = SaxRes.GetString(SaxRsId.FeatureWriteNotSupported);
      throw new NotSupportedException(String.Format(msg, name));
    }

    /// <summary>Returns interface to named property.</summary>
    /// <remarks>Supported properties are:
    /// <list type="table">
    ///   <listheader>
    ///     Extension properties - prefixed with "http://kd-soft.net/sax/properties/"
    ///     <term>Name</term>
    ///     <description>Properties</description>
    ///   </listheader>
    ///   <item>
    ///     <term>default-handler</term>
    ///     <description>
    ///       Value access: read/write.<br/>
    ///       The default handler receives events for which no handler is set.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public IProperty<T> GetProperty<T>(string name)
    {
      switch(name) {
        case Kds.Xml.Sax.Constants.DefaultHandlerProperty:
          return (IProperty<T>)defHandlerProp;
        default:
          return null;
      }
    }

    /// <remarks>When <see cref="IEntityResolver.GetExternalSubset"/> is called
    /// for a document without a DOCTYPE declaration, then the Name argument
    /// will be empty since Expat does not read ahead to determine the name
    /// of the root element. This behaviour does not follow the SAX2 specs.
    /// However, it should not be a problem because the application which
    /// provides the external subset should rather use information from the
    /// application context to supply the correct DTD, and not rely on the
    /// Name argument.</remarks>
    public IEntityResolver EntityResolver
    {
      get { return entityResolver; }
      set { entityResolver = value; }
    }

    public IDtdHandler DtdHandler
    {
      get { return dtdHandler; }
      set {
        dtdHandler = value;
        SetDtdHandlerForParsing(value != null);
      }
    }

    public IContentHandler ContentHandler
    {
      get { return contentHandler; }
      set {
        contentHandler = value;
        SetContentHandlerForParsing(value != null);
      }
    }

    public ILexicalHandler LexicalHandler
    {
      get { return lexicalHandler; }
      set {
        lexicalHandler = value;
        SetLexicalHandlerForParsing(value != null);
      }
    }

    public IDeclHandler DeclHandler
    {
      get { return declHandler; }
      set { 
        declHandler = value;
        SetDeclHandlerForParsing(value != null);
      }
    }

    public IErrorHandler ErrorHandler
    {
      get { return errorHandler; }
      set { errorHandler = value; }
    }

    // source must be InputSource<Stream> or InputSource<TextReader> - not checked!
    private void ParseSource(InputSource source, bool isStreamSource)
    {
      try {
        ParseStatus status = StartParsing(source, isStreamSource);
        if (status != ParseStatus.Suspended)
          FinishParsing();
      }
      catch {
        FinishParsing();
        throw;
      }
    }

    public void Parse(InputSource input)
    {
      if (input == null)
        throw new ArgumentNullException("input");
      bool isStreamSource = input is InputSource<Stream>;
      // we can deal with InputSource<Stream> and InputSource<TextReader> only,
      // for everything else we will use the SystemId to resolve the entity
      if (isStreamSource || (input is InputSource<TextReader>))
        ParseSource(input, isStreamSource);
      else {
        string msg = Constants.GetString(RsId.StreamReaderInputSourceOnly);
        if (input.SystemId != String.Empty) {
          CallErrorHandler(ErrorLevel.Warning, msg);
          Parse(input.SystemId);
        }
        else
          throw new ArgumentException(msg);
      }
    }

    public void Parse(string systemId)
    {
      string msg;
      Exception error;
      InputSource input = DefaultResolveExternalEntity(null, systemId, out error);
      if (input == null) {
        if (error == null) {
          msg = KdsConsts.GetString(KdsRsId.CannotResolveEntity);
          throw new SaxParseException(String.Format(msg, "", systemId));
        }
        else
          throw error;
      }
      bool isStreamSource = input is InputSource<Stream>;
      // we can deal with StreamInputSource and ReaderInputSource only
      if (isStreamSource || (input is InputSource<TextReader>))
        ParseSource(input, isStreamSource);
      else {
        msg = Constants.GetString(RsId.StreamReaderInputSourceOnly);
        throw new SaxParseException(msg);
      }
    }

    #endregion

    #region SAX2 Extension Properties and Features exposed as C# Properties

    public bool Namespaces
    {
      get { return namespaces; }
      set {
        SetNamespacesForParsing(value);
        namespaces = value;
        // in SAXExpat namespace-prefixes depends on namespaces
        namespacePrefixes = !value;
      }
    }

    public bool NamespacePrefixes
    {
      get { return namespacePrefixes; }
      set {
        // in SAXExpat namespace-prefixes depends on namespaces
        if (value != namespacePrefixes) {
          string msg = Constants.GetString(RsId.NamespacePrefixesDepends);
          throw new NotSupportedException(msg);
        }
      }
    }

    public bool Validation
    {
      get { return false; }
      set {
        if (value) {
          string msg = SaxRes.GetString(SaxRsId.FeatureNotSupported);
          msg = String.Format(msg, SaxConsts.ValidationFeature, value);
          throw new NotSupportedException(msg);
        }
      }
    }

    public bool ExternalGeneral
    {
      get { return externalGeneral; }
      set { externalGeneral = value; }
    }

    public bool ExternalParameter
    {
      get { return externalParameter; }
      set { externalParameter = value; }
    }

    public bool AttributeDeclared
    {
      get { return true; }
    }

    public bool XmlDecl
    {
      get { return true; }
    }

    public bool UseExternalSubset
    {
      get { return true; }
    }

    public bool IsStandalone
    {
      get { return DocStandalone == XmlDocStandalone.Yes; }
    }

    public bool ResolveDtdUris
    {
      get { return resolveDtdUris; }
      set { resolveDtdUris = value; }
    }

    public bool ReaderControl
    {
      get {
      #if EXPAT_1_95_8_UP
        return true;
      #else
        return false;
      #endif
      }
    }

#if EXPAT_1_95_8_UP
    public void Suspend()
    {
      if (!Parsing) {
        string msg = KdsConsts.GetString(KdsRsId.IllegalWhenNotParsing);
        throw new SaxParseException(String.Format(msg, "Suspend"));
      }
      if (!parser.Suspend()) {
        switch (parser.EntityContext.Error) {
          case XMLError.FINISHED:    // parser finished - ignore error
            goto case XMLError.SUSPEND_PE;
          case XMLError.SUSPEND_PE:  // parser not suspendable - ignore error
            return;
        }
        throw new SaxException(LibExpat.XMLErrorString(parser.EntityContext.Error));
      }
    }

    public void Abort()
    {
      if (!Parsing) {
        string msg = KdsConsts.GetString(KdsRsId.IllegalWhenNotParsing);
        throw new SaxParseException(String.Format(msg, "Abort"));
      }
      // no cleanup here, as we are in the middle of a call-back
      if (!parser.Abort()) {
        // if we are at end of entity, ignore error
        if (parser.EntityContext.Error == XMLError.FINISHED)
          return;
        throw new SaxException(LibExpat.XMLErrorString(parser.EntityContext.Error));
      }
    }

    public void Resume()
    {
      if (!Parsing) {
        string msg = KdsConsts.GetString(KdsRsId.IllegalWhenNotParsing);
        throw new SaxParseException(String.Format(msg, "Resume"));
      }
      try {
        ParseStatus status = parser.Resume();
        switch (status) {
          case ParseStatus.Suspended:
            break;
          case ParseStatus.FatalError:
            if (parser.EntityContext.Error != XMLError.NONE)
              ProcessExpatError(parser.EntityContext.Error);
            FinishParsing();
            break;
          default:
            FinishParsing();
            break;
        }
      }
      catch {
        FinishParsing();
        throw;
      }
    }

    public XmlReaderStatus Status
    {
      get {
        if (!Parsing)
          return XmlReaderStatus.Ready;
        else {
          switch (parser.ParsingStatus.Parsing) {
            case XMLParsing.INITIALIZED:
              return XmlReaderStatus.Ready;
            case XMLParsing.PARSING:
              return XmlReaderStatus.Parsing;
            case XMLParsing.FINISHED:
              return XmlReaderStatus.Parsing;
            case XMLParsing.SUSPENDED:
              return XmlReaderStatus.Suspended;
            default:
              return XmlReaderStatus.Parsing;
          }
        }
      }
    }
#else
    public void Suspend()
    {
      throw new NotSupportedException();
    }

    public void Abort()
    {
      throw new NotSupportedException();
    }

    public void Resume()
    {
      throw new NotSupportedException();
    }

    public XmlReaderStatus Status
    {
      get {
        if (Parsing)
          return XmlReaderStatus.Parsing;
        else
          return XmlReaderStatus.Ready;
      }
    }
#endif

    #endregion

    #region SAXExpat Extension Properties and Features exposed as C# Properties

    public IDefaultHandler DefaultHandler
    {
      get { return (IDefaultHandler)(defHandlerProp.Value); }
      set { defHandlerProp.Value = value; }
    }

    /// <summary>Base URI of the entity currently being parsed.</summary>
    /// <remarks>Must be an absolute URI.</remarks>
    public unsafe string BaseUri
    {
      get {
        CheckParsing(RsId.AccessingBaseUri);
        Uri baseUri = parser.EntityContext.BaseUri;
        string result = baseUri == null ? null : baseUri.AbsoluteUri;
        return result;
      }
      set {
        CheckParsing(RsId.AccessingBaseUri);
        if (value == null)
          parser.EntityContext.BaseUri = null;
        else
          // we will get an exception if value is not an absolute URI
          parser.EntityContext.BaseUri = new Uri(value);
      }
    }

    public bool ParameterEntities
    {
      get { return parameterEntities; }
      set {
        parameterEntities = value;
        SetParamEntitiesForParsing(value);
      }
    }

    public bool XmlNsUris
    {
      get { return false; }
      set {
        if (value) {
          string msg = SaxRes.GetString(SaxRsId.FeatureNotSupported);
          msg = String.Format(msg, SaxConsts.XmlNsUrisFeature, value);
          throw new NotSupportedException(msg);
        }
      }
    }

    public bool Xml11
    {
      get { return false; }
    }

    public bool UnicodeNormCheck
    {
      get { return false; }
      set {
        if (value) {
          string msg = SaxRes.GetString(SaxRsId.FeatureNotSupported);
          msg = String.Format(msg, SaxConsts.UnicodeNormCheckFeature, value);
          throw new NotSupportedException(msg);
        }
      }
    }

    public bool SkipInternal
    {
      get { return parser.SkipInternal; }
      set { SetDefHandlerForParsing(DefaultHandler != null, value); }
    }

    public bool ParseUnlessStandalone
    {
      get { return parseUnlessStandalone; }
      set {
        parseUnlessStandalone = value;
        SetParamEntitiesForParsing(value);
      }
    }

    public bool StandaloneError
    {
      get { return standaloneError; }
      set { standaloneError = value; }
    }

    public XmlDocStandalone DocStandalone
    {
      get {
        if (!Parsing) {
          string msg = SaxRes.GetString(SaxRsId.FeatureWhenParsing);
          throw new NotSupportedException(msg);
        }
        if (parseData.Standalone < 0)
          return XmlDocStandalone.Undefined;
        else if (parseData.Standalone > 0)
          return XmlDocStandalone.Yes;
        else
          return XmlDocStandalone.No;
      }
    }

    public string ExpatVersion
    {
      get { return LibExpat.XMLExpatVersion(); }
    }

    public string ForeignDoctypeName
    {
      get { return foreignDoctypeName; }
      set { foreignDoctypeName = value; }
    }

    /// <summary>String table used for string interning.</summary>
    /// <remarks>Must not be <c>null</c>. Should not be set while parsing.</remarks>
    [CLSCompliant(false)]
    public StringTable StrTable
    {
      get { return strTable; }
      set {
        if (value == null)
          throw new ArgumentNullException("StrTable");
        strTable = value;
      }
    }

    /// <summary>Expat specific content model structure.</summary>
    /// <remarks>Only valid in ElementDecl() handler call-back.</remarks>
    [CLSCompliant(false)]
    public unsafe XMLContent* ContentModel
    {
      get { return parseData.ContentModel; }
    }

    #endregion

    #region Expat Handlers

    /* ElementDeclHandler */

    // helper routine to convert XMLContent structure back to string
    private static unsafe void
    WriteModelNode(StringBuilder builder, StringTable strTable, XMLContent* node)
    {
      uint num;
      XMLContent* child;
      char sepChar = '|';
      switch ((*node).Type) {
        case XMLContentType.EMPTY:
          builder.Append("EMPTY");
          break;
        case XMLContentType.ANY:
          builder.Append("ANY");
          break;
        case XMLContentType.NAME:
          // we assume that (*node).Name != null
          string nameStr = strTable.Intern((*node).Name);
          builder.Append(nameStr);
          break;
        case XMLContentType.CHOICE:
          sepChar = ',';
          goto case XMLContentType.SEQ;
        case XMLContentType.SEQ:
          builder.Append(KdsConsts.LeftPar);
          num = (*node).NumChildren;
          Debug.Assert(num > 0);
          // sepChar is either '|' or ','
          child = (*node).Children;
          WriteModelNode(builder, strTable, child);
          child++;
          num--;
          while (num > 0) {
            builder.Append(sepChar);
            WriteModelNode(builder, strTable, child);
            child++;
            num--;
          }
          builder.Append(KdsConsts.RightPar);
          break;
        case XMLContentType.MIXED:
          builder.Append("(#PCDATA");
          num = (*node).NumChildren;
          child = (*node).Children;
          while (num > 0) {
            builder.Append('|');
            WriteModelNode(builder, strTable, child);
            child++;
            num--;
          }
          builder.Append(KdsConsts.RightPar);
          break;
      }
      switch ((*node).Quant) {
        case XMLContentQuant.OPT:
          builder.Append('?');
          break;
        case XMLContentQuant.REP:
          builder.Append('*');
          break;
        case XMLContentQuant.PLUS:
          builder.Append('+');
          break;
      }
    }

    private static unsafe void
    ElementDeclHandlerImpl(IntPtr userData, char* name, XMLContent* model)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      IDeclHandler declHandler = reader.DeclHandler;
      // not necessary - callback only set when DeclHandler set
      // if (declHandler == null) return;
      try {
        reader.parseData.ContentModel = model;
        StringBuilder builder = reader.parseData.ContentBuilder;
        builder.Length = 0;
        WriteModelNode(builder, reader.StrTable, model);
        string nameStr = reader.StrTable.Intern(name);
        declHandler.ElementDecl(nameStr, builder.ToString());
      }
      finally {
        // was allocated by Expat, but we own it and must de-allocate it
        reader.parser.FreeContentmodel(model);
        reader.parseData.ContentModel = null;
      }
    }

    /* AttlistDeclHandler */

    /* Note: DTDs don't know about namespaces, therefore the elName and attName
     * arguments have to be taken literally.
     */
    private static unsafe void
    AttlistDeclHandlerImpl(IntPtr userData,
                           char* elName,
                           char* attName,
                           char* attType,
                           char* dflt,
                           int isRequired)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      IDeclHandler declHandler = reader.DeclHandler;
      // not necessary - callback only set when DeclHandler set
      // if (declHandler == null) return;
      string dfltType;
      // determine default type
      if (isRequired != 0) {
        if (dflt == null)
          dfltType = "#REQUIRED";
        else
          dfltType = "#FIXED";
      }
      else {
        if (dflt == null)
          dfltType = "#IMPLIED";
        else
          dfltType = null;
      }

      StringTable strTable = reader.StrTable;
      string elNameStr = strTable.Intern(elName);
      string attNameStr = strTable.Intern(attName);
      string attTypeStr = strTable.Intern(attType);
      string dfltStr = (dflt == null) ? null : strTable.Intern(dflt);
      reader.Attributes.AttDecls.Add(
                        elNameStr, attNameStr, attTypeStr, dfltType, dfltStr);
      declHandler.AttributeDecl(
                        elNameStr, attNameStr, attTypeStr, dfltType, dfltStr);
    }

    /* XmlDeclHandler */

    /* Notes:
     * - Version and Encoding are reported through the new ILocator interface,
     *   Standalone is reported through the ILocator.EntityType property.
     * - Called when the XML or Text declarations are parsed. The value of
     *   standalone will be 1 if the document is declared standalone, 0 if it is
     *   declared not to be standalone, or -1 if the standalone clause was omitted.
     */
    private static unsafe void
    XmlDeclHandlerImpl(IntPtr userData,
                       char* version,
                       char* encoding,
                       int standalone)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // The docs say that version == null for Text declarations (which occur
      // in external entities), but this does not seem to be correct, so we
      // use a different test to check if we have an XML or a Text declaration.
      SaxEntityContext entSC = reader.parser.EntityContext;
      if (entSC.Parent == null)
        reader.parseData.Standalone = standalone;
      entSC.Version = (version == null) ?
                        null : reader.StrTable.Intern(version);
      // The declaration's encoding cannot override the input source!
      if (entSC.Encoding == null)  // no encoding set by input source
        entSC.Encoding = (encoding == null) ?
                           null : reader.StrTable.Intern(encoding);
    }

    /* StartElementHandler */

    /* Note: the atts argument is only valid for this call. */
    private static unsafe void
    StartElementHandlerImpl(IntPtr userData, char* name, char** atts)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when ContentHandler set
      // if (reader.ContentHandler == null) return;
      string uri, locName, qName;
      int specAttCount = reader.parser.SpecifiedAttributeCount;
      ExpatUtils.ParseNameSax(name, reader.StrTable, out uri, out locName, out qName);
      ExpatAttributes attributes = reader.Attributes;
      attributes.Initialize(qName, atts, specAttCount);
      reader.ContentHandler.StartElement(uri, locName, qName, attributes);
    }

    /* EndElementHandler */

    private static unsafe void
    EndElementHandlerImpl(IntPtr userData, char* name)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when ContentHandler set
      // if (reader.ContentHandler == null) return;
      string uri, locName, qName;
      ExpatUtils.ParseNameSax(name, reader.StrTable, out uri, out locName, out qName);
      reader.ContentHandler.EndElement(uri, locName, qName);
    }

    /* CharacterDataHandler */

    private static unsafe void
    CharacterDataHandlerImpl(IntPtr userData, char* s, int len)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when ContentHandler set
      // if (reader.ContentHandler == null) return;
      char[] charBuf = reader.parseData.GetCharBuffer(len);
      Marshal.Copy((IntPtr)s, charBuf, 0, len);
      reader.ContentHandler.Characters(charBuf, 0, len);
    }

    /* ProcessingInstructionHandler */

    /*  target and data are null terminated */
    private static unsafe void
    ProcessingInstructionHandlerImpl(IntPtr userData, char* target, char* data)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when ContentHandler set
      // if (reader.ContentHandler == null) return;
      StringTable strTable = reader.StrTable;
      string targetStr = strTable.Intern(target);
      string dataStr = (data == null) ? null : strTable.Intern(data);
      reader.ContentHandler.ProcessingInstruction(targetStr, dataStr);
    }

    /* CommentHandler */

    /* data is null terminated */
    private static unsafe void
    CommentHandlerImpl(IntPtr userData, char* data)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when LexicalHandler set
      // if (reader.LexicalHandler == null) return;
      int len = StringUtils.StrLen(data);
      char[] charBuf = reader.parseData.GetCharBuffer(len);
      Marshal.Copy((IntPtr)data, charBuf, 0, len);
      reader.LexicalHandler.Comment(charBuf, 0, len);
    }

    /* StartCdataSectionHandler */

    private static void
    StartCdataSectionHandlerImpl(IntPtr userData)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when LexicalHandler set
      // if (reader.LexicalHandler == null) return;
      reader.LexicalHandler.StartCData();
    }

    /* EndCdataSectionHandler */

    private static void
    EndCdataSectionHandlerImpl(IntPtr userData)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when LexicalHandler set
      // if (reader.LexicalHandler == null) return;
      reader.LexicalHandler.EndCData();
    }

    /* DefaultHandler */

    private static unsafe void
    DefaultHandlerImpl(IntPtr userData, char* s, int len)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when DefaultHandler set
      // if (reader.DefaultHandler == null) return;
      char[] charBuf = reader.parseData.GetCharBuffer(len);
      Marshal.Copy((IntPtr)s, charBuf, 0, len);
      reader.DefaultHandler.UnhandledData(charBuf, 0, len);
    }

    /* StartDoctypeDeclHandler */

    /* Note: not sure what to do with HasInternalSubset (how to pass or store) */
    private static unsafe void
    StartDoctypeDeclHandlerImpl(IntPtr userData,
                                char* doctypeName,
                                char* systemId,
                                char* publicId,
                                int hasInternalSubset)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // normalize empty strings
      string pubId = null;
      string sysId = null;
      string doctype = reader.StrTable.Intern(doctypeName);

      if (publicId != null && *publicId != Constants.NullChar)
        pubId = new string(publicId);
      if (systemId != null && *systemId != Constants.NullChar)
        sysId = new string(systemId);

      reader.parseData.HasDoctypeDecl = true;
      if (systemId == null) {
        InputSource dtdSource = reader.ResolveForeignDtd(doctype);
        reader.parseData.ForeignDtdSource = dtdSource;
        if (dtdSource != null) {
          pubId = dtdSource.PublicId;
          sysId = dtdSource.SystemId;
        }
      }
      // If publicId == null *and* systemId == null, then we don't have
      // an external subset, but ExternalEntityRefHandler may still be
      // called for a foreign DTD, so maybe we should store the entity
      // with a dummy system id.
      if (pubId == null && sysId == null)
        sysId = Constants.ForeignDtdId;
      reader.ParamEntityDecls.Add(KdsConsts.XmlDtdName, pubId, sysId);
      ILexicalHandler lexHandler = reader.LexicalHandler;
      if (lexHandler == null) return;
      // we report the "declared", not absolutized, system identifier
      lexHandler.StartDtd(doctype, pubId, sysId);
    }

    /* EndDoctypeDeclHandler */

    private static void
    EndDoctypeDeclHandlerImpl(IntPtr userData)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      ILexicalHandler lexHandler = reader.LexicalHandler;
      if (lexHandler == null) return;
      lexHandler.EndDtd();
    }

    /* EntityDeclHandler */

    private static unsafe void
    EntityDeclHandlerImpl(IntPtr userData,
                          char* entityName,
                          int isParameterEntity,
                          char* value,
                          int valueLen,
                          char* baseUri,
                          char* systemId,
                          char* publicId,
                          char* notationName)
    {
      int entLen = (entityName == null) ? 0 : StringUtils.StrLen(entityName);
      Debug.Assert(entLen > 0);
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // normalize empty strings
      string pubId = null;
      string sysId = null;
      if (publicId != null && *publicId != Constants.NullChar)
        pubId = new string(publicId);
      if (systemId != null && *systemId != Constants.NullChar)
        sysId = new string(systemId);

      string entNameStr;
      if (isParameterEntity == 0) {  // general entity
        entNameStr = reader.StrTable.Intern(entityName, entLen);
      }
      else {  // parameter entity
        char[] charBuf = reader.parseData.GetCharBuffer(entLen + 1);
        charBuf[0] = '%';
        Marshal.Copy((IntPtr)entityName, charBuf, 1, entLen);
        entNameStr = reader.StrTable.Intern(charBuf, 0, entLen + 1);
        if (sysId != null)
          // we report the "declared", not absolutized, system identifier
          reader.ParamEntityDecls.Add(entNameStr, pubId, sysId);
      }

      if (notationName != null) { // unparsed entity declaration
        IDtdHandler dtdHandler = reader.DtdHandler;
        if (dtdHandler == null)
          return;
        // must absolutize the system identifier if it is an URI and
        // the resolve-dtd-uris feature is turned on
        if (sysId != null && reader.ResolveDtdUris)
          sysId = reader.ResolveSystemId(
                            sysId, RsId.UnparsedEntityDeclError, entNameStr);
        string notationStr = reader.StrTable.Intern(notationName);
        dtdHandler.UnparsedEntityDecl(entNameStr, pubId, sysId, notationStr);
      }
      else {
        IDeclHandler declHandler = reader.DeclHandler;
        if (declHandler == null)
          return;
        if (value != null) {
          string valueStr = new string(value, 0, valueLen);
          declHandler.InternalEntityDecl(entNameStr, valueStr);
        }
        else {
          // must absolutize the system identifier if it is an URI and
          // the resolve-dtd-uris feature is turned on
          if (sysId != null && reader.ResolveDtdUris)
            sysId = reader.ResolveSystemId(
                             sysId, RsId.ExternalEntityDeclError, entNameStr);
          declHandler.ExternalEntityDecl(entNameStr, pubId, sysId);
        }
      }
    }

    /* NotationDeclHandler */

    private static unsafe void
    NotationDeclHandlerImpl(IntPtr userData,
                            char* notationName,
                            char* baseUri,
                            char* systemId,
                            char* publicId)
    {
      Debug.Assert(notationName != null);
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when DtdHandler set
      // if (reader.DtdHandler == null) return;

      // normalize empty strings
      string pubId = null;
      string sysId = null;
      if (publicId != null && *publicId != Constants.NullChar)
        pubId = new string(publicId);
      if (systemId != null && *systemId != Constants.NullChar)
        sysId = new string(systemId);
      string notationStr = reader.StrTable.Intern(notationName);
      // must absolutize the system identifier if it is an URI and
      // the resolve-dtd-uris feature is turned on
      if (sysId != null && reader.ResolveDtdUris)
        sysId = reader.ResolveSystemId(
                             sysId, RsId.NotationDeclError, notationStr);
      reader.DtdHandler.NotationDecl(notationStr, pubId, sysId);
    }

    /* StartNamespaceDeclHandler */

    private static unsafe void
    StartNamespaceDeclHandlerImpl(IntPtr userData, char* prefix, char* uri)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when ContentHandler set
      // if (reader.ContentHandler == null) return;
      string prefixStr = reader.StrTable.Intern(prefix);
      string uriStr = reader.StrTable.Intern(uri);
      reader.ContentHandler.StartPrefixMapping(prefixStr, uriStr);
    }

    /* EndNamespaceDeclHandler */

    private static unsafe void
    EndNamespaceDeclHandlerImpl(IntPtr userData, char* prefix)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when ContentHandler set
      // if (reader.ContentHandler == null) return;
      string prefixStr = reader.StrTable.Intern(prefix);
      reader.ContentHandler.EndPrefixMapping(prefixStr);
    }

    /* NotStandaloneHandler */

    /* Note: This determines if the Expat parser returns an
     * XML_ERROR_NOT_STANDALONE error when it encounters an external subset
     * or a reference to a parameter entity, but does have standalone set
     * to "yes" in an XML declaration.
     */
    private static int
    NotStandaloneHandlerImpl(IntPtr userData)
    {
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      if (reader.StandaloneError)
        return 0;  // this causes the parser to return an error
      else
        return 1;
    }

    /* SkippedEntityHandler */

    private static unsafe void
    SkippedEntityHandlerImpl(IntPtr userData,
                             char* entityName,
                             int isParameterEntity)
    {
      int entLen = (entityName == null) ? 0 : StringUtils.StrLen(entityName);
      Debug.Assert(entLen > 0);
      ExpatReader reader = ((SaxExpatParser)((GCHandle)userData).Target).UserData;
      // not necessary - callback only set when ContentHandler set
      // if (reader.ContentHandler == null) return;

      string entNameStr;
      if (isParameterEntity == 0) {  // general entity
        entNameStr = reader.StrTable.Intern(entityName, entLen);
      }
      else {  // parameter entity
        char[] charBuf = reader.parseData.GetCharBuffer(entLen + 1);
        charBuf[0] = '%';
        Marshal.Copy((IntPtr)entityName, charBuf, 1, entLen);
        entNameStr = reader.StrTable.Intern(charBuf, 0, entLen + 1);
      }
      reader.ContentHandler.SkippedEntity(entNameStr);
    }

    #endregion
  }

  /**<summary>Class for internal use in <see cref="ExpatReader"/>. Groups all
   * entity specific fields that are used during the parsing process.</summary>
   */
  internal class SaxEntityContext: EntityParseContext<SaxEntityContext, SaxExpatParser, ExpatReader>
  {
    private Stream stream;
    private TextReader reader;
    private bool owned;  // indicates if we own the stream or reader

    private string encoding;
    private string publicId;
    private string systemId;
    private Uri baseUri;

    private string name;
    private bool isParameterEntity;
    private string version;

    // source must be IInputSource<Stream> or IInputSource<TextReader>
    internal unsafe void Init(
      string name,
      bool isParameterEntity,
      InputSource source,
      bool isStreamSource,
      bool owned)
    {
      this.owned = owned;
      if (isStreamSource) {
        InputSource<Stream> streamSource = (InputSource<Stream>)source;
        stream = streamSource.Source;
        if (stream == null) {
          string msg = KdsConsts.GetString(KdsRsId.InvalidInputStream);
          throw new SaxException(msg);
        }
      }
      else {
        InputSource<TextReader> readerSource = (InputSource<TextReader>)source;
        reader = readerSource.Source;
        if (reader == null) {
          string msg = KdsConsts.GetString(KdsRsId.InvalidInputReader);
          throw new SaxException(msg);
        }
      }
      this.name = name;
      this.isParameterEntity = isParameterEntity;
      this.encoding = source.Encoding;
      this.publicId = source.PublicId;
      // InputSource.SystemId is guaranteed to be null or an absolute URI
      this.systemId = source.SystemId;
      this.baseUri = (systemId == null) ? null : new Uri(systemId);
    }

    // will be called before Reset()
    protected internal override void Cleanup()
    {
      base.Cleanup();
      if (owned) {
        if (stream != null)
          stream.Close();
        else if (reader != null)
          reader.Close();
      }
    }

    protected override unsafe bool Start(
      char* context,
      char* baseUri,
      char* systemId, char* publicId,
      out ReadBuffer read,
      out string encoding)
    {
      ExpatReader reader = Parser.UserData;
      read = null;
      encoding = null;

      string entName = null;
      string pubId = null;
      string sysId = null;
      InputSource inSource = null;
      // normalize empty strings
      if (publicId != null && *publicId != Constants.NullChar)
        pubId = new string(publicId);
      if (systemId != null && *systemId != Constants.NullChar)
        sysId = new string(systemId);

      bool isParameterEntity = context == null;
      if (!isParameterEntity) {
        entName = ExpatUtils.GetEntityName(context, reader.StrTable);
        if (!reader.ExternalGeneral) {
          if (reader.ContentHandler != null)
            // entName == null should not happen
            reader.ContentHandler.SkippedEntity(entName == null ? String.Empty : entName);
          return false;
        }
      }
      else {
        if (sysId == null) {  // use foreign DTD
          entName = KdsConsts.XmlDtdName;
          if (reader.parseData.HasDoctypeDecl)
            inSource = reader.parseData.ForeignDtdSource;
          else {
            inSource = reader.ResolveForeignDtd(null);
            reader.parseData.ForeignDtdSource = inSource;
          }
          if (inSource == null)
            return false;
        }  // don't have name for parameter entity, must look it up
        else {
          ParEntDeclEntry entry = reader.ParamEntityDecls.GetEntry(pubId, sysId);
          if (entry.Exists)
            entName = reader.ParamEntityDecls.GetName(entry);
        }
        if (entName != null) {
          if (!reader.ExternalParameter) {
            if (reader.ContentHandler != null)
              reader.ContentHandler.SkippedEntity(entName);
            return false;
          }
        }
        else {
          // Expat should not report an entity here if it hasn't read it's
          // declaration, since it would not know it's external id then
          string msg = KdsConsts.GetString(KdsRsId.UnknownEntity);
          msg = String.Format(msg, pubId, sysId);
          reader.CallErrorHandler(ErrorLevel.Error, msg);
          return false;
        }
      }

      // if we got that far, then we don't want to skip the entity
      ILexicalHandler lexHandler = reader.LexicalHandler;
      if (lexHandler != null) {
        // A foreign DTD without DocType declaration would not trigger a
        // StartDocTypeDecl call-back from Expat, so we call StartDtd here.
        if (sysId == null && !reader.parseData.HasDoctypeDecl)
          lexHandler.StartDtd(reader.ForeignDoctypeName,
                              inSource.PublicId,
                              inSource.SystemId);
        lexHandler.StartEntity(entName);
      }

      // input source not yet resolved, except for foreign DTD (sysId == null)
      if (inSource == null) {
        Exception error;
        string msg;
        // we are not using the baseUri parameter - we already have a copy in this instance
        inSource = reader.ResolveExternalEntity(
          entName, pubId, this.BaseUri, sysId, out msg, out error);
        if (inSource == null) {
          if (error == null)
            reader.CallErrorHandler(ErrorLevel.Error, msg);
          else
            reader.CallErrorHandler(ErrorLevel.Error, msg, error);
          return false;
        }
      }

      bool sourceOwned = false;
      InputSource<Stream> streamSource = inSource as InputSource<Stream>;
      if (streamSource != null) {
        read = new StreamBufferReader(streamSource.Source).Read;
        sourceOwned = streamSource is ExpatStreamSource;
      }
      else {
        InputSource<TextReader> readerSource = inSource as InputSource<TextReader>;
        if (readerSource != null)
          read = new TextBufferReader(readerSource.Source).Read;
        else {
          string msg = KdsConsts.GetString(KdsRsId.InvalidInputSource);
          reader.CallErrorHandler(ErrorLevel.Error, msg);
          return false;
        }
      }
      encoding = inSource.Encoding;
      Init(entName, isParameterEntity, inSource, streamSource != null, sourceOwned);
      return true;
    }

    // this is called *after* the parser's EntityParseContext has been popped
    protected override void Finish()
    {
      ExpatReader reader = Parser.UserData;
      try {
        ILexicalHandler lexHandler = reader.LexicalHandler;
        if (lexHandler != null) {
          lexHandler.EndEntity(Name);
          /* a foreign DTD without DocType declaration would not trigger
         * an EndDocTypeDecl call-back from Expat, so we call EndDtd here
         */
          if (reader.parseData.ForeignDtdSource != null) {
            if (reader.parseData.HasDoctypeDecl)
              lexHandler.EndDtd();
            reader.parseData.ForeignDtdSource = null;
          }
        }
      }
      finally {
        base.Finish();
      }
    }

    protected internal override void Reset()
    {
      base.Reset();
      name = null;
      isParameterEntity = false;
      version = null;
      publicId = null;
      systemId = null;
      baseUri = null;
      // stream/reader could be re-used in root context
      // so let's not close or clear them
      if (Parent != null) {
        stream = null;
        reader = null;
      }
    }

    public Stream Stream
    {
      get { return stream; }
    }

    public TextReader Reader
    {
      get { return reader; }
    }

    public string PublicId
    {
      get { return publicId; }
    }

    public string SystemId
    {
      get { return systemId; }
    }

    public string Name
    {
      get { return name; }
    }

    public bool IsParameterEntity
    {
      get { return isParameterEntity; }
    }

    public string Encoding
    {
      get { return encoding; }
      set { encoding = value; }
    }

    public Uri BaseUri
    {
      get { return baseUri; }
      set { baseUri = value; }
    }

    public string Version
    {
      get { return version; }
      set { version = value; }
    }
  }

  /**<summary>Class for internal use in <see cref="ExpatReader"/>.
   * Represents Expat parser.</summary>
   */
  internal class SaxExpatParser: ExpatParser<SaxExpatParser, SaxEntityContext, ExpatReader>
  {
    public SaxExpatParser(string encoding, bool namespaces, ExpatReader userData)
      : base(encoding, namespaces, userData) { }
  }

}

