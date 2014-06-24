/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.CompilerServices;

namespace Kds.Xml.Expat
{
  /// <summary>Delegate for reading data from a pinned managed buffer.</summary>
  /// <remarks> It is the responsibility of the caller to free bufferHandle.</remarks>
  /// <param name="count">Number of bytes to read, that is, size of buffer.</param>
  /// <param name="bufferHandle">Pinned GCHandle for buffer, or "empty" handle
  /// if no data were read.</param>
  /// <returns>Number of bytes actually read. If 0, bufferHandle was not pinned.</returns>
  public delegate int ReadBuffer(int count, out GCHandle bufferHandle);

  /// <summary>Completion status of parsing process.</summary>
  public enum ParseStatus: byte
  {
    Finished,
    Suspended,
    FatalError,
    Aborted
  }

  /**<summary>Base class for exceptions in Expat.</summary> */
  public class ExpatException: ApplicationException
  {
    public ExpatException() { }

    public ExpatException(string message): base(message) { }

    public ExpatException(string message, Exception e): base(message, e) { }
  }

  /**<summary>Exception class for parse errors in Expat.</summary> */
  public class ExpatParseException: ExpatException
  {
    XMLError error;

    public ExpatParseException(XMLError error) {
      this.error = error;
    }

    public ExpatParseException(XMLError error, string message) : base(message) {
      this.error = error;
    }

    public ExpatParseException(XMLError error, string message, Exception e) : base(message, e) {
      this.error = error;
    }

    /// <summary>Expat error code.</summary>
    public XMLError Error {
      get { return error; }
    }

    public override string Message {
      get {
        string msg = base.Message;
        if (!string.IsNullOrEmpty(msg))
          msg += Environment.NewLine;
        msg += "Expat error: " + LibExpat.XMLErrorString(error) + ".";
        return msg;
      }
    }
  }

  /**<summary>Holds entity specific information while the entity
   * is being parsed. Can be subclassed to add more fields.</summary>
   * <remarks>Override the <see cref="Start"/>, <see cref="Finish"/>,
   * <see cref="Cleanup"/> and <see cref="Reset"/> methods accordingly.</remarks>
   */
  public unsafe class EntityParseContext<E, X, U>: CriticalFinalizerObject
    where E: EntityParseContext<E, X, U>, new()
    where X: ExpatParser<X, E, U>
    where U: class
  {
    // volatile, to better avoid access violations!
    private volatile XMLParser* xmlParser = null;
    private ReadBuffer read;

    internal E parent;
    internal X parser;
    internal XMLError error = XMLError.NONE;

    ~EntityParseContext()
    {
      // A finalizer thread and an application thread could be simultaneously
      // active if the object got resurrected!
      lock (this) {
        XMLParser* parser = xmlParser;
        if (parser != null) {
          LibExpat.XMLParserFree(parser);
          xmlParser = null;
        }
      }
    }

    /// <summary>Cleans up unmanaged resources.</summary>
    /// <remarks>Called at end of child entity, after <see cref="Finish"/> and
    /// before <see cref="Reset"/>. Gets called for root entity only when the
    /// <see cref="ExpatParser&lt;X, E, U>">ExpatParser</see> instance that owns
    /// it gets disposed.</remarks>
    protected internal virtual void Cleanup()
    {
      lock (this) {
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          XMLParser* parser = xmlParser;
          if (parser != null) {
            LibExpat.XMLParserFree(parser);
            xmlParser = null;
          }
        }
      }
      GC.SuppressFinalize(this);
    }

    // for use with root entity only
    internal void Init(ReadBuffer read)
    {
      Debug.Assert(read != null);
      this.read = read;
    }

    internal void InitAsParent(string encoding, bool namespaces)
    {
      XMLParser* parser;
      lock (this) {
        if (xmlParser != null)
          throw new ExpatException(ExpatParser<X, E, U>.InternalStateError);
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          if (namespaces)
            parser = LibExpat.XMLParserCreateNS(encoding, ExpatUtils.NSSep);
          else
            parser = LibExpat.XMLParserCreate(encoding);
          xmlParser = parser;
          GC.ReRegisterForFinalize(this);
        }
      }
      if (parser == null)
        throw new OutOfMemoryException();
    }

    internal void InitAsChild(XMLParser* parentParser, char* context, string encoding)
    {
      Debug.Assert(parentParser != null);
      XMLParser* parser;
      lock (this) {
        if (xmlParser != null)
          throw new ExpatException(ExpatParser<X, E, U>.InternalStateError);
        RuntimeHelpers.PrepareConstrainedRegions();
        try { }
        finally {
          parser = LibExpat.XMLExternalEntityParserCreate(parentParser, context, encoding);
          xmlParser = parser;
          GC.ReRegisterForFinalize(this);
        }
      }
      if (parser == null)
        throw new OutOfMemoryException();
    }

    /// <summary>Called when an external entity reference or a request for an external DTD
    /// subset is encountered. Must try to resolve the entity reference or DTD subset request
    /// and initialize the new entity context. Not implemented - must be overridden.</summary>
    /// <remarks>When returning <c>false</c>, then <c>read</c> and <c>encoding</c>
    /// will be set to <c>null</c>.</remarks>
    /// <param name="context">Expat specific parsing context structure.</param>
    /// <param name="baseUri">Base URI. Can be <c>null</c>.</param>
    /// <param name="systemId">System identifier for entity. <c>null</c> for foreign DTD.</param>
    /// <param name="publicId">Public identifier for entity.</param>
    /// <param name="read">Source for the entity's data.</param>
    /// <param name="encoding">Encoding of the entity. Can be <c>null</c>.</param>
    /// <returns><c>false</c>, if the entity should be skipped, <c>true</c> otherwise.</returns>
    protected virtual bool Start(
      char* context,
      char* baseUri,
      char* systemId,
      char* publicId,
      out ReadBuffer read,
      out string encoding)
    {
      read = null;
      encoding = null;
      return false;  // skip entity by default
    }

    /// <summary>The opposite to <see cref="Start"/> - override to perform steps
    /// required at end of external entity.</summary>
    /// <remarks>Called at end of parsing child entity, before <see cref="Cleanup"/> 
    /// and <see cref="Reset"/> are called, never called for root entity.</remarks>
    protected virtual void Finish()
    {
      // No-op by default. Override in derived class
    }

    // for use with (external) child entities only
    internal bool Open(
      char* context, char* baseUri, char* systemId, char* publicId, out string encoding)
    {
      bool result = Start(context, baseUri, systemId, publicId, out read, out encoding);
      Debug.Assert(!result || read != null);
      return result;
    }

    // for use with (external) child entities
    internal void Close()
    {
      Debug.Assert(Parent != null);
      try {
        Finish();
      }
      finally {
        Cleanup();
        Reset();
      }
    }

    /// <summary>Resets state of instance to be used again.</summary>
    /// <remarks>Called at end of parsing entity, after <see cref="Finish"/> and
    /// <see cref="Cleanup"/> are called. Also called for root entity, unlike
    /// <see cref="Finish"/> and <see cref="Cleanup"/>.</remarks>
    protected internal virtual void Reset()
    {
      read = null;
      error = XMLError.NONE;
    }

    internal XMLParser* XmlParser
    {
      get { return xmlParser; }
    }

    public X Parser
    {
      get { return parser; }
    }

    public ReadBuffer Read
    {
      get { return read; }
    }

    public E Parent
    {
      get { return parent; }
    }

    public XMLError Error
    {
      get { return error; }
    }
  }

  /**<summary>Object wrapper for Expat parser instance.</summary>
   * <remarks>Not thread-safe. Accessing an ExpatParser instance from multiple
   * threads can lead to memory access violations and leaks.</remarks>
   */
  public unsafe class ExpatParser<X, E, U>: IDisposable
    where X: ExpatParser<X, E, U>
    where E: EntityParseContext<E, X, U>, new()
    where U: class
  {
    private bool isDisposed = false;
    private bool isDone = false;
    /// <summary>GCHandle passed from unmanaged code to call-back functions,
    /// so that the call-back can access the ExpatParser instance.</summary>
    private GCHandle parserHandle;
    // stack of entity specific EntityParseContext instances, used while parsing
    private E entityContext;
    // linked list of unused EntityParseContext instances - must never be null
    private E freeEntityContext;

    private bool namespaces;
    private string encoding;
    private U userData;
    private Exception error = null;
    private bool skipInternal = false;
    private XMLParamEntityParsing paramEntityParsing = XMLParamEntityParsing.ALWAYS;

    public const string IllegalWhenParsing = "Illegal when parsing.";
    public const string InternalStateError = "Internal parser state error.";

    private E CreateEntityContext()
    {
      E result = new E();
      result.parser = (X)this;
      return result;
    }

    public ExpatParser(string encoding, bool namespaces, U userData)
    {
      freeEntityContext = CreateEntityContext();
      this.encoding = encoding;
      this.namespaces = namespaces;
      this.userData = userData;
      // Use a weak handle since the handle and the target have the same life-cycle
      this.parserHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);
      // we always have a document entity parser instance
      PushDocumentEntityParseContext(encoding, namespaces);
    }

    ~ExpatParser()
    {
      Cleanup();
    }

    private void CheckNotDisposed()
    {
      if (isDisposed)
        throw new ObjectDisposedException(this.GetType().Name);
    }

    private void CheckNotParsing()
    {
      if (Parsing)
        throw new ExpatException(IllegalWhenParsing);
    }

    // this re-assigns all handlers except for the UnknownEncodingHandler
    protected void ReAssignHandlers(XMLParser* parser)
    {
      LibExpat.XMLSetElementDeclHandler(parser, elementDeclHandler);
      LibExpat.XMLSetAttlistDeclHandler(parser, attlistDeclHandler);
      LibExpat.XMLSetXmlDeclHandler(parser, xmlDeclHandler);
      LibExpat.XMLSetElementHandler(parser, startElementHandler, endElementHandler);
      LibExpat.XMLSetCharacterDataHandler(parser, characterDataHandler);
      LibExpat.XMLSetProcessingInstructionHandler(parser, processingInstructionHandler);
      LibExpat.XMLSetCommentHandler(parser, commentHandler);
      LibExpat.XMLSetCdataSectionHandler(parser, startCdataSectionHandler, endCdataSectionHandler);
      if (skipInternal)
        LibExpat.XMLSetDefaultHandler(parser, defaultHandler);
      else
        LibExpat.XMLSetDefaultHandlerExpand(parser, defaultHandler);
      LibExpat.XMLSetDoctypeDeclHandler(parser, startDoctypeDeclHandler, endDoctypeDeclHandler);
      LibExpat.XMLSetEntityDeclHandler(parser, entityDeclHandler);
      LibExpat.XMLSetNotationDeclHandler(parser, notationDeclHandler);
      LibExpat.XMLSetNamespaceDeclHandler(parser, startNamespaceDeclHandler, endNamespaceDeclHandler);
      LibExpat.XMLSetNotStandaloneHandler(parser, notStandaloneHandler);
      LibExpat.XMLSetSkippedEntityHandler(parser, skippedEntityHandler);
      // this is a private handler that always needs to be set
      LibExpat.XMLSetExternalEntityRefHandler(parser, externalEntityRefHandler);
    }

    // this sets all non-null handlers except for the UnknownEncodingHandler
    protected void ReInitializeHandlers(XMLParser* parser)
    {
      if (elementDeclHandler != null)
        LibExpat.XMLSetElementDeclHandler(parser, elementDeclHandler);
      if (attlistDeclHandler != null)
        LibExpat.XMLSetAttlistDeclHandler(parser, attlistDeclHandler);
      if (xmlDeclHandler != null)
        LibExpat.XMLSetXmlDeclHandler(parser, xmlDeclHandler);
      if (startElementHandler != null || endElementHandler != null)
        LibExpat.XMLSetElementHandler(parser, startElementHandler, endElementHandler);
      if (characterDataHandler != null)
        LibExpat.XMLSetCharacterDataHandler(parser, characterDataHandler);
      if (processingInstructionHandler != null)
        LibExpat.XMLSetProcessingInstructionHandler(parser, processingInstructionHandler);
      if (commentHandler != null)
        LibExpat.XMLSetCommentHandler(parser, commentHandler);
      if (startCdataSectionHandler != null || endCdataSectionHandler != null)
        LibExpat.XMLSetCdataSectionHandler(parser, startCdataSectionHandler, endCdataSectionHandler);
      if (defaultHandler != null) {
        if (skipInternal)
          LibExpat.XMLSetDefaultHandler(parser, defaultHandler);
        else
          LibExpat.XMLSetDefaultHandlerExpand(parser, defaultHandler);
      }
      if (startDoctypeDeclHandler != null || endDoctypeDeclHandler != null)
        LibExpat.XMLSetDoctypeDeclHandler(parser, startDoctypeDeclHandler, endDoctypeDeclHandler);
      if (entityDeclHandler != null)
        LibExpat.XMLSetEntityDeclHandler(parser, entityDeclHandler);
      if (notationDeclHandler != null)
        LibExpat.XMLSetNotationDeclHandler(parser, notationDeclHandler);
      if (startNamespaceDeclHandler != null || endNamespaceDeclHandler != null)
        LibExpat.XMLSetNamespaceDeclHandler(parser, startNamespaceDeclHandler, endNamespaceDeclHandler);
      if (notStandaloneHandler != null)
        LibExpat.XMLSetNotStandaloneHandler(parser, notStandaloneHandler);
      if (skippedEntityHandler != null)
        LibExpat.XMLSetSkippedEntityHandler(parser, skippedEntityHandler);
      // this is a private handler that always needs to be set
      LibExpat.XMLSetExternalEntityRefHandler(parser, externalEntityRefHandler);
    }

    private void Reset()
    {
      error = null;
      while (entityContext.Parent != null)
        PopChildEntityParseContext();
      // now reset root entity context
      entityContext.Reset();
    }

    private XMLParser* ResetParser()
    {
      Reset();
      XMLParser* parser = entityContext.XmlParser;
      if (parser != null && LibExpat.XMLParserReset(parser, encoding) == XMLBool.FALSE)
        throw new ExpatException(InternalStateError);
      // all handlers have been cleared (except for unknownEncodingHandler)
      LibExpat.XMLSetUserData(parser, (IntPtr)parserHandle);
      ReInitializeHandlers(parser);
      LibExpat.XMLSetParamEntityParsing(parser, paramEntityParsing);
      return parser;
    }

    public void Reset(string encoding, bool namespaces, bool nsTriplets)
    {
      CheckNotParsing();
      this.encoding = encoding;
      XMLParser* parser;
      if (this.namespaces != namespaces) {
        Reset();
        this.namespaces = namespaces;
        entityContext.Cleanup();
        entityContext.InitAsParent(encoding, namespaces);
        parser = entityContext.XmlParser;
        ConfigureParentParser(parser);
        ReInitializeHandlers(parser);
        LibExpat.XMLSetParamEntityParsing(parser, paramEntityParsing);
      }
      else
        parser = ResetParser();
      LibExpat.XMLSetReturnNSTriplet(parser, nsTriplets ? 1 : 0);
      isDone = false;
    }

    public bool Parsing
    {
      get {
        CheckNotDisposed();
        XMLParsingStatus status;
        LibExpat.XMLGetParsingStatus(entityContext.XmlParser, out status);
        bool parsing = status.Parsing == XMLParsing.PARSING || status.Parsing == XMLParsing.SUSPENDED;
        return !isDone && parsing;
      }
    }

    public U UserData
    {
      get {
        CheckNotDisposed();
        return userData; 
      }
      set {
        CheckNotDisposed();
        userData = value;
      }
    }

    public E EntityContext
    {
      get { return entityContext; }
    }

    public Exception Error
    {
      get { return error; }
    }

    public bool Namespaces
    {
      get { return namespaces; }
    }

    public string Encoding
    {
      get { return encoding; }
    }

    public byte* GetInputContext(ref int offset, ref int size)
    {
      return LibExpat.XMLGetInputContext(entityContext.XmlParser, ref offset, ref size);
    }

    public long CurrentLineNumber
    {
      get { return unchecked((long)LibExpat.XMLGetCurrentLineNumber(entityContext.XmlParser)); }
    }

    public long CurrentColumnNumber
    {
      get { return unchecked((long)LibExpat.XMLGetCurrentColumnNumber(entityContext.XmlParser)); }
    }

    public bool SetParamEntityParsing(XMLParamEntityParsing peParsing)
    {
      bool result = LibExpat.XMLSetParamEntityParsing(entityContext.XmlParser, peParsing) != 0;
      if (result)
        paramEntityParsing = peParsing;
      return result;
    }

    public bool UseForeignDtd
    {
      set {
        LibExpat.XMLUseForeignDTD(entityContext.XmlParser, value ? XMLBool.TRUE : XMLBool.FALSE);
      }
    }

    public bool NSTriplets
    {
      set { LibExpat.XMLSetReturnNSTriplet(entityContext.XmlParser, value ? 1 : 0); }
    }

    public int SpecifiedAttributeCount
    {
      get { return LibExpat.XMLGetSpecifiedAttributeCount(entityContext.XmlParser); }
    }

    public void FreeContentmodel(XMLContent* model)
    {
      LibExpat.XMLFreeContentModel(entityContext.XmlParser, model);
    }

    public XMLParsingStatus ParsingStatus
    {
      get {
        XMLParsingStatus result;
        LibExpat.XMLGetParsingStatus(entityContext.XmlParser, out result);
        return result;
      }
    }

    private void PushEntityParseContext()
    {
      E parentContext = entityContext;
      E newContext = freeEntityContext;
      if (newContext.Parent == null)
        freeEntityContext = CreateEntityContext();  // make sure it is never null
      else
        freeEntityContext = newContext.Parent;
      entityContext = newContext;
      entityContext.parent = parentContext;
    }

    private void ConfigureParentParser(XMLParser* xmlParser)
    {
      if (namespaces)
        LibExpat.XMLSetReturnNSTriplet(xmlParser, 1);
      LibExpat.XMLSetUserData(xmlParser, (IntPtr)parserHandle);
      // this is a private handler that always needs to be set
      LibExpat.XMLSetExternalEntityRefHandler(xmlParser, externalEntityRefHandler);
    }

    // call only once
    private void PushDocumentEntityParseContext(string encoding, bool namespaces)
    {
      PushEntityParseContext();
      entityContext.InitAsParent(encoding, namespaces);
      ConfigureParentParser(entityContext.XmlParser);
    }

    private void PushChildEntityParseContext(
      XMLParser* parentParser, char* context, string encoding)
    {
      PushEntityParseContext();
      try {
        entityContext.InitAsChild(parentParser, context, encoding);
      }
      catch {
        PopChildEntityParseContext();
        throw;
      }
    }

    // Precondition: entityContext != null! Not checked!
    private void PopChildEntityParseContext()
    {
      E parentContext = freeEntityContext;
      freeEntityContext = entityContext;
      entityContext = entityContext.Parent;
      // clean up "popped" instance - after entityContext has changed
      freeEntityContext.parent = parentContext;
      freeEntityContext.Close();
      /* In expat, a child parser (for parsing external references) will
         * inherit all context and handlers from the parent, but will not
         * give changes back to the parent parser. This is a problem when
         * handlers or configuration items have changed. Therefore, we have
         * to track those changes and update the configuration of the parent
         * parser when returning from parsing an external entity.
         */
      ReAssignHandlers(entityContext.XmlParser);
    }

    private void Cleanup()
    {
      if (parserHandle.IsAllocated)
        parserHandle.Free();
    }

    /* IDisposable */

    // do not use instance after calling this
    public void Dispose()
    {
      if (isDisposed)
        return;
      // clean up left-over EntityParseContext instances
      E entCtx = entityContext;
      while (entCtx != null) {
        entCtx.Cleanup();
        entCtx = entCtx.Parent;
      }
      entityContext = entCtx;

      Cleanup();
      // resources cleaned up - no need to have object finalized
      GC.SuppressFinalize(this);
      isDisposed = true;
    }

    /* SkipInternal */

    public bool SkipInternal
    {
      get { return skipInternal; }
    }

    /* ElementDeclHandler */

    private XMLElementDeclHandler elementDeclHandler;

    /// <summary>Handler for element declarations.</summary>
    public XMLElementDeclHandler ElementDeclHandler
    {
      get { return elementDeclHandler; }
      set {
        CheckNotDisposed();
        elementDeclHandler = value;
        LibExpat.XMLSetElementDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* AttlistDeclHandler */

    private XMLAttlistDeclHandler attlistDeclHandler;

    /// <summary>Handler for attribute list declarations.</summary>
    public XMLAttlistDeclHandler AttlistDeclHandler
    {
      get { return attlistDeclHandler; }
      set {
        CheckNotDisposed();
        attlistDeclHandler = value;
        LibExpat.XMLSetAttlistDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* XmlDeclHandler */

    private XMLXmlDeclHandler xmlDeclHandler;

    /// <summary>Handler for XML declarations.</summary>
    public XMLXmlDeclHandler XmlDeclHandler
    {
      get { return xmlDeclHandler; }
      set {
        CheckNotDisposed();
        xmlDeclHandler = value;
        LibExpat.XMLSetXmlDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* StartElementHandler */

    private XMLStartElementHandler startElementHandler;

    /// <summary>Handler for start tag.</summary>
    public XMLStartElementHandler StartElementHandler
    {
      get { return startElementHandler; }
      set {
        CheckNotDisposed();
        startElementHandler = value;
        LibExpat.XMLSetStartElementHandler(entityContext.XmlParser, value);
      }
    }

    /* EndElementHandler */

    private XMLEndElementHandler endElementHandler;

    /// <summary>Handler for end tag.</summary>
    public XMLEndElementHandler EndElementHandler
    {
      get { return endElementHandler; }
      set {
        CheckNotDisposed();
        endElementHandler = value;
        LibExpat.XMLSetEndElementHandler(entityContext.XmlParser, value);
      }
    }

    /* CharacterDataHandler */

    private XMLCharacterDataHandler characterDataHandler;

    /// <summary>Handler for character data.</summary>
    public XMLCharacterDataHandler CharacterDataHandler
    {
      get { return characterDataHandler; }
      set {
        CheckNotDisposed();
        characterDataHandler = value;
        LibExpat.XMLSetCharacterDataHandler(entityContext.XmlParser, value);
      }
    }

    /* ProcessingInstructionHandler */

    private XMLProcessingInstructionHandler processingInstructionHandler;

    /// <summary>Handler for processing instructions.</summary>
    public XMLProcessingInstructionHandler ProcessingInstructionHandler
    {
      get { return processingInstructionHandler; }
      set {
        CheckNotDisposed();
        processingInstructionHandler = value;
        LibExpat.XMLSetProcessingInstructionHandler(entityContext.XmlParser, value);
      }
    }

    /* CommentHandler */

    private XMLCommentHandler commentHandler;

    /// <summary>Handler for comments.</summary>
    public XMLCommentHandler CommentHandler
    {
      get { return commentHandler; }
      set {
        CheckNotDisposed();
        commentHandler = value;
        LibExpat.XMLSetCommentHandler(entityContext.XmlParser, value);
      }
    }

    /* StartCdataSectionHandler */

    private XMLStartCdataSectionHandler startCdataSectionHandler;

    /// <summary>Handler for start of CDATA section.</summary>
    public XMLStartCdataSectionHandler StartCdataSectionHandler
    {
      get { return startCdataSectionHandler; }
      set {
        CheckNotDisposed();
        startCdataSectionHandler = value;
        LibExpat.XMLSetStartCdataSectionHandler(entityContext.XmlParser, value);
      }
    }

    /* EndCdataSectionHandler */

    private XMLEndCdataSectionHandler endCdataSectionHandler;

    /// <summary>Handler for end of CDATA section.</summary>
    public XMLEndCdataSectionHandler EndCdataSectionHandler
    {
      get { return endCdataSectionHandler; }
      set {
        CheckNotDisposed();
        endCdataSectionHandler = value;
        LibExpat.XMLSetEndCdataSectionHandler(entityContext.XmlParser, value);
      }
    }

    /* DefaultHandler */

    private XMLDefaultHandler defaultHandler;

    /// <summary>Handler for unhandled events.</summary>
    public XMLDefaultHandler DefaultHandler
    {
      get { return defaultHandler; }
    }

    /// <summary>Handler for unhandled events.</summary>
    /// <remarks>Will optionally skip internal entities.</remarks>
    public void SetDefaultHandler(XMLDefaultHandler value, bool skipInternal)
    {
      CheckNotDisposed();
      defaultHandler = value;
      this.skipInternal = skipInternal;
      if (skipInternal)
        LibExpat.XMLSetDefaultHandler(entityContext.XmlParser, value);
      else
        LibExpat.XMLSetDefaultHandlerExpand(entityContext.XmlParser, value);
    }

    /* StartDoctypeDeclHandler */

    private XMLStartDoctypeDeclHandler startDoctypeDeclHandler;

    /// <summary>Handler for start of Doctype declaration.</summary>
    public XMLStartDoctypeDeclHandler StartDoctypeDeclHandler
    {
      get { return startDoctypeDeclHandler; }
      set {
        CheckNotDisposed();
        startDoctypeDeclHandler = value;
        LibExpat.XMLSetStartDoctypeDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* EndDoctypeDeclHandler */

    private XMLEndDoctypeDeclHandler endDoctypeDeclHandler;

    /// <summary>Handler for end of Doctype declaration.</summary>
    public XMLEndDoctypeDeclHandler EndDoctypeDeclHandler
    {
      get { return endDoctypeDeclHandler; }
      set {
        CheckNotDisposed();
        endDoctypeDeclHandler = value;
        LibExpat.XMLSetEndDoctypeDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* EntityDeclHandler */

    private XMLEntityDeclHandler entityDeclHandler;

    /// <summary>Handler for entity declarations.</summary>
    public XMLEntityDeclHandler EntityDeclHandler
    {
      get { return entityDeclHandler; }
      set {
        CheckNotDisposed();
        entityDeclHandler = value;
        LibExpat.XMLSetEntityDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* NotationDeclHandler */

    private XMLNotationDeclHandler notationDeclHandler;

    /// <summary>Handler for notation declarations.</summary>
    public XMLNotationDeclHandler NotationDeclHandler
    {
      get { return notationDeclHandler; }
      set {
        CheckNotDisposed();
        notationDeclHandler = value;
        LibExpat.XMLSetNotationDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* StartNamespaceDeclHandler */

    private XMLStartNamespaceDeclHandler startNamespaceDeclHandler;

    /// <summary>Handler called when namespace scope starts.</summary>
    public XMLStartNamespaceDeclHandler StartNamespaceDeclHandler
    {
      get { return startNamespaceDeclHandler; }
      set {
        CheckNotDisposed();
        startNamespaceDeclHandler = value;
        LibExpat.XMLSetStartNamespaceDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* EndNamespaceDeclHandler */

    private XMLEndNamespaceDeclHandler endNamespaceDeclHandler;

    /// <summary>Handler called when namespace scope ends.</summary>
    public XMLEndNamespaceDeclHandler EndNamespaceDeclHandler
    {
      get { return endNamespaceDeclHandler; }
      set {
        CheckNotDisposed();
        endNamespaceDeclHandler = value;
        LibExpat.XMLSetEndNamespaceDeclHandler(entityContext.XmlParser, value);
      }
    }

    /* NotStandaloneHandler */

    private XMLNotStandaloneHandler notStandaloneHandler;

    /// <summary>Handler called when document is not "standalone".</summary>
    /// <remarks>If this handler returns XMLStatus.ERROR, then
    /// the parser will return an XMLError.NOT_STANDALONE error.</remarks>
    public XMLNotStandaloneHandler NotStandaloneHandler
    {
      get { return notStandaloneHandler; }
      set {
        CheckNotDisposed();
        notStandaloneHandler = value;
        LibExpat.XMLSetNotStandaloneHandler(entityContext.XmlParser, value);
      }
    }

    /* SkippedEntityHandler */

    private XMLSkippedEntityHandler skippedEntityHandler;

    /// <summary>Handler called when an entity reference needed to be skipped.</summary>
    public XMLSkippedEntityHandler SkippedEntityHandler
    {
      get { return skippedEntityHandler; }
      set {
        CheckNotDisposed();
        skippedEntityHandler = value;
        LibExpat.XMLSetSkippedEntityHandler(entityContext.XmlParser, value);
      }
    }

    /// <summary>This references keeps the ExternalEntityRefHandlerImpl delegate from
    /// being garbage collected.</summary>
    private XMLExternalEntityRefHandler externalEntityRefHandler = ExternalEntityRefHandlerImpl;

    /// <summary>Handler called by Expat when an external entity needs to be parsed.</summary>
    /// <remarks>If entities are nested, this handler may be called recursively.</remarks>
    private static int
    ExternalEntityRefHandlerImpl(XMLParser* parser,
                                 char* context,
                                 char* baseUri,
                                 char* systemId,
                                 char* publicId)
    {
      int result = (int)XMLStatus.OK;

      GCHandle parserHandle = (GCHandle)LibExpat.XMLGetUserData(parser);
      X expatParser = (X)parserHandle.Target;

      E newEntityContext = expatParser.freeEntityContext;  // should always be != null
      string encoding;
      try {
        if (!newEntityContext.Open(context, baseUri, systemId, publicId, out encoding))
          return result;
      }
      catch (Exception e) {
        expatParser.error = e;
        result = (int)XMLStatus.ERROR;
        return result;
      }

      expatParser.PushChildEntityParseContext(parser, context, encoding);

      ParseStatus status = ExpatUtils.Parse(
        newEntityContext.XmlParser, newEntityContext.Read, out newEntityContext.error);

      switch (status) {
        case ParseStatus.Finished:
          expatParser.PopChildEntityParseContext();
          break;
        case ParseStatus.Suspended:
          // must suspend parent parser as well - don't check return value
          LibExpat.XMLStopParser(parser, XMLBool.TRUE);
          break;
        case ParseStatus.FatalError:
          result = (int)XMLStatus.ERROR;
          break;
        case ParseStatus.Aborted:
          // must abort parent parser as well - don't check return value
          LibExpat.XMLStopParser(parser, XMLBool.FALSE);
          break;
      }
      return result;
    }

    /* UnknownEncodingHandler */

    private XMLUnknownEncodingHandler unknownEncodingHandler;

    public void SetUnknownEncodingHandler(XMLUnknownEncodingHandler handler,
                                          IntPtr encodingHandlerData)
    {
      CheckNotParsing();
      unknownEncodingHandler = handler;
      LibExpat.XMLSetUnknownEncodingHandler(
        entityContext.XmlParser, handler, encodingHandlerData);
    }

    /* Main Parse method */

    public ParseStatus Parse(ReadBuffer read)
    {
      CheckNotParsing();
      XMLParser* parser;
      if (isDone) {
        parser = ResetParser();
        isDone = false;
      }
      else 
        parser = entityContext.XmlParser;
      entityContext.Init(read);
      ParseStatus status;
      try {
        status = ExpatUtils.Parse(parser, read, out entityContext.error);
        isDone = status != ParseStatus.Suspended;
      }
      catch {
        isDone = true;
        throw;
      }
      return status;
    }

    /* Resumable features */

#if EXPAT_1_95_8_UP
    public bool Suspend()
    {
      CheckNotDisposed();
      bool result;
      XMLParser* parser = entityContext.XmlParser;
      result = LibExpat.XMLStopParser(parser, XMLBool.TRUE) == XMLStatus.OK;
      if (result)
        entityContext.error = XMLError.NONE;
      else
        entityContext.error = LibExpat.XMLGetErrorCode(parser);
      return result;
    }

    public bool Abort()
    {
      CheckNotDisposed();
      bool result;
      XMLParser* parser = entityContext.XmlParser;
      // no cleanup here, as we are in the middle of a call-back
      result = LibExpat.XMLStopParser(parser, XMLBool.FALSE) == XMLStatus.OK;
      if (result)
        entityContext.error = XMLError.NONE;
      else
        entityContext.error = LibExpat.XMLGetErrorCode(parser);
      return result;
    }

    public ParseStatus Resume()
    {
      CheckNotDisposed();
      ParseStatus status;
      for (; ; ) {
        XMLParser* parser = entityContext.XmlParser;
        try {
          status = ExpatUtils.ResumeParsing(parser, entityContext.Read, out entityContext.error);
          isDone = status != ParseStatus.Suspended;
        }
        catch {
          isDone = true;
          throw;
        }
        if (status == ParseStatus.Finished && entityContext.Parent != null)
          PopChildEntityParseContext();  // return to parsing parent entity
        else
          break;
      }
      return status;
    }
#endif
  }

}