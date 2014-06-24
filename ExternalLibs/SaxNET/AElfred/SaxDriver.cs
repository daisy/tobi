using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using Org.System.Xml.Sax;
using Org.System.Xml.Sax.Helpers;

/*** License **********************************************************
*
* Copyright (c) 2005, Jeff Rafter
*
* This file is part of the AElfred C# library. AElfred C# was converted 
* from the AElfred2 library for Java written by David Brownell (from
* the March 2002 version). Because his changes were released under 
* the GPL with Library Exception license, this file inherits that 
* license because it is a derived work. 
*
* Many enhancements for XML conformance and SAX conformance were added 
* in the conversion. All changes can be utilized under the a public
* domain license.
*/

/*** Original GNU JAXP License ****************************************
*
* This file is part of GNU JAXP, a library.
*
* GNU JAXP is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
* 
* GNU JAXP is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  
* USA
*
* As a special exception, if you link this library with other files to
* produce an executable, this library does not by itself cause the
* resulting executable to be covered by the GNU General Public License.
* This exception does not however invalidate any other reasons why the
* executable file might be covered by the GNU General Public License. 
*/

/*** Original Microstar License ****************************************
*
* Copyright (c) 1997, 1998 by Microstar Software Ltd.
* From Microstar's README (the entire original license):
*
* Separate statements also said it's in the public domain.
* All modifications are distributed under the license
* above (GPL with library exception).
*
* AElfred is free for both commercial and non-commercial use and
* redistribution, provided that Microstar's copyright and disclaimer are
* retained intact.  You are free to modify AElfred for your own use and
* to redistribute AElfred with your modifications, provided that the
* modifications are clearly documented.
*
* This program is distributed in the hope that it will be useful, but
* WITHOUT ANY WARRANTY; without even the implied warranty of
* merchantability or fitness for a particular purpose.  Please use it AT
* YOUR OWN RISK.
*/

namespace AElfred 
{  
  
  /*
  * An enhanced Sax2 version of Microstar's &AElig;lfred Xml parser.
  * The enhancements primarily relate to significant improvements in
  * conformance to the Xml specification, and Sax2 support.  Performance
  * has been improved.  See the package level documentation for more
  * information.
  *
  * <table border="1" width='100%' cellpadding='3' cellspacing='0'>
  * <tr bgcolor='#ccccff'>
  *	<th><font size='+1'>Name</font></th>
  *	<th><font size='+1'>Notes</font></th></tr>
  *
  * <tr><td colspan=2><center><em>Features ... URL prefix is
  * <b>http://xml.org/sax/features/</b></em></center></td></tr>
  *
  * <tr><td>(URL)/external-general-entities</td>
  *	<td>Value defaults to <em>true</em></td></tr>
  * <tr><td>(URL)/external-parameter-entities</td>
  *	<td>Value defaults to <em>true</em></td></tr>
  * <tr><td>(URL)/is-standalone</td>
  *	<td>(PRELIMINARY) Returns true iff the document's parsing
  *	has started(some non-error event after <em>startDocument()</em>
  *	was reported) and the document's standalone flag is set.</td></tr>
  * <tr><td>(URL)/namespace-prefixes</td>
  *	<td>Value defaults to <em>false</em>(but Xml 1.0 names are
  *		always reported)</td></tr>
  * <tr><td>(URL)/lexical-handler/parameter-entities</td>
  *	<td>Value is fixed at <em>true</em></td></tr>
  * <tr><td>(URL)/namespaces</td>
  *	<td>Value defaults to <em>true</em></td></tr>
  * <tr><td>(URL)/resolve-dtd-uris</td>
  *	<td>Value defaults to <em>true</em></td></tr>
  * <tr><td>(URL)/string-interning</td>
  *	<td>Value is fixed at <em>true</em></td></tr>
  * <tr><td>(URL)/validation</td>
  *	<td>Value is fixed at <em>false</em></td></tr>
  *
  * <tr><td colspan=2><center><em>Handler Properties ... URL prefix is
  * <b>http://xml.org/sax/properties/</b></em></center></td></tr>
  *
  * <tr><td>(URL)/declaration-handler</td>
  *	<td>A declaration handler may be provided.  </td></tr>
  * <tr><td>(URL)/lexical-handler</td>
  *	<td>A lexical handler may be provided.  </td></tr>
  * </table>
  *
  * <p>This parser currently implements the Sax1 Parser API, but
  * it may not continue to do so in the future.
  *
  * @author Written by David Megginson(version 1.2a from Microstar)
  * @author Updated by David Brownell &lt;dbrownell@users.sourceforge.net&gt;
  * @author Converted to C# by Jeff Rafter
  * @see XmlParser
  */
  public sealed class SaxDriver : ILocator, IAttributes, IXmlReader {

    /*
       * Internal class for maintaining interal array of attribute data */
    private class AttributeData {
      public bool Declaration;
      public bool Specified;
      public bool Declared;
      public long LineNumber;
      public long ColumnNumber;
      public long ValueLineNumber;
      public long ValueColumnNumber;
      public string QName;
      public string Uri;
      public string LocalName;
      public string Prefix;
      public string Value;
    }

    private static AElfredDefaultHandler handlerBase = new AElfredDefaultHandler();
    private XmlParser parser;

    private XmlReaderStatus status = XmlReaderStatus.Ready;
    private IEntityResolver entityResolver = handlerBase;
    private IContentHandler contentHandler = handlerBase;
    private IDtdHandler dtdHandler = handlerBase;
    private IErrorHandler errorHandler = handlerBase;
    private IDeclHandler declHandler = handlerBase;
    private ILexicalHandler lexicalHandler = handlerBase;
    private IContentModelHandler cmHandler = handlerBase;

    private string elementName = null;
    private Stack entityStack = new Stack();

    // could use just one vector(of object/struct): faster, smaller
    private AttributeData[] attributeData = new AttributeData[10];

    internal bool namespaces = true;
    internal bool xmlNames = false;
    internal bool xmlnsUris = false;
    internal bool extGE = true;
    internal bool extPE = true;
    internal bool resolveAll = true;

    private int attributeCount = 0;
    private bool attributes;
    private string[] nsTemp = new string [3];
    private NamespaceSupport namespaceSupport;

    //
    // Constructor.
    //

    /* Constructs a Sax Parser.  */
    public SaxDriver() { }

    public void Abort() 
    {
      throw new NotImplementedException();
    }

    public void Suspend() 
    {
      throw new NotImplementedException();
    }

    public void Resume() 
    {
      throw new NotImplementedException();
    }

    public XmlReaderStatus Status 
    {
      get 
      {
        return status;
      }
    }
    
    public IEntityResolver EntityResolver 
    {
      /*
       * <b>Sax2</b>: Returns the object used when resolving external
       * entities during parsing(both general and parameter entities).
       */
      get {
        return(entityResolver == handlerBase) ? null : entityResolver;
      }
      /*
       * <b>Sax1, Sax2</b>: Set the entity resolver for this parser.
       * @param handler The object to receive entity events.
       */
      set {
        if(value == null)
          value = handlerBase;
        entityResolver = value;
      }
    }

    public IDtdHandler DtdHandler {
      /*
       * <b>Sax2</b>: Returns the object used to process declarations related
       * to notations and unparsed entities.
       */
      get {
        return(dtdHandler == handlerBase) ? null : dtdHandler;
      }
      /*
       * <b>Sax1, Sax2</b>: Set the Dtd handler for this parser.
       * @param handler The object to receive Dtd events.
       */
      set {
        if(value == null)
          value = handlerBase;
        this.dtdHandler = value;
      }
    }

    public IContentHandler ContentHandler {
      /*
       * <b>Sax2</b>: Returns the object used to report the logical
       * content of an Xml document.
       */
      get {
        return contentHandler == handlerBase ? null : contentHandler;
      }
      /*
       * <b>Sax2</b>: Assigns the object used to report the logical
       * content of an Xml document.  If a document handler was set,
       * this content handler will supplant it(but Xml 1.0 style name
       * reporting may remain enabled).
       */
      set {
        if(value == null)
          value = handlerBase;
        contentHandler = value;
      }
    }

    public IErrorHandler ErrorHandler {
      /*
       * <b>Sax2</b>: Returns the object used to receive callbacks for Xml
       * errors of all levels(fatal, nonfatal, warning); this is never null;
       */
      get {
        return errorHandler == handlerBase ? null : errorHandler;       
      }
      /*
       * <b>Sax1, Sax2</b>: Set the error handler for this parser.
       * @param handler The object to receive error events.
       */
      set {
        if(value == null)
          value = handlerBase;
        this.errorHandler = value;
      }
    }

    public ILexicalHandler LexicalHandler 
    {
      get 
      {
        return lexicalHandler == handlerBase ? null : lexicalHandler;
      }
      set 
      {
        if(value == null)
          value = handlerBase;
        lexicalHandler = value;
      }
    }

    public IDeclHandler DeclHandler 
    {
      get 
      {
        return declHandler == handlerBase ? null : declHandler;
      }
      set 
      {
        if(value == null)
          value = handlerBase;
        declHandler = value;
      }
    }

    public IContentModelHandler ContentModelHandler 
    {
      get 
      {
        return(cmHandler == handlerBase) ? null : cmHandler;
      }
      set 
      {
        if(value == null)
          value = handlerBase;
        this.cmHandler = value;
      }
    }

    /*
     * <b>Sax1, Sax2</b>: Auxiliary API to parse an Xml document, used mostly
     * when no URI is available.
     * If you want anything useful to happen, you should set
     * at least one type of handler.
     * @param source The Xml input source.  Don't set 'encoding' unless
     *  you know for a fact that it's correct.
     * @see #setEntityResolver
     * @see #setDtdHandler
     * @see #setContentHandler
     * @see #setErrorHandler
     * @exception SaxException The handlers may throw any SaxException,
     *  and the parser normally throws SaxParseException objects.
     * @exception IOException IOExceptions are normally through through
     *  the parser if there are problems reading the source document.
     */
    public void Parse(InputSource source) 
    {
      //FIXME!! synchronized(base) {
      parser = new XmlParser();
      if(namespaces) 
      {
        namespaceSupport = new NamespaceSupport();
        namespaceSupport.NamespaceDeclUris = true;
      }
      parser.setHandler(this);

      try {
        // .NET 
        status = XmlReaderStatus.Parsing;
        Stream stream = null;
        TextReader reader = null;
        if(source is InputSource<Stream>)
          stream = ((InputSource<Stream>)source).Source;
        if (source is InputSource<TextReader>)
          reader = ((InputSource<TextReader>)source).Source;
        if (source.SystemId == null && stream != null) {
          if (stream is FileStream)
            source.SystemId = ((FileStream)stream).Name;
        }
         parser.doParse(source.SystemId,
          source.PublicId,
          reader,
          stream,
          source.Encoding);
      } catch(SaxException e) {
        throw e;
      } catch(Exception e) {
        throw new SaxParseException(new ParseErrorImpl(e.Message, this, e));
      } finally {
        contentHandler.EndDocument();
        entityStack.Clear();
        parser = null;
        namespaceSupport = null;
        status = XmlReaderStatus.Ready;
      }
    }


    /*
     * <b>Sax1, Sax2</b>: Preferred API to parse an Xml document, using a
     * system identifier(URI).
     */
    public void Parse(string systemId) {
      Parse(new InputSource(systemId));
    }

    //
    // Implementation of Sax2 "XmlReader" interface
    //
    public const string FEATURE = "http://xml.org/sax/features/";
    public const string PROPERTY = "http://xml.org/sax/properties/";

    /*
     * <b>Sax2</b>: Tells the value of the specified feature flag.
     *
     * @exception SaxNotRecognizedException thrown if the feature flag
     *  is neither built in, nor yet assigned.
     */
    public bool GetFeature(string featureId) {
      if((FEATURE + "validation").Equals(featureId))
        return false;

      // external entities(both types) are optionally included
      if((FEATURE + "external-general-entities").Equals(featureId))
        return extGE;
      if((FEATURE + "external-parameter-entities") .Equals(featureId))
        return extPE;

      // element/attribute names are as written in document; no mangling
      if((FEATURE + "namespace-prefixes").Equals(featureId))
        return xmlNames;

      // report element/attribute namespaces?
      if((FEATURE + "namespaces").Equals(featureId))
        return namespaces;

      // report xmlns attributes as being in http://www.w3.org/2000/xmlns
      if((FEATURE + "xmlns-uris").Equals(featureId))
        return xmlnsUris;

      // all PEs and GEs are reported
      if((FEATURE + "lexical-handler/parameter-entities").Equals(featureId))
        return true;

      // always interns
      if((FEATURE + "string-interning").Equals(featureId))
        return true;

      // EXTENSIONS 1.1

      // optionally don't absolutize URIs in declarations
      if((FEATURE + "resolve-dtd-uris").Equals(featureId))
        return resolveAll;

      throw new NotSupportedException(featureId);
    }

    // package private
    internal IDeclHandler getDeclHandler() { return declHandler; }

    // package private
    internal bool resolveURIs() { return resolveAll; }

    /*
     * <b>Sax2</b>:  Returns the specified property.
     *
     * @exception SaxNotRecognizedException thrown if the property value
     *  is neither built in, nor yet stored.
     */
    public IProperty GetProperty(string propertyId) {
      // unknown properties
      throw new NotSupportedException(propertyId);
    }

    public IProperty<T> GetProperty<T>(string propertyId) {
      // unknown properties
      throw new NotSupportedException(propertyId);
    }

    /*
     * <b>Sax2</b>:  Sets the state of feature flags in this parser.  Some
     * built-in feature flags are mutable.
     */
    public void SetFeature(string featureId, bool value) {
      bool  state;

      // Features with a defined value, we just change it if we can.
      state = GetFeature(featureId);

      if(state == value)
        return;
      if(parser != null)
        throw new NotSupportedException("not while parsing");

      if((FEATURE + "namespace-prefixes").Equals(featureId)) {
        // in this implementation, this only affects xmlns reporting
        xmlNames = value;
        // .NET removed forcibly prevent illegal parser state
        // .NET removed if(!xmlNames)
        // .NET removed   namespaces = true;
        return;
      }

      if((FEATURE + "namespaces").Equals(featureId)) {
        namespaces = value;
        // .NET removed forcibly prevent illegal parser state
        // .NET removed if(!namespaces)
        // .NET removed   xmlNames = true;
        return;
      }

      if((FEATURE + "xmlns-uris").Equals(featureId)) {
        // treat xmlns attributes as being in the NS http://www.w3.org/2000/xmlns/ 
        // namespace-prefixes must be set
        xmlnsUris = true;
        return;
      }

      if((FEATURE + "external-general-entities").Equals(featureId)) {
        extGE = value;
        return;
      }
      if((FEATURE + "external-parameter-entities") .Equals(featureId)) {
        extPE = value;
        return;
      }
      if((FEATURE + "resolve-dtd-uris").Equals(featureId)) {
        resolveAll = value;
        return;
      }

      throw new NotSupportedException(featureId);
    }

    //
    // This is where the driver receives XmlParser callbacks and translates
    // them into Sax callbacks.  Some more callbacks have been added for
    // Sax2 support.
    //

    internal void startDocument() {
      contentHandler.SetDocumentLocator(this);
      contentHandler.StartDocument();      
      if (namespaces) 
      {
        namespaceSupport.Reset();
        namespaceSupport.PushContext();
      }
      attributeCount = 0;
      attributes = false;
      for (int i = attributeCount; i < attributeData.Length; i++) {
        attributeData[i] = new AttributeData();
      }
    }

    internal void skippedEntity(string name) { 
      contentHandler.SkippedEntity(name); 
    }

    internal InputSource getExternalSubset(string name, string baseURI) {
      if(!extPE)
        return null;
      return entityResolver.GetExternalSubset(name, baseURI);
    }

    internal InputSource resolveEntity(bool isPE, string name,
      InputSource inp, string baseURI) {
      InputSource  source;

      // external entities might be skipped
      if(isPE && !extPE)
        return null;
      if(!isPE && !extGE)
        return null;

      // ... or not
      lexicalHandler.StartEntity(name);
      source = entityResolver.ResolveEntity(name, inp.PublicId,
        baseURI, inp.SystemId);
      if(source == null) {
        inp.SystemId = absolutize(baseURI, inp.SystemId, false);
        source = inp;
      }
      startExternalEntity(name, source.SystemId, true);
      return source;
    }

    // absolutize a system ID relative to the specified base URI
    //(temporarily) package-visible for external entity decls
    internal string absolutize(string baseURI, string systemId, bool nice) {
      // FIXME normalize system IDs -- when?
      // - Convert to UTF-8
      // - Map reserved and non-ASCII characters to %HH
      if (systemId == null) 
        return null;

      try {
        if (systemId.IndexOf(':') != -1)
          return new Uri(systemId).ToString();
      } catch(UriFormatException) { }

      try {
        if(baseURI == null) {
          warn("No base URI; SYSTEM id must be absolute: " + systemId);
          return systemId;
        } else
          return new Uri( new Uri(baseURI), systemId).ToString();
      } catch(UriFormatException e) {

        // Let unknown URI schemes pass through unless we need
        // the JVM to map them to i/o streams for us...
        if(!nice)
          throw e;

        // sometimes sysids for notations or unparsed entities
        // aren't really URIs...
        warn("Can't absolutize SYSTEM id: " + e.Message);
        return systemId;
      }
    }

    internal void startExternalEntity(string name, string systemId,
      bool stackOnly) {
      if(systemId == null)
        warn("URI was not reported to parser for entity " + name);
      if(!stackOnly)    // spliced [dtd] needs startEntity
        lexicalHandler.StartEntity(name);
      entityStack.Push(systemId);
    }

    internal void endExternalEntity(string name) {
      if(!"[document]".Equals(name))
        lexicalHandler.EndEntity(name);
      entityStack.Pop();
    }

    internal void startInternalEntity(string name) {
      lexicalHandler.StartEntity(name);
    }

    internal void endInternalEntity(string name) {
      lexicalHandler.EndEntity(name);
    }

    internal void doctypeDecl(string name, string publicId, string systemId) {
      lexicalHandler.StartDtd(name, publicId, systemId);

      // ... the "name" is a declaration and should be given
      // to the DeclHandler(but sax2 doesn't).

      // the IDs for the external subset are lexical details,
      // as are the contents of the internal subset; but sax2
      // doesn't provide the internal subset "pre-parse"
    }

    internal void notationDecl(string name, string[] ids) {
      try {
        dtdHandler.NotationDecl(name, ids [0],
         (resolveAll && ids [1] != null)
          ? absolutize(ids [2], ids [1], true)
          : ids [1]);
      } catch(Exception e) {
        // "can't happen"
        throw new SaxParseException(new ParseErrorImpl(e.Message, this, e));
      }
    }

    internal void unparsedEntityDecl(string name, string[] ids, string notation) {
      try {
        dtdHandler.UnparsedEntityDecl(name, ids [0],
          resolveAll
          ? absolutize(ids [2], ids [1], true)
          : ids [1],
          notation);
      } catch(Exception e) {
        // "can't happen"
        throw new SaxParseException(new ParseErrorImpl(e.Message, this, e));
      }
    }

    internal void endDoctype() {
      lexicalHandler.EndDtd();
    }

    internal void pushContext() 
    {
      if (namespaces)
        namespaceSupport.PushContext();
    }

    internal void popContext() 
    {
      if (namespaces)
        namespaceSupport.PopContext();
    }

    private void processElement(string qName, out string localName, out string uri) 
    {
      int index = qName.IndexOf(':');
      string prefix = string.Empty;
      localName = string.Empty;
      uri = string.Empty;
      if (index == 0) {
        fatal("empty prefix used in: " + qName);
        return;
      }
      if (index == qName.Length-1) 
      {
        fatal("empty local name used in: " + qName);
        return;
      }
      if(index != -1) 
      {
        prefix = string.Intern(qName.Substring(0, index));
        localName = qName.Substring(index+1);
        uri = namespaceSupport.GetURI(prefix);
        if (uri == null) 
        {
          fatal("undeclared element prefix in: " + qName);
          return;
        }
      } 
      else if(prefix == string.Empty) 
      {  
        // Default namespace
        localName = qName;
        uri = namespaceSupport.GetURI(string.Empty);
      }
    }

    private void processAttribute(string qName, out string localName, out string uri) {
      int index = qName.IndexOf(':');
      string prefix = string.Empty;
      localName = string.Empty;
      uri = string.Empty;
      if (index == qName.Length-1) 
      {
        fatal("empty local name used in: " + qName);
        return;
      }
      if(index != -1) 
      {
        prefix = string.Intern(qName.Substring(0, index));
        localName = qName.Substring(index+1);
      }
      if(prefix == string.Empty) {  // no default namepspace for attributes
        uri = string.Empty;
        localName = qName;
        return;
      }
      uri = namespaceSupport.GetURI(prefix);
      if (uri == null) 
      {
        fatal("undeclared attribute prefix in: " + qName);
        return;
      }
    }

    private void declarePrefix(string prefix, string uri) {
      int index = uri.IndexOf(':');

      // many versions of nwalsh docbook stylesheets
      // have bogus URLs; so this can't be an error...
      if(index < 1 && uri.Length != 0)
        warn("relative URI for namespace: " + uri);

      // FIXME:  char [0] must be ascii alpha; chars [1..index]
      // must be ascii alphanumeric or in "+-." [RFC 2396]
      uri = string.Intern(uri);
      if (namespaceSupport.DeclarePrefix(prefix, uri))
        contentHandler.StartPrefixMapping(prefix, uri);    
    }

    internal void attribute(string qname, string value, bool isSpecified, bool isDeclared,
      long attLine, long attCol, long attValLine, long attValCol) {
      if(!attributes) {
        attributes = true;
      }

      // Prep for the incompatible change in NS 1.0 erratum NE05
      string uri = string.Empty;
      string prefix = string.Empty;
      bool isXmlNS = false;

      // process namespace decls immediately;
      // then maybe forget this as an attribute
      if(namespaces) {
        // default NS declaration?
        if(qname.StartsWith("xmlns")) {
          int len = qname.Length;
          if (len == 5) {
            declarePrefix(string.Empty, value);
            if(!xmlNames)
              return;
          } else if (len > 6 && qname[5] == ':') {
            // FIXME NS Rec 1.1 does not have this constraint, prefixed ns decls can be reset to ""
            if(value.Length == 0) {
              fatal("missing URI in namespace decl attribute: " + qname);
            } else {
              prefix = string.Intern(qname.Substring(6));
              
              // Check prefix/namespace bindings
              if (value == NamespaceSupport.XMLNS) 
              {
                if (prefix != "xml")
                  fatal ("cannot bind the namespace \"" + NamespaceSupport.XMLNS + "\" to prefix " + prefix);
              }
              else if (prefix == "xml")
                fatal ("reserved prefix \"xml\" bound to namespace " + value);
              else if (prefix == "xmlns")
                fatal ("reserved prefix \"xmlns\" cannot be declared");
              else if (value == NamespaceSupport.NSDECL) 
                fatal ("cannot bind the namespace \"" + NamespaceSupport.NSDECL + "\" to prefix " + prefix);


              declarePrefix(prefix, value);
              if(xmlnsUris)
                uri = NamespaceSupport.NSDECL;
            }
            if(!xmlNames)
              return;
          } else if (len > 5)
            fatal ("invalid namespace declaration: " + qname);
          isXmlNS = true;
        }

      }

      // remember this attribute ...
      if(attributeCount == attributeData.Length) {   // grow array?
        AttributeData[] temp = new AttributeData[attributeData.Length+5];
        System.Array.Copy(attributeData, 0, temp, 0, attributeCount);
        attributeData = temp;
        for (int i = attributeCount; i < attributeData.Length; i++) {
          attributeData[i] = new AttributeData();
        }
      }
      
      AttributeData att = attributeData [attributeCount];
      att.LineNumber = attLine;
      att.ColumnNumber = attCol;
      att.ValueLineNumber = attValLine;
      att.ValueColumnNumber = attValCol;
      att.Declared = isDeclared;
      att.Specified = isSpecified;
      att.QName = qname;
      // attribute type comes from querying parser's Dtd records
      att.Value = value;
      // ... patching {lname, uri} later, if needed(here we handle xmlns, so we don't do it twice)
      if(isXmlNS) {
        att.Declaration = true;
        att.Uri = uri;
        att.LocalName = prefix;
        att.Prefix = "xmlns";
      } else {
        att.Declaration = false;
        att.Uri = string.Empty;
        att.LocalName = string.Empty;
        att.Prefix = "";
      }
      attributeCount++;
    }

    internal void startElement(string elname) {
      IContentHandler handler = contentHandler;

      string localName = string.Empty;
      string ns = string.Empty;

      if(!attributes) {

      } else if(namespaces) {

        // now we can patch up namespace refs; we saw all the
        // declarations, so now we'll do the Right Thing
        for(int i = 0; i < attributeCount; i++) {
          AttributeData att = attributeData[i];

          // NS prefix declaration?
          if(att.Declaration && !xmlNames) 
            continue;

          // it's not a NS decl; patch namespace info items
          processAttribute(att.QName, out localName, out ns); 
          att.Uri = ns;
          att.LocalName = localName;

          for (int j = 0; j < i; j++)
            if (att.Uri == attributeData[j].Uri &&
              att.LocalName == attributeData[j].LocalName ) 
            {
              if (att.Uri != null && att.Uri != "")
                fatal ("duplicate attribute {"+att.Uri+"}"+att.LocalName);
              else
                fatal ("duplicate attribute \""+att.LocalName + "\"");
            }

        }
      }

      // save element name so attribute callbacks work
      elementName = elname;
      if(namespaces) {
        processElement(elname, out localName, out ns);
        handler.StartElement(ns, localName, elname, this);
      } else
        handler.StartElement("", "", elname, this);
      
      // elements with no attributes are pretty common!
      if(attributes) {
        attributeCount = 0;
        attributes = false;
      }
    }

    internal void endElement(string elname) {
      IContentHandler  handler = contentHandler;

      if(!namespaces) {
        handler.EndElement("", "", elname);
        return;
      }
      
      string localName = string.Empty;
      string uri = string.Empty;
      processElement(elname, out localName, out uri);
      handler.EndElement(uri, localName, elname);

      ArrayList declared = namespaceSupport.GetDeclaredPrefixes();
      foreach (string prefix in declared) 
      {
        handler.EndPrefixMapping(prefix);
      }

      popContext();
    }

    internal void startCData() {
      lexicalHandler.StartCData();
    }

    internal void charData(char[] ch, int start, int length) {
      contentHandler.Characters(ch, start, length);
    }

    internal void endCData() {
      lexicalHandler.EndCData();
    }

    internal void ignorableWhitespace(char[] ch, int start, int length) {
      contentHandler.IgnorableWhitespace(ch, start, length);
    }

    internal void processingInstruction(string target, string data) {
      contentHandler.ProcessingInstruction(target, data);
    }

    internal void comment(char[] ch, int start, int length) {
      if(lexicalHandler != handlerBase)
        lexicalHandler.Comment(ch, start, length);
    }

    internal void fatal(string message) {
      ParseErrorImpl err = new ParseErrorImpl(message, this, "");
      errorHandler.FatalError(err);

      // Even if the application can continue ... we can't!
      throw new SaxParseException(err);
    }

    // We can safely report a few validity errors that
    // make layered Sax2 Dtd validation more conformant
    internal void verror(string message) {
      errorHandler.Error(new ParseErrorImpl(message, this, ""));
    }

    internal void warn(string message) {
      errorHandler.Warning(new ParseErrorImpl(message, this, ""));
    }


    //
    // Implementation of org.xml.sax.Attributes.
    //    

    /*
     * <b>Sax2 Attributes</b> method
     *(don't invoke on parser);
     */
    public int Length {
      get {
        return attributeCount;
      }
    }

    /*
     * <b>Sax2 Attributes</b> method(don't invoke on parser);
     */
    public string GetUri(int index) {
      if(index < 0 || index >= attributeCount)
        throw new IndexOutOfRangeException();
      return attributeData[index].Uri;
    }

    /*
     * <b>Sax2 Attributes</b> method(don't invoke on parser);
     */
    public string GetLocalName(int index) {
      if(index < 0 || index >= attributeCount)
        throw new IndexOutOfRangeException();
      return attributeData[index].LocalName;
    }

    /*
     * <b>Sax2 Attributes</b> method(don't invoke on parser);
     */
    public string GetQName(int index) {
      if(index < 0 || index >= attributeCount)
        throw new IndexOutOfRangeException();
      return attributeData[index].QName;
    }

    /*
     * <b>Sax1 AttributeList, Sax2 Attributes</b> method
     *(don't invoke on parser);
     */
    public string GetType(int index) {
      string type = parser.getAttributeType(elementName, GetQName(index));
      if(type == null)
        return "CData";
      // ... use DeclHandler.attributeDecl to see enumerations
      if(type == "ENUMERATION")
        return "NMTOKEN";
      return type;
    }


    /*
     * <b>Sax1 AttributeList, Sax2 Attributes</b> method
     *(don't invoke on parser);
     */
    public string GetValue(int index) {
      if(index < 0 || index >= attributeCount)
        throw new IndexOutOfRangeException();
      return attributeData[index].Value;
    }

    /*
     * <b>Sax2 Attributes</b> method(don't invoke on parser);
     */
    public int GetIndex(string uri, string local) {
      int length = Length;

      for(int i = 0; i < length; i++) {
        if(!attributeData[i].Uri.Equals(uri))
          continue;
        if(attributeData[i].LocalName.Equals(local))
          return i;
      }
      return -1;
    }


    /*
     * <b>Sax2 Attributes</b> method(don't invoke on parser);
     */
    public int GetIndex(string xmlName) {
      int length = Length;

      for(int i = 0; i < length; i++) {
        if(attributeData[i].QName.Equals(xmlName))
          return i;
      }
      return -1;
    }

    /*
     * <b>Sax2 Attributes</b> method(don't invoke on parser);
     */
    public string GetType(string uri, string local) {
      int index = GetIndex(uri, local);

      if(index < 0)
        throw new ArgumentException("Index out of range");
      return GetType(index);
    }


    /*
     * <b>Sax1 AttributeList, Sax2 Attributes</b> method
     *(don't invoke on parser);
     */
    public string GetType(string xmlName) {
      int index = GetIndex(xmlName);

      if(index < 0)
        return null;
      return GetType(index);
    }


    /*
     * <b>Sax Attributes</b> method(don't invoke on parser);
     */
    public string GetValue(string uri, string local) {
      int index = GetIndex(uri, local);

      if(index < 0)
        return null;
      return GetValue(index);
    }


    /*
     * <b>Sax1 AttributeList, Sax2 Attributes</b> method
     *(don't invoke on parser);
     */
    public string GetValue(string xmlName) {
      int index = GetIndex(xmlName);

      if(index < 0)
        return null;
      return GetValue(index);
    }


    /*
     * <b>Sax-ext Attributes</b> method(don't invoke on parser);
     */
    public bool IsSpecified(int index) {
      if(index < 0 || index >= attributeCount)
        throw new IndexOutOfRangeException();
      return attributeData[index].Specified;
    }


    /*
     * <b>Sax-ext Attributes</b> method(don't invoke on parser);
     */
    public bool IsSpecified(string uri, string local) {
      int index = GetIndex(uri, local);

      if(index < 0)
        throw new IndexOutOfRangeException();
      return attributeData[index].Specified;
    }


    /*
     * <b>Sax-ext Attributes</b> method(don't invoke on parser);
     */
    public bool IsSpecified(string xmlName) {
      int index = GetIndex(xmlName);

      if(index < 0)
        throw new IndexOutOfRangeException();
      return attributeData[index].Specified;
    }


    /*
     * <b>Sax-ext Attributes</b> method(don't invoke on parser);
     */
    public bool IsDeclared(int index) {
      if(index < 0 || index >= attributeCount)
        throw new IndexOutOfRangeException ();
      return attributeData[index].Declared;
    }


    /*
     * <b>Sax-ext Attributes</b> method(don't invoke on parser);
     */
    public bool IsDeclared(string uri, string local) {
      int index = GetIndex(uri, local);

      if(index < 0)
        throw new IndexOutOfRangeException();
      return attributeData[index].Declared;
    }


    /*
     * <b>Sax-ext Attributes</b> method(don't invoke on parser);
     */
    public bool IsDeclared(string xmlName) {
      int index = GetIndex(xmlName);

      if(index < 0)
        throw new IndexOutOfRangeException();
      return attributeData[index].Declared;
    }

    //
    // Implementation of org.xml.sax.Locator.
    //

    /*
     * <b>Sax Locator</b> method(don't invoke on parser);
     */
    public string PublicId {
      get {
        return null;     // FIXME track public IDs too
      }
    }

    /*
     * <b>Sax Locator</b> method(don't invoke on parser);
     */
    public string SystemId {
      get {
        if(entityStack.Count == 0)
          return null;
        else
          return(string) entityStack.Peek();
      }
    }

    /*
     * <b>Sax Locator</b> method(don't invoke on parser);
     */
    public long LineNumber {
      get {
        return parser.getLineNumber();
      }
    }

    /*
     * <b>Sax Locator</b> method(don't invoke on parser);
     */
    public long ColumnNumber {
      get {
        return parser.getColumnNumber();
      }
    }

    /*
     * <b>Sax Locator</b> method(don't invoke on parser);
     */
    public ParsedEntityType EntityType 
    {
      get 
      {
        if(parser != null)
          switch (parser.getParsedEntityType()) 
          {
            case 1:
              string sa = parser.getStandalone();
              if (sa == "yes")
                return ParsedEntityType.Standalone;
              else if (sa == "no")
                return ParsedEntityType.NotStandalone;
              else
                return ParsedEntityType.Document;
            case 2:
              return ParsedEntityType.General;
            case 3:
              return ParsedEntityType.Parameter;
          }
        return ParsedEntityType.Unknown;
      }
    }

    /*
     * <b>Sax Locator</b> method(don't invoke on parser);
     */
    public string XmlVersion 
    {
      get 
      {
        if(parser != null)
          return parser.getXmlVersion();
        else 
          return null;
      }
    }

    /*
     * <b>Sax Locator</b> method(don't invoke on parser);
     */
    public string Encoding 
    {
      get 
      {
        if(parser != null)
          return parser.getEncoding();
        else
          return null;
      }
    }   
    
    internal void startContentModel(string name) 
    {
      cmHandler.StartContentModel(name);
    }

    internal void endContentModel()
    {
      cmHandler.EndContentModel();
    }
    
    internal void contentModelEmpty()
    {
      cmHandler.Empty();
    }

    internal void contentModelAny()
    {
      cmHandler.Any();
    }

    internal void contentModelChoice()
    {
      cmHandler.Choice();
    }

    internal void contentModelMixed()
    {
      cmHandler.Mixed();
    }

    internal void contentModelSequence()
    {
      cmHandler.Sequence();
    }

    internal void contentModelStartGroup()
    {
      cmHandler.StartGroup();
    }

    internal void contentModelEndGroup(char occurences)
    {
      cmHandler.EndGroup(occurences);
    }

    internal void contentModelElementParticle(string name, char occurences)
    {
      cmHandler.ElementParticle(name, occurences);
    }
  }
}

//!!
//FIXME
//.NET
//... fix xlmnsUris
//... fix duplicate attribute bug
//... fix  NCName checking?

//OPTIMIZE Filter to count the number of filters that need to occur

