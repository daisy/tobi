using System;
using System.IO;
using System.Collections;
using System.Net;
using Org.System.Xml.Sax;

/*** License **********************************************************
*
* Copyright (c) 2005, Jeff Rafter
*
* This file is part of the AElfred C# library. AElfred C# was converted 
* from the AElfred2 library for Java written by David Brownell (from
* the October 2002 version). Because his changes were released under 
* the GPL with Library Exception license, this file inherits that 
* license because it is a derived work. 
*
* Many enhancements for XML conformance and SAX conformance were added 
* in the conversion. All changes can be utilized under the a public
* domain license.
*
* AElfred is free for both commercial and non-commercial use and
* redistribution, provided that the following copyrights and disclaimers 
* are retained intact.  You are free to modify AElfred for your own use and
* to redistribute AElfred with your modifications, provided that the
* modifications are clearly documented.
*
* This program is distributed in the hope that it will be useful, but
* WITHOUT ANY WARRANTY; without even the implied warranty of
* merchantability or fitness for a particular purpose.  Please use it AT
* YOUR OWN RISK.
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
     * Custom exception for AElfred to stop parsing. The exception type is 
     * used for determining appropriate action when parsing has reached the 
     * end of the file.
     * @author Jeff Rafter
     * @see XmlParser
     * @see SaxException
    */
  public class AElfredEofException : SaxException {
    public AElfredEofException(string message): base(message) { }    
  }

  /*
     * Custom exception for AElfred to indicate an unsupported encoding was
     * encountered. The encoding string is stored in the exception.
     * @author Jeff Rafter
     * @see XmlParser
     * @see SaxException
    */
  public class AElfredUnsupportedEncodingException : SaxException {
    
    private string encoding;

    public AElfredUnsupportedEncodingException(string encoding) {
      this.encoding = encoding;
    }
	  
    public AElfredUnsupportedEncodingException(string message, string encoding): base(message) {
      this.encoding = encoding;
    }

    public string getEncoding() {
      return this.encoding;
    }
  }


  /*
   * Parse Xml documents and return parse events through call-backs.
   * Use the <code>SaxDriver</code> class as your entry point, as all
   * internal parser interfaces are subject to change.
   *
   * @author Written by David Megginson &lt;dmeggins@microstar.com&gt;
   * (version 1.2a with bugfixes)
   * @author Updated by David Brownell &lt;dbrownell@users.sourceforge.net&gt;
   * @author Converted to C# by Jeff Rafter
   * @see SaxDriver
  */
  public sealed class XmlParser {

    // avoid slow per-character readCh()
    private const bool USE_CHEATS = true;

    /*
     * Construct a new parser with no associated handler.
     * @see #setHandler
     * @see #parse
     */
    // package private
    internal XmlParser() {
    
    }

    /*
     * Set the handler that will receive parsing events.
     * @param handler The handler to receive callback events.
     * @see #parse
     */
    // package private
    internal void setHandler (SaxDriver handler) {
      this.handler = handler;
    }

    /*
     * Parse an Xml document from the character stream, byte stream, or URI
     * that you provide (in that order of preference).  Any URI that you
     * supply will become the base URI for resolving relative URI, and may
     * be used to acquire a reader or byte stream.
     *
     * <p> Only one thread at a time may use this parser; since it is
     * private to this package, post-parse cleanup is done by the caller,
     * which MUST NOT REUSE the parser (just null it).
     *
     * @param systemId Absolute URI of the document; should never be null,
     * but may be so iff a reader <em>or</em> a stream is provided.
     * @param publicId The public identifier of the document, or null.
     * @param reader A character stream; must be null if stream isn't.
     * @param stream A byte input stream; must be null if reader isn't.
     * @param encoding The suggested encoding, or null if unknown.
     */
    // package private 
    internal void doParse (string systemId, string publicId, TextReader reader, Stream stream,
      string encoding) {
      if (handler == null)
        throw new SaxException("no callback handler");

      initializeVariables ();

      // predeclare the built-in entities here (replacement texts)
      // we don't need to intern(), since we're guaranteed literals
      // are always (globally) interned.
      setInternalEntity ("amp", "&#38;");
      setInternalEntity ("lt", "&#60;");
      setInternalEntity ("gt", "&#62;");
      setInternalEntity ("apos", "&#39;");
      setInternalEntity ("quot", "&#34;");

      try {
        // pushURL first to ensure locator is correct in startDocument
        // ... it might report an IO or encoding exception.        
        try 
        {
          pushURL (false, "[document]", new string [] {publicId, systemId, null},
            reader, stream, encoding, false);

          parseDocument ();
        } 
        catch (AElfredEofException) 
        { 
          fatalError("premature end of file", "[EOF]", null);
        }
      } finally {
        if (reader != null)
          try { 
            reader.Close ();
          } catch { /* ignore */ }
        if (stream != null) 
          try { 
            stream.Close ();
          } catch { /* ignore */ }
        if (inputStream != null)
          try {
            inputStream.Close ();
          } catch { /* ignore */ }
        if (reader != null)
          try {
            reader.Close ();
          } catch { /* ignore */
          }
      }
    }

    ////////////////////////////////////////////////////////////////////////
    // Constants.
    ////////////////////////////////////////////////////////////////////////

    //
    // Constants for element content type.
    //

    /*
     * Constant: an element has not been declared.
     * @see #getElementContentType
     */
    public const int CONTENT_UNDECLARED = 0;

    /*
     * Constant: the element has a content model of ANY.
     * @see #getElementContentType
     */
    public const int CONTENT_ANY = 1;

    /*
     * Constant: the element has declared content of EMPTY.
     * @see #getElementContentType
     */
    public const int CONTENT_EMPTY = 2;

    /*
     * Constant: the element has mixed content.
     * @see #getElementContentType
     */
    public const int CONTENT_MIXED = 3;

    /*
     * Constant: the element has element content.
     * @see #getElementContentType
     */
    public const int CONTENT_ELEMENTS = 4;


    //
    // Constants for the entity type.
    //

    /*
     * Constant: the entity has not been declared.
     * @see #getEntityType
     */
    public const int ENTITY_UNDECLARED = 0;

    /*
     * Constant: the entity is internal.
     * @see #getEntityType
     */
    public const int ENTITY_INTERNAL = 1;

    /*
     * Constant: the entity is external, non-parsable data.
     * @see #getEntityType
     */
    public const int ENTITY_NDATA = 2;

    /*
     * Constant: the entity is external Xml data.
     * @see #getEntityType
     */
    public const int ENTITY_TEXT = 3;


    //
    // Attribute type constants are interned literal strings.
    //

    //
    // Constants for supported encodings.  "external" is just a flag.
    //
    private const int ENCODING_EXTERNAL = 0;
    private const int ENCODING_UTF_8 = 1;
    private const int ENCODING_ISO_8859_1 = 2;
    private const int ENCODING_UCS_2_12 = 3;
    private const int ENCODING_UCS_2_21 = 4;
    private const int ENCODING_UCS_4_1234 = 5;
    private const int ENCODING_UCS_4_4321 = 6;
    private const int ENCODING_UCS_4_2143 = 7;
    private const int ENCODING_UCS_4_3412 = 8;
    private const int ENCODING_ASCII = 9;


    //
    // Constants for attribute default value.
    //

    /*
     * Constant: the attribute is not declared.
     * @see #getAttributeDefaultValueType
     */
    public const int ATTRIBUTE_DEFAULT_UNDECLARED = 30;

    /*
     * Constant: the attribute has a literal default value specified.
     * @see #getAttributeDefaultValueType
     * @see #getAttributeDefaultValue
     */
    public const int ATTRIBUTE_DEFAULT_SPECIFIED = 31;

    /*
     * Constant: the attribute was declared #IMPLIED.
     * @see #getAttributeDefaultValueType
     */
    public const int ATTRIBUTE_DEFAULT_IMPLIED = 32;

    /*
     * Constant: the attribute was declared #REQUIRED.
     * @see #getAttributeDefaultValueType
     */
    public const int ATTRIBUTE_DEFAULT_REQUIRED = 33;

    /*
     * Constant: the attribute was declared #FIXED.
     * @see #getAttributeDefaultValueType
     * @see #getAttributeDefaultValue
     */
    public const int ATTRIBUTE_DEFAULT_FIXED = 34;


    //
    // Constants for input.
    //
    private const int INPUT_NONE = 0;
    private const int INPUT_INTERNAL = 1;
    private const int INPUT_STREAM = 3;
    private const int INPUT_READER = 5;

    //
    // Constants for entities.
    //
    private const int ENTITY_TYPE_NONE = 0;
    private const int ENTITY_TYPE_DOCUMENT = 1;
    private const int ENTITY_TYPE_EXT_GE = 2;
    private const int ENTITY_TYPE_EXT_PE = 3;

    //
    // Flags for reading literals.
    //
    // expand general entity refs (attribute values in dtd and content)
    private const int LIT_ENTITY_REF = 2;
    // normalize this value (space chars) (attributes, public ids)
    private const int LIT_NORMALIZE = 4;
    // literal is an attribute value 
    private const int LIT_ATTRIBUTE = 8;
    // don't expand parameter entities
    private const int LIT_DISABLE_PE = 16;
    // don't expand [or parse] character refs
    private const int LIT_DISABLE_CREF = 32;
    // don't parse general entity refs
    private const int LIT_DISABLE_EREF = 64;
    // literal is a public ID value 
    private const int LIT_PUBID = 256;

    //
    // Flags affecting PE handling in DTDs (if expandPE is true).
    // PEs expand with space padding, except inside literals.
    //
    private const int CONTEXT_NORMAL = 0;
    private const int CONTEXT_LITERAL = 1;

    //
    // Flags affecting entity literal balance checking and WFness.
    //
    private const int ENTITY_BOUNDARY_ENABLE = 0;
    private const int ENTITY_BOUNDARY_DISABLE = 1;
    private const int ENTITY_BOUNDARY_DISABLE_STAG = 2;
    private const int ENTITY_BOUNDARY_DISABLE_ETAG = 3;
    private const int ENTITY_BOUNDARY_DISABLE_EMPTY_ELEMENT = 4;
    private const int ENTITY_BOUNDARY_DISABLE_COMMENT = 5;
    private const int ENTITY_BOUNDARY_DISABLE_PI = 6;
    private const int ENTITY_BOUNDARY_DISABLE_EREF = 7;
    private const int ENTITY_BOUNDARY_DISABLE_CREF = 8;
    private const int ENTITY_BOUNDARY_DISABLE_CDATASECT = 9;

    //////////////////////////////////////////////////////////////////////
    // Signature for detectEncoding
    //////////////////////////////////////////////////////////////////////
    private byte[] signature = null;
    private int signatureLengthRead = 0;

    //////////////////////////////////////////////////////////////////////
    // Error reporting.
    //////////////////////////////////////////////////////////////////////


    /*
     * Report an entity boundary error.
     * @see SaxDriver#error
     * @see #line
     */
    private void entityBoundaryError () 
    {
      string message = "Entity boundary error, ";

      switch (entityBoundary) 
      {
        case ENTITY_BOUNDARY_DISABLE:
          message += " attempting to switch from current entity during the parse of a content production";
          break;
        case ENTITY_BOUNDARY_DISABLE_STAG:
          message += " attempting to switch from current entity during the parse of a start tag";
          break;
        case ENTITY_BOUNDARY_DISABLE_ETAG:
          message += " attempting to switch from current entity during the parse of an end tag";
          break;
        case ENTITY_BOUNDARY_DISABLE_EMPTY_ELEMENT:
          message += " attempting to switch from current entity during the parse of an empty element";
          break;
        case ENTITY_BOUNDARY_DISABLE_COMMENT:
          message += " attempting to switch from current entity during the parse of a comment";
          break;
        case ENTITY_BOUNDARY_DISABLE_PI:
          message += " attempting to switch from current entity during the parse of a processing instruction";
          break;
        case ENTITY_BOUNDARY_DISABLE_EREF:
          message += " attempting to switch from current entity during the parse of a entity reference";
          break;
        case ENTITY_BOUNDARY_DISABLE_CREF:
          message += " attempting to switch from current entity during the parse of a character reference";
          break;
        case ENTITY_BOUNDARY_DISABLE_CDATASECT:
          message += " attempting to switch from current entity during the parse of a CDATA section";
          break;
      }
      handler.fatal (message);

      // "can't happen"
      throw new SaxException (message);
    }

    /*
     * Report an error.
     * @param message The error message.
     * @param textFound The text that caused the error (or null).
     * @see SaxDriver#error
     * @see #line
     */
    private void fatalError (string message, string textFound, string textExpected) 
    {
      if (textFound != null) {
        message = message + " (found \"" + textFound + "\")";
      }
      if (textExpected != null) {
        message = message + " (expected \"" + textExpected + "\")";
      }
      handler.fatal (message);

      // "can't happen"
      throw new SaxException (message);
    }


    /*
     * Report a serious error.
     * @param message The error message.
     * @param textFound The text that caused the error (or null).
     */
    private void fatalError (string message, char textFound, string textExpected) {
      fatalError (message, textFound.ToString(), textExpected);
    }

    /* Report typical case fatal errors. */
    private void fatalError (string message) {
      handler.fatal (message);
    }

    //////////////////////////////////////////////////////////////////////
    // Major syntactic productions.
    //////////////////////////////////////////////////////////////////////


    /*
     * Parse an Xml document.
     * <pre>
     * [1] document ::= prolog element Misc*
     * </pre>
     * <p>This is the top-level parsing function for a single Xml
     * document.  As a minimum, a well-formed document must have
     * a document element, and a valid document must have a prolog
     * (one with doctype) as well.
     */
    private void parseDocument () {
      bool sawDTD = parseProlog ();
      require ('<');
      parseElement (!sawDTD);
        
      try {
        parseMisc ();   //skip all white, PIs, and comments
        char c = readCh ();    //if this doesn't throw an exception...
        fatalError ("unexpected characters after document end", c, null);
      } catch (AElfredEofException) {
        return;
      }
    }

    public char[] startDelimComment = new char[4] { '<', '!', '-', '-' };
    public char[] endDelimComment = new char[2] { '-', '-' };

    /*
     * Skip a comment.
     * <pre>
     * [15] Comment ::= '&lt;!--' ((Char - '-') | ('-' (Char - '-')))* "-->"
     * </pre>
     * <p> (The <code>&lt;!--</code> has already been read.)
     */
    private void parseComment () {
      bool saved = expandPE;

      expandPE = false;
      parseUntil (endDelimComment);
      require ('>');
      expandPE = saved;
      handler.comment (dataBuffer, 0, dataBufferPos);
      dataBufferPos = 0;
    }

    public char[] startDelimPI = new char[2] { '<', '?' };
    public char[] endDelimPI = new char[2] { '?', '>' };

    /*
     * Parse a processing instruction and do a call-back.
     * <pre>
     * [16] PI ::= '&lt;?' PITarget
     *  (S (Char* - (Char* '?&gt;' Char*)))?
     *  '?&gt;'
     * [17] PITarget ::= Name - ( ('X'|'x') ('M'|m') ('L'|l') )
     * </pre>
     * <p> (The <code>&lt;?</code> has already been read.)
     */
    private void parsePI () {
      string name;
      bool saved = expandPE;

      expandPE = false;
      bool savedAllowColon = allowColon;
      allowColon = false;
      name = readNmtoken (true);
      allowColon = savedAllowColon;

      if ("xml".Equals (name.ToLower()))
        fatalError ("Illegal processing instruction target", name, null);
      if (!tryRead (endDelimPI)) {
        requireWhitespace ();
        parseUntil (endDelimPI);
      }
      expandPE = saved;
      handler.processingInstruction (name, dataBufferTostring ());
    }

    public char[] endDelimCDATA = new char[3] { ']', ']', '>' };

    /*
     * Parse a CDATA section.
     * <pre>
     * [18] CDSect ::= CDStart CData CDEnd
     * [19] CDStart ::= '&lt;![CDATA['
     * [20] CData ::= (Char* - (Char* ']]&gt;' Char*))
     * [21] CDEnd ::= ']]&gt;'
     * </pre>
     * <p> (The '&lt;![CDATA[' has already been read.)
     */
    private void parseCDSect () {
      parseUntil (endDelimCDATA);
      dataBufferFlush ();
    }

    /*
     * Parse the prolog of an Xml document.
     * <pre>
     * [22] prolog ::= XmlDecl? Misc* (Doctypedecl Misc*)?
     * </pre>
     * <p>We do not look for the Xml declaration here, because it was
     * handled by pushURL ().
     * @see pushURL
     * @return true if a DTD was read.
     */
    private bool parseProlog () {
      parseMisc ();

      if (tryRead ("<!DOCTYPE")) {
        parseDoctypedecl ();
        parseMisc ();
        return true;
      }
      return false;
    }

    private void checkLegalVersion (string version) {
      // E38 simplified this check
      //FIXME, can handle 1.1
      if (version != "1.0") 
      {
        xmlVersion = version;
        fatalError ("illegal character in version", version, "1.0");
      }
    }

    private void checkLegalEncoding (string encoding) 
    {
      int len = encoding.Length;
      char c;
      if (len > 0) 
      {     
        c = encoding[0];
        if (!(('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z')))
          fatalError ("illegal encoding name \"" + encoding + "\", encodings must begin with A-Z or a-z");
      }

      for (int i = 1; i < len; i++) 
      {
        c = encoding[i];
        if ('A' <= c && c <= 'Z')
          continue;
        if ('a' <= c && c <= 'z')
          continue;
        if ('0' <= c && c <= '9')
          continue;
        if (c == '_' || c == '.' || c == '-')
          continue;
        fatalError ("illegal character \"" + c + "\" in encoding \"" + encoding + "\"");
      }
    }

    /*
     * Parse the Xml declaration.
     * <pre>
     * [23] XmlDecl ::= '&lt;?xml' VersionInfo EncodingDecl? SDDecl? S? '?&gt;'
     * [24] VersionInfo ::= S 'version' Eq
     *  ("'" VersionNum "'" | '"' VersionNum '"' )
     * [26] VersionNum ::= "1.0" (was: ([a-zA-Z0-9_.:] | '-')*, changed in [E38])
     * [32] SDDecl ::= S 'standalone' Eq
     *  ( "'"" ('yes' | 'no') "'"" | '"' ("yes" | "no") '"' )
     * [80] EncodingDecl ::= S 'encoding' Eq
     *  ( "'" EncName "'" | "'" EncName "'" )
     * [81] EncName ::= [A-Za-z] ([A-Za-z0-9._] | '-')*
     * </pre>
     * <p> (The <code>&lt;?xml</code> and whitespace have already been read.)
     * @return the encoding in the declaration, uppercased; or null
     * @see #parseTextDecl
     * @see #setupDecoding
     */
    private string parseXmlDecl (bool ignoreEncoding) 
    {
      string version;
      string encodingLabel = null;
      int flags = LIT_DISABLE_CREF | LIT_DISABLE_PE | LIT_DISABLE_EREF;

      // Read the version.
      require ("version");
      parseEq ();
      checkLegalVersion (version = readLiteral (flags));
      //FIXME handle version 1.1
      if (!version.Equals ("1.0"))
        handler.warn ("expected Xml version 1.0, not: " + version);

      // Try reading an encoding declaration.
      bool white = tryWhitespace ();

      if (tryRead ("encoding")) {
        if (!white)
          fatalError ("whitespace required before 'encoding='");
        parseEq ();
        checkLegalEncoding (encodingLabel = readLiteral (flags));
        if (!ignoreEncoding)
          setupDecoding (encodingLabel);
      }

      // Try reading a standalone declaration
      if (encodingLabel != null)
        white = tryWhitespace ();
      if (tryRead ("standalone")) {
        if (!white)
          fatalError ("whitespace required before 'standalone='");
        parseEq ();
        standalone = readLiteral (flags);
        if ("yes".Equals (standalone))
          docIsStandalone = true;
        else if (!"no".Equals (standalone))
          fatalError ("standalone flag must be 'yes' or 'no'");
      }

      skipWhitespace ();
      require ("?>");

      return encodingLabel;
    }


    /*
     * Parse a text declaration.
     * <pre>
     * [79] TextDecl ::= '&lt;?xml' VersionInfo? EncodingDecl S? '?&gt;'
     * [80] EncodingDecl ::= S 'encoding' Eq
     *  ( '"' EncName '"' | "'" EncName "'" )
     * [81] EncName ::= [A-Za-z] ([A-Za-z0-9._] | '-')*
     * </pre>
     * <p> (The <code>&lt;?xml</code>' and whitespace have already been read.)
     * @return the encoding in the declaration, uppercased; or null
     * @see #parseXmlDecl
     * @see #setupDecoding
     */
    private string parseTextDecl (bool ignoreEncoding) { 
      string encodingLabel = null;
      int flags = LIT_DISABLE_CREF | LIT_DISABLE_PE | LIT_DISABLE_EREF;

      // Read an optional version.
      if (tryRead ("version")) {
        string version;
        parseEq ();
        checkLegalVersion (version = readLiteral (flags));
        //FIXME handle version 1.1
        if (!version.Equals ("1.0"))
          handler.warn ("expected Xml version 1.0, not: " + version);
        requireWhitespace ();
      }


      // Read the encoding.
      require ("encoding");
      parseEq ();
      encodingLabel = readLiteral (flags);
      if (!ignoreEncoding)
        setupDecoding (encodingLabel);

      skipWhitespace ();
      require ("?>");

      return encodingLabel;
    }


    /*
     * Sets up internal state so that we can decode an entity using the
     * specified encoding.  This is used when we start to read an entity
     * and we have been given knowledge of its encoding before we start to
     * read any data (e.g. from a Sax input source or from a MIME type).
     *
     * <p> It is also used after autodetection, at which point only very
     * limited adjustments to the encoding may be used (switching between
     * related builtin decoders).
     *
     * @param decodingName The name of the encoding specified by the user.
     * @exception IOException if the encoding isn't supported either
     * internally to this parser, or by the hosting environment.
     * @see #parseXmlDecl
     * @see #parseTextDecl
     */
    private void setupDecoding (string decodingName) {
      string decodingNameUpper = decodingName.ToUpper ();

      // ENCODING_EXTERNAL indicates an encoding that wasn't
      // autodetected ... we can use builtin decoders, or
      // ones that are built in.

      // Otherwise we can only tweak what was autodetected, and
      // only for single byte (ASCII derived) builtin encodings.

      // ASCII-derived encodings
      if (encoding == ENCODING_UTF_8 || encoding == ENCODING_EXTERNAL) 
      {
        if (decodingNameUpper.Equals ("ISO-8859-1")
          || decodingNameUpper.Equals ("8859_1")
          || decodingNameUpper.Equals ("ISO8859_1")
          ) {
          encoding = ENCODING_ISO_8859_1;
          return;
        } else if (decodingNameUpper.Equals ("US-ASCII")
          || decodingNameUpper.Equals ("ASCII")) {
          encoding = ENCODING_ASCII;
          return;
        } else if (decodingNameUpper.Equals ("UTF-8")
          || decodingNameUpper.Equals ("UTF8")) {
          encoding = ENCODING_UTF_8;
          return;
        } else if (encoding != ENCODING_EXTERNAL) {
          // used to start with a new reader ...
          throw new AElfredUnsupportedEncodingException (decodingName);
        }
        // else fallthrough ...
        // it's ASCII-ish and something other than a builtin
      }

      // Unicode and such
      if (encoding == ENCODING_UCS_2_12) {
        if (!(decodingNameUpper.Equals ("ISO-10646-UCS-2")
          || decodingNameUpper.Equals ("UTF-16")
          || decodingNameUpper.Equals ("UTF-16BE")))
          throw new AElfredUnsupportedEncodingException (decodingName);
        //!!.NET changed to exception 
        //fatalError ("unsupported Unicode encoding",
        //    decodingName,
        //    "UTF-16");
        return;
      }

      // Unicode and such (Split these to check for correct endian labeling)
      if (encoding == ENCODING_UCS_2_21) 
      {
        if (!(decodingNameUpper.Equals ("ISO-10646-UCS-2")
          || decodingNameUpper.Equals ("UTF-16")
          || decodingNameUpper.Equals ("UTF-16LE")))
          throw new AElfredUnsupportedEncodingException (decodingName);
        //!!.NET changed to exception 
        // fatalError ("unsupported Unicode encoding",
        //    decodingName,
        //    "UTF-16");
        return;
      }

      // four byte encodings (we are not checking the byte order explicitly here, but in the decoding)
      if (encoding == ENCODING_UCS_4_1234
        || encoding == ENCODING_UCS_4_4321
        || encoding == ENCODING_UCS_4_2143
        || encoding == ENCODING_UCS_4_3412) {
        // Strictly:  "UCS-4" == "UTF-32BE"; also, "UTF-32LE" exists
        if (!decodingNameUpper.Equals ("ISO-10646-UCS-4"))
          throw new AElfredUnsupportedEncodingException (decodingName);
        //!!.NET changed to exception 
        //fatalError ("unsupported 32-bit encoding",
        //    decodingName,
        //    "ISO-10646-UCS-4");
        return;
      }

      // assert encoding == ENCODING_EXTERNAL
      // if (encoding != ENCODING_EXTERNAL)
      //     throw new RuntimeException ("encoding = " + encoding);

      if (decodingNameUpper.Equals ("UTF-16BE")) {
        encoding = ENCODING_UCS_2_12;
        return;
      }
      if (decodingNameUpper.Equals ("UTF-16LE")) {
        encoding = ENCODING_UCS_2_21;
        return;
      }

      // We couldn't use the builtin decoders at all.  But we can try to
      // create a reader, since we haven't messed up buffering.  Tweak
      // the encoding name if necessary.

      if (decodingNameUpper.Equals ("UTF-16")
        || decodingNameUpper.Equals ("ISO-10646-UCS-2"))
        decodingName = "Unicode";
      
      //.NET
      throw new AElfredUnsupportedEncodingException (decodingName);

      //!! we are now offloading this to the exception handling routine which will reset
      // Ignoring all the EBCDIC aliases here
      //!!enc reader = new StreamReader(inputStream, System.Text.Encoding.GetEncoding(decodingName));
      //!!enc sourceType = INPUT_READER;
    }


    /*
     * Parse miscellaneous markup outside the document element and DOCTYPE
     * declaration.
     * <pre>
     * [27] Misc ::= Comment | PI | S
     * </pre>
     */
    private void parseMisc () {
      while (true) {
        skipWhitespace ();
        if (tryRead (startDelimPI)) {
          parsePI ();
        } else if (tryRead (startDelimComment)) {
          parseComment ();
        } else {
          return;
        }
      }
    }

    /*
     * Parse a document type declaration.
     * <pre>
     * [28] doctypedecl ::= '&lt;!DOCTYPE' S Name (S ExternalID)? S?
     *  ('[' (markupdecl | PEReference | S)* ']' S?)? '&gt;'
     * </pre>
     * <p> (The <code>&lt;!DOCTYPE</code> has already been read.)
     */
    private void parseDoctypedecl () {
      string rootName;
      string[] ids;

      // Read the document type name.
      requireWhitespace ();
      rootName = readNmtoken (true);

      // Read the External subset's IDs
      skipWhitespace ();
      ids = readExternalIds (false, true);

      // report (a) declaration of name, (b) lexical info (ids)
      handler.doctypeDecl (rootName, ids [0], ids [1]);

      // Internal subset is parsed first, if present
      skipWhitespace ();
      if (tryRead ('[')) {
        inInternalSubset = true;
        // loop until the subset ends
        while (true) {
          doReport = expandPE = true;
          skipWhitespace ();
          doReport = expandPE = false;
          if (tryRead (']')) {
            // end of subset
            inInternalSubset = false;
            break;   
          } 
          else 
            parseMarkupdecl ();
        }
      }
      skipWhitespace ();
      require ('>');

      // Read the external subset, if any
      InputSource subset;

      if (ids [1] == null)
        subset = handler.getExternalSubset (rootName,
          handler.SystemId);
      else
        subset = null;
      if (ids [1] != null || subset != null) {
        //!!.NET I cut this because it is confusing and can be handled by checking the inputStack.Count 
        //!!.NET pushstring (null, ">");

        // NOTE:  [dtd] is so we say what Sax2 expects,
        // though it's misleading (subset, not entire dtd)
        if (ids [1] != null)
          pushURL (true, "[dtd]", ids, null, null, null, true);
        else {
          handler.warn ("modifying document by adding external subset");
          if (subset is InputSource<Stream>)
            pushURL (true, "[dtd]", new string [] { subset.PublicId, subset.SystemId, null },
              null, ((InputSource<Stream>)subset).Source, subset.Encoding, false);
          else if (subset is InputSource<TextReader>)
            pushURL (true, "[dtd]", new string [] { subset.PublicId, subset.SystemId, null },
              ((InputSource<TextReader>)subset).Source, null, subset.Encoding, false);
          else
            pushURL (true, "[dtd]", new string [] { subset.PublicId, subset.SystemId, null },
              null, null,  subset.Encoding, false);

        }

        // Loop until we end up back at the document entity
        while (true) 
        {
          doReport = expandPE = true;
          skipWhitespace ();
          doReport = expandPE = false;
          if (inputStack.Count == 0) {
            break;
          } else 
            parseMarkupdecl ();
        }

        //!!.NET This was cut to match the cut above, we no longer pushString(">");
        //!!.NET the ">" string isn't popped yet
        //!!.NET if (inputStack.Count != 1)
        //!!.NET   fatalError ("external subset has unmatched '>'");
      }

      // done dtd
      handler.endDoctype ();
      expandPE = false;
      doReport = true;
    }


    /*
     * Parse a markup declaration in the internal or external DTD subset.
     * <pre>
     * [29] markupdecl ::= elementdecl | Attlistdecl | EntityDecl
     *  | NotationDecl | PI | Comment
     * [30] extSubsetDecl ::= (markupdecl | conditionalSect
     *  | PEReference | S) *
     * </pre>
     * <p> Reading toplevel PE references is handled as a lexical issue
     * by the caller, as is whitespace.
     */
    private void parseMarkupdecl () {
      char[] saved = null;
      expandPE = true;
      inDeclaration = true;
      bool savedPeRefBetweenDecls = inPeRefBetweenDecls;
      
      // prevent "<%foo;" and ensures saved entity is right
      require ('<');
      unread ('<');
      expandPE = false;

      if (tryRead ("<!ELEMENT")) {
        saved = readBuffer;
        expandPE = true;
        parseElementDecl ();
        if (readBuffer != saved) 
          handler.verror ("Illegal Declaration/PE nesting");
      } 
      else if (tryRead ("<!ATTLIST")) 
      {
        saved = readBuffer;
        expandPE = true;
        parseAttlistDecl ();
        if (readBuffer != saved)
          handler.verror ("Illegal Declaration/PE nesting");
      } 
      else if (tryRead ("<!ENTITY")) 
      {
        saved = readBuffer;
        expandPE = true;
        parseEntityDecl ();
        if (readBuffer != saved)
          handler.verror ("Illegal Declaration/PE nesting");
      } 
      else if (tryRead ("<!NOTATION")) 
      {
        saved = readBuffer;
        expandPE = true;
        parseNotationDecl ();
        if (readBuffer != saved)
          handler.verror ("Illegal Declaration/PE nesting");
      } 
      else if (tryRead (startDelimPI)) 
      {
        saved = readBuffer;
        expandPE = true;
        parsePI ();
        if (readBuffer != saved)
          handler.verror ("Illegal Declaration/PE nesting");
      } 
      else if (tryRead (startDelimComment)) 
      {
        saved = readBuffer;
        expandPE = true;
        parseComment ();
        if (readBuffer != saved)
          handler.verror ("Illegal Declaration/PE nesting");
      } 
      else if (tryRead ("<![")) 
      {
        saved = readBuffer;
        expandPE = true;
        if (inputStack.Count > 0)
          parseConditionalSect (saved);
        else if (tryRead ("CDATA"))
          // Okay, this is admittedly overkill, but either way it is a fatal 
          // error at this point, so why not add a little extra checking?
          fatalError ("CDATA sections not permitted between markup declarations");
        else
          fatalError ("conditional sections illegal in internal subset");
        if (readBuffer != saved)
          handler.verror ("Illegal Conditional Section/PE nesting");
      } 
      else 
      {
        fatalError ("expected markup declaration");
      }

      if ((readBuffer != saved) && (savedPeRefBetweenDecls))
        fatalError ("parameter entity reference between declarations has replacement text that is not wellformed");

      inDeclaration = false;
      expandPE = false;
    }


    /*
     * Parse an element, with its tags.
     * <pre>
     * [39] element ::= EmptyElementTag | STag content ETag
     * [40] STag ::= '&lt;' Name (S Attribute)* S? '&gt;'
     * [44] EmptyElementTag ::= '&lt;' Name (S Attribute)* S? '/&gt;'
     * </pre>
     * <p> (The '&lt;' has already been read.)
     * <p>NOTE: this method actually chains onto parseContent (), if necessary,
     * and parseContent () will take care of calling parseETag ().
     */
    private void parseElement (bool maybeGetSubset) {
      string gi;
      char c;
      int oldElementContent = currentElementContent;
      string oldElement = currentElement;
      object[] element;

      // This is the (global) counter for the
      // array of specified attributes.
      tagAttributePos = 0;

      // Read the element type name.
      gi = readNmtoken (true);

      // If we saw no DTD, and this is the document root element,
      // let the application modify the input stream by providing one.
      if (maybeGetSubset) {
        InputSource subset = handler.getExternalSubset (gi,
          handler.SystemId);
        if (subset != null) {
          string publicId = subset.PublicId;
          string systemId = subset.SystemId;

          handler.warn ("modifying document by adding DTD");
          handler.doctypeDecl (gi, publicId, systemId);
          pushstring (null, ">");

          // NOTE:  [dtd] is so we say what Sax2 expects,
          // though it's misleading (subset, not entire dtd)
          if (subset is InputSource<Stream>)
            pushURL (true, "[dtd]", new string [] { publicId, systemId, null },
              null, ((InputSource<Stream>)subset).Source, subset.Encoding, false);
          else if (subset is InputSource<TextReader>)
            pushURL (true, "[dtd]", new string [] { publicId, systemId, null },
              ((InputSource<TextReader>)subset).Source, null, subset.Encoding, false);
          else
            pushURL (true, "[dtd]", new string [] { publicId, systemId, null },
              null, null,  subset.Encoding, false);

          // Loop until we end up back at '>'
          while (true) {
            doReport = expandPE = true;
            skipWhitespace ();
            doReport = expandPE = false;
            if (tryRead ('>')) {
              break;
            } else 
              parseMarkupdecl ();
          }

          // the ">" string isn't popped yet
          if (inputStack.Count != 1)
            fatalError ("external subset has unmatched '>'");

          handler.endDoctype ();
        }
      }

      // Determine the current content type.
      currentElement = gi;
      element = (object []) elementInfo[gi];
      currentElementContent = getContentType (element, CONTENT_ANY);

      // Push a new element context now to prepare for the declarations
      handler.pushContext();

      // Read the attributes, if any.
      // After this loop, "c" is the closing delimiter.
      bool white = tryWhitespace ();
      c = readCh ();
      while (c != '/' && c != '>') {
        unread (c);
        if (!white) 
        {
          if ((!XmlUtils.isXmlLetter(c)) && (c != ':') && (c != '_'))
            fatalError ("unexpected character (found \"" + c + "\") (expected \"\\\", \">\", or whitespace)");
          else
            fatalError ("need whitespace between attributes");
        }
        parseAttribute (gi);
        white = tryWhitespace ();
        c = readCh ();
      }

      // Supply any defaulted attributes.
      ICollection atts = declaredAttributes (element); //DR?
      if (atts != null) {
        string aname;
        bool continueLoop = false;
        // FIXME.NET this could be bad...
        foreach (object item in atts) {
          aname = (string)item;
          // See if it was specified.
          continueLoop = false;
          for (int i = 0; i < tagAttributePos; i++) {
            if (tagAttributes [i] == aname) {
              continueLoop = true;
              break;
            }
          }
          if (continueLoop) {
            continueLoop = false;
            continue;
          }
          // ... or has a default
          string value = getAttributeDefaultValue (gi, aname);

          if (value == null)
            continue;
          handler.attribute (aname, value, false, false, -1, -1, -1, -1);
        }
      }

      // Figure out if this is a start tag
      // or an empty element, and dispatch an
      // event accordingly.
      switch (c) {
        case '>':
          entityBalance++;
          handler.startElement (gi); //DR
          parseContent ();
          break;
        case '/':
          require ('>');
          handler.startElement (gi); //DR
          handler.endElement (gi);  //DR
          break;
      }

      // Restore the previous state.
      currentElement = oldElement;
      currentElementContent = oldElementContent;
    }


    /*
     * Parse an attribute assignment.
     * <pre>
     * [41] Attribute ::= Name Eq AttValue
     * </pre>
     * @param name The name of the attribute's element.
     * @see SaxDriver#attribute
     */
    private void parseAttribute (string name) {
      string aname;
      string type;
      string value;
      int flags = LIT_ATTRIBUTE |  LIT_ENTITY_REF;
      long attLine = line;
      long attCol = column;

      // Read the attribute name.
      aname = readNmtoken (true);
      type = getAttributeType (name, aname);

      // Parse '='
      parseEq ();

      long attValLine = line;
      long attValCol = column;

      // Read the value, normalizing whitespace
      // unless it is CDATA.
      if (type == "CDATA" || type == null) {
        value = readLiteral (flags);
      } else {
        value = readLiteral (flags | LIT_NORMALIZE);
      }

      // WFC: no duplicate attributes (if namespaces, handle in driver)
      if (!handler.namespaces)
        for (int i = 0; i < tagAttributePos; i++)
          if (aname.Equals (tagAttributes [i]))
            fatalError ("duplicate attribute", aname, null);

      // Inform the handler about the
      // attribute.
      handler.attribute (aname, value, true, false, attLine, attCol, attValLine, attValCol);
      dataBufferPos = 0;

      // Note that the attribute has been
      // specified.
      if (tagAttributePos == tagAttributes.Length) {
        string[] newAttrib = new string [tagAttributes.Length * 2];
        System.Array.Copy (tagAttributes, 0, newAttrib, 0, tagAttributePos);
        tagAttributes = newAttrib;
      }
      tagAttributes [tagAttributePos++] = aname;
    }


    /*
     * Parse an equals sign surrounded by optional whitespace.
     * <pre>
     * [25] Eq ::= S? '=' S?
     * </pre>
     */
    private void parseEq () {
      skipWhitespace ();
      require ('=');
      skipWhitespace (); 
    }
                         

    /*
     * Parse an end tag.
     * <pre>
     * [42] ETag ::= '</' Name S? '>'
     * </pre>
     * <p>NOTE: parseContent () chains to here, we already read the
     * "&lt;/".
     */
    private void parseETag () {
      if (currentElement == null) 
        fatalError("End tag encountered without start tag");
      require (currentElement);
      skipWhitespace ();
      require ('>');
      if (--entityBalance < 0) 
        fatalError ("entity begins with end tag", "", currentElement);
      handler.endElement (currentElement);

      // not re-reporting any SaxException re bogus end tags,
      // even though that diagnostic might be clearer ...
    }


    /*
     * Parse the content of an element.
     * <pre>
     * [43] content ::= (element | CharData | Reference
     *  | CDSect | PI | Comment)*
     * [67] Reference ::= EntityRef | CharRef
     * </pre>
     * <p> NOTE: consumes ETtag.
     */
    private void parseContent () {
      char c;

      // At this point any STag has completed, regardless of expansion
      // so reset the boundary to enabled
      entityBoundary = ENTITY_BOUNDARY_ENABLE;

      while (true) 
      {
        // consume characters (or ignorable whitspace) until delimiter
        parseCharData ();

        try {
          // Handle delimiters
          c = readCh ();
          // We can't switch named buffers at this point
          entityBoundary = ENTITY_BOUNDARY_DISABLE;

          switch (c) {
            case '&':    // Found "&"
              // We assume it is a general entity at first, if we see a "#" we switch
              entityBoundary = ENTITY_BOUNDARY_DISABLE_EREF;
              c = readCh ();
              if (c == '#') {
                entityBoundary = ENTITY_BOUNDARY_DISABLE_CREF;
                parseCharRef ();
              } else {
                unread (c);
                parseEntityRef (true);
              }
              break;
            case '<':    // Found "<"
              dataBufferFlush ();
              c = readCh ();
              switch (c) {
                case '!':    // Found "<!"
                  c = readCh ();
                  switch (c) {
                    case '-':   // Found "<!-"
                      entityBoundary = ENTITY_BOUNDARY_DISABLE_COMMENT;
                      require ('-');
                      parseComment ();
                      break;
                    case '[':   // Found "<!["
                      entityBoundary = ENTITY_BOUNDARY_DISABLE_CDATASECT;
                      require ("CDATA[");
                      handler.startCData ();
                      inCDATA = true;
                      parseCDSect ();
                      inCDATA = false;
                      handler.endCData ();
                      break;
                    default:
                      fatalError ("expected comment or CDATA section", c, null);
                      break;
                  }
                  break;

                case '?':   // Found "<?"
                  entityBoundary = ENTITY_BOUNDARY_DISABLE_PI;
                  parsePI ();
                  break;

                case '/':   // Found "</"
                  entityBoundary = ENTITY_BOUNDARY_DISABLE_ETAG;
                  parseETag ();
                  return;

                default:   // Found "<" followed by something else
                  unread (c);
                  entityBoundary = ENTITY_BOUNDARY_DISABLE_STAG;
                  parseElement (false);
                  break;
              }
              break;        
          }        
        } finally {
          entityBoundary = ENTITY_BOUNDARY_ENABLE;
        }
      }     
    }


    /*
     * Parse an element type declaration.
     * <pre>
     * [45] elementdecl ::= '&lt;!ELEMENT' S Name S contentspec S? '&gt;'
     * </pre>
     * <p> NOTE: the '&lt;!ELEMENT' has already been read.
     */
    private void parseElementDecl () {
      string name;

      requireWhitespace ();
      // Read the element type name.
      name = readNmtoken (true);

      requireWhitespace ();
      // Read the content model.
      parseContentspec (name);

      skipWhitespace ();
      require ('>');
    }


    /*
     * Content specification.
     * <pre>
     * [46] contentspec ::= 'EMPTY' | 'ANY' | Mixed | elements
     * </pre>
     */
    private void parseContentspec (string name) {
      handler.startContentModel(name);
      // FIXME: move elementDecl() into setElement(), pass EMTPY/ANY ...
      if (tryRead ("EMPTY")) 
      {
        handler.contentModelEmpty();
        setElement (name, CONTENT_EMPTY, null, null);
        if (!skippedPE)
          handler.getDeclHandler ().ElementDecl (name, "EMPTY");
        return;
      } else if (tryRead ("ANY")) {
        handler.contentModelAny();
        setElement (name, CONTENT_ANY, null, null);
        if (!skippedPE)
          handler.getDeclHandler ().ElementDecl (name, "ANY");
        return;
      } else {
        string model;
        char[] saved; 

        require ('(');
        saved = readBuffer;
        dataBufferAppend ('(');
        skipWhitespace ();
        handler.contentModelStartGroup();
        if (tryRead ("#PCDATA")) {
          dataBufferAppend ("#PCDATA");
          parseMixed (saved);
          model = dataBufferTostring ();
          setElement (name, CONTENT_MIXED, model, null);
        } else {
          parseElements (saved);
          model = dataBufferTostring ();
          setElement (name, CONTENT_ELEMENTS, model, null);
        }
        if (!skippedPE)
          handler.getDeclHandler ().ElementDecl (name, model);
      }
      handler.endContentModel();
    }

    /*
     * Parse an element-content model.
     * <pre>
     * [47] elements ::= (choice | seq) ('?' | '*' | '+')?
     * [49] choice ::= '(' S? cp (S? '|' S? cp)+ S? ')'
     * [50] seq ::= '(' S? cp (S? ',' S? cp)* S? ')'
     * </pre>
     *
     * <p> NOTE: the opening '(' and S have already been read.
     *
     * @param saved Buffer for entity that should have the terminal ')'
     */
    private void parseElements (char[] saved) {
      char c;
      char sep;

      // Parse the first content particle
      skipWhitespace ();
      parseCp ();

      // Check for end or for a separator.
      skipWhitespace ();
      c = readCh ();
      switch (c) {
        case ')':
          // VC: Proper Group/PE Nesting
          if (readBuffer != saved)
            handler.verror ("Illegal Group/PE nesting");

          dataBufferAppend (')');
          c = readCh ();
          switch (c) {
            case '*':
            case '+':
            case '?':
              dataBufferAppend (c);
              handler.contentModelEndGroup(c);
              break;
            default:
              handler.contentModelEndGroup('\0');
              unread (c);
              break;
          }
          return;
        case ',':    // Register the separator.
          sep = c;
          dataBufferAppend (c);
          handler.contentModelSequence();
          break;
        case '|':
          sep = c;
          dataBufferAppend (c);
          handler.contentModelChoice();
          break;
        default:
          fatalError ("bad separator in content model", c, null);
          return;
      }

      // Parse the rest of the content model.
      while (true) {
        skipWhitespace ();
        parseCp ();
        skipWhitespace ();
        c = readCh ();
        if (c == ')') {
          // VC: Proper Group/PE Nesting
          if (readBuffer != saved)
            handler.verror ("Illegal Group/PE nesting");

          dataBufferAppend (')');
          break;
        } else if (c != sep) {
          fatalError ("bad separator in content model", c, null);
          return;
        } else {
          dataBufferAppend (c);
        }
      }

      // Check for the occurrence indicator.
      c = readCh ();
      switch (c) {
        case '?':
        case '*':
        case '+':
          dataBufferAppend (c);
          handler.contentModelEndGroup (c);
          return;
        default:
          unread (c);
          handler.contentModelEndGroup ('\0');
          return;
      }
    }


    /*
     * Parse a content particle.
     * <pre>
     * [48] cp ::= (Name | choice | seq) ('?' | '*' | '+')?
     * </pre>
     */
    private void parseCp () {
      if (tryRead ('(')) {
        dataBufferAppend ('(');
        handler.contentModelStartGroup();
        parseElements (readBuffer);
      } else {
        string token = readNmtoken (true);
        dataBufferAppend (token);
        char c = readCh ();
        switch (c) {
          case '?':
          case '*':
          case '+':
            dataBufferAppend (c);
            handler.contentModelElementParticle(token, c);
            break;
          default:
            unread (c);
            handler.contentModelElementParticle(token, '\0');
            break;
        }
      }
    }


    /*
     * Parse mixed content.
     * <pre>
     * [51] Mixed ::= '(' S? ( '#PCDATA' (S? '|' S? Name)*) S? ')*'
     *       | '(' S? ('#PCDATA') S? ')'
     * </pre>
     *
     * @param saved Buffer for entity that should have the terminal ')'
     */
    private void parseMixed (char[] saved) {
      // Check for PCDATA alone.
      handler.contentModelMixed();
      skipWhitespace ();
      if (tryRead (')')) {
        // VC: Proper Group/PE Nesting
        if (readBuffer != saved)
          handler.verror ("Illegal Group/PE nesting");

        dataBufferAppend (")*");
        tryRead ('*');
        handler.contentModelEndGroup('*');
        return;
      }

      // Parse mixed content.
      skipWhitespace ();
      string token;
      while (!tryRead (')')) {
        require ('|');
        dataBufferAppend ('|');
        skipWhitespace ();
        token = readNmtoken (true);
        dataBufferAppend (token);
        skipWhitespace ();
        handler.contentModelElementParticle(token, '\0');
      }

      // VC: Proper Group/PE Nesting
      if (readBuffer != saved)
        handler.verror ("Illegal Group/PE nesting");

      require ('*');
      dataBufferAppend (")*");
      handler.contentModelEndGroup('*');
    }


    /*
     * Parse an attribute list declaration.
     * <pre>
     * [52] AttlistDecl ::= '&lt;!ATTLIST' S Name AttDef* S? '&gt;'
     * </pre>
     * <p>NOTE: the '&lt;!ATTLIST' has already been read.
     */
    private void parseAttlistDecl () {
      string elementName;

      requireWhitespace ();
      elementName = readNmtoken (true);
      bool white = tryWhitespace ();
      while (!tryRead ('>')) {
        if (!white)
          fatalError ("whitespace required before attribute definition");
        parseAttDef (elementName);
        white = tryWhitespace ();
      }
    }


    /*
     * Parse a single attribute definition.
     * <pre>
     * [53] AttDef ::= S Name S AttType S DefaultDecl
     * </pre>
     */
    private void parseAttDef (string elementName) {
      string name;
      string type;
      string enumVal = null;

      // Read the attribute name.
      name = readNmtoken (true);

      // Read the attribute type.
      requireWhitespace ();
      type = readAttType ();

      // Get the string of enumerated values if necessary.
      if ("ENUMERATION" == type || "NOTATION" == type)
        enumVal = dataBufferTostring ();

      // Read the default value.
      requireWhitespace ();
      parseDefault (elementName, name, type, enumVal);
    }


    /*
     * Parse the attribute type.
     * <pre>
     * [54] AttType ::= stringType | TokenizedType | EnumeratedType
     * [55] stringType ::= 'CDATA'
     * [56] TokenizedType ::= 'ID' | 'IDREF' | 'IDREFS' | 'ENTITY'
     *  | 'ENTITIES' | 'NMTOKEN' | 'NMTOKENS'
     * [57] EnumeratedType ::= NotationType | Enumeration
     * </pre>
     */
    private string readAttType () {
      if (tryRead ('(')) {
        parseEnumeration (false);
        return "ENUMERATION";
      } else {
        string typestring = readNmtoken (true);
        if ("NOTATION" == typestring) {
          parseNotationType ();
          return typestring;
        } else if ("CDATA" == typestring
          || "ID" == typestring
          || "IDREF" == typestring
          || "IDREFS" == typestring
          || "ENTITY" == typestring
          || "ENTITIES" == typestring
          || "NMTOKEN" == typestring
          || "NMTOKENS" == typestring)
          return typestring;
        fatalError ("illegal attribute type", typestring, null);
        return null;
      }
    }


    /*
     * Parse an enumeration.
     * <pre>
     * [59] Enumeration ::= '(' S? Nmtoken (S? '|' S? Nmtoken)* S? ')'
     * </pre>
     * <p>NOTE: the '(' has already been read.
     */
    private void parseEnumeration (bool isNames) {
      dataBufferAppend ('(');

      // Read the first token.
      skipWhitespace ();
      dataBufferAppend (readNmtoken (isNames));
      // Read the remaining tokens.
      skipWhitespace ();
      while (!tryRead (')')) {
        require ('|');
        dataBufferAppend ('|');
        skipWhitespace ();
        dataBufferAppend (readNmtoken (isNames));
        skipWhitespace ();
      }
      dataBufferAppend (')');
    }


    /*
     * Parse a notation type for an attribute.
     * <pre>
     * [58] NotationType ::= 'NOTATION' S '(' S? NameNtoks
     *  (S? '|' S? name)* S? ')'
     * </pre>
     * <p>NOTE: the 'NOTATION' has already been read
     */
    private void parseNotationType () {
      requireWhitespace ();
      require ('(');

      parseEnumeration (true);
    }


    /*
     * Parse the default value for an attribute.
     * <pre>
     * [60] DefaultDecl ::= '#REQUIRED' | '#IMPLIED'
     *  | (('#FIXED' S)? AttValue)
     * </pre>
     */
    private void parseDefault (string elementName, string name, string type, string enumVal) {
      int valueType = ATTRIBUTE_DEFAULT_SPECIFIED;
      string val = null;
      int flags = LIT_ATTRIBUTE;
      bool saved = expandPE;
      string defaultType = null;

      // LIT_ATTRIBUTE forces '<' checks now (ASAP) and turns whitespace
      // chars to spaces (doesn't matter when that's done if it doesn't
      // interfere with char refs expanding to whitespace).

      if (!skippedPE) {
        flags |= LIT_ENTITY_REF;
        if ("CDATA" != type)
          flags |= LIT_NORMALIZE;
      }

      expandPE = false;
      if (tryRead ('#')) {
        if (tryRead ("FIXED")) {
          defaultType = "#FIXED";
          valueType = ATTRIBUTE_DEFAULT_FIXED;
          requireWhitespace ();
          val = readLiteral (flags);
        } else if (tryRead ("REQUIRED")) {
          defaultType = "#REQUIRED";
          valueType = ATTRIBUTE_DEFAULT_REQUIRED;
        } else if (tryRead ("IMPLIED")) {
          defaultType = "#IMPLIED";
          valueType = ATTRIBUTE_DEFAULT_IMPLIED;
        } else {
          fatalError ("illegal keyword for attribute default value");
        }
      } else
        val = readLiteral (flags);
      expandPE = saved;
      setAttribute (elementName, name, type, enumVal, val, valueType);
      if ("ENUMERATION" == type)
        type = enumVal;
      else if ("NOTATION" == type)
        type = "NOTATION " + enumVal;
      if (!skippedPE) handler.getDeclHandler ().AttributeDecl (elementName, name, type, defaultType, val);
    }


    /*
     * Parse a conditional section.
     * <pre>
     * [61] conditionalSect ::= includeSect || ignoreSect
     * [62] includeSect ::= '&lt;![' S? 'INCLUDE' S? '['
     *  extSubsetDecl ']]&gt;'
     * [63] ignoreSect ::= '&lt;![' S? 'IGNORE' S? '['
     *  ignoreSectContents* ']]&gt;'
     * [64] ignoreSectContents ::= Ignore
     *  ('&lt;![' ignoreSectContents* ']]&gt;' Ignore )*
     * [65] Ignore ::= Char* - (Char* ( '&lt;![' | ']]&gt;') Char* )
     * </pre>
     * <p> NOTE: the '&gt;![' has already been read.
     */
    private void parseConditionalSect (char[] saved) {
      skipWhitespace ();
      if (tryRead ("INCLUDE")) {
        skipWhitespace ();
        require ('[');
        // VC: Proper Conditional Section/PE Nesting
        if (readBuffer != saved)
          handler.verror ("Illegal Conditional Section/PE nesting");
        skipWhitespace ();
        while (!tryRead ("]]>")) {
          parseMarkupdecl ();
          skipWhitespace ();
        }
      } 
      else if (tryRead ("IGNORE")) 
      {
        skipWhitespace ();
        require ('[');
        // VC: Proper Conditional Section/PE Nesting
        if (readBuffer != saved)
          handler.verror ("Illegal Conditional Section/PE nesting");
        char c;
        expandPE = false;
        for (int nest = 1; nest > 0;) {
          c = readCh ();
          switch (c) {
            case '<':
              if (tryRead ("![")) {
                nest++;
              }
              goto case ']';
            case ']':
              if (tryRead ("]>")) {
                nest--;
              }
              break;
          }
        }
        expandPE = true;
      } else {
        fatalError ("conditional section must begin with INCLUDE or IGNORE");
      }
    }


    /*
     * Read and interpret a character reference.
     * <pre>
     * [66] CharRef ::= '&#' [0-9]+ ';' | '&#x' [0-9a-fA-F]+ ';'
     * </pre>
     * <p>NOTE: the '&#' has already been read.
     */
    private void parseCharRef () {
      int value = 0;
      char c;

      if (tryRead ('x')) {
        bool breakLoop1 = false;
        while (true) {
          c = readCh ();
          int n = 0;
          switch (c) {
            case '0': case '1': case '2': case '3': case '4':
            case '5': case '6': case '7': case '8': case '9':
              n = c - '0';
              break;
            case 'a': case 'b': case 'c': case 'd': case 'e': case 'f':
              n = (c - 'a') + 10;
              break;
            case 'A': case 'B': case 'C': case 'D': case 'E': case 'F':
              n = (c - 'A') + 10;
              break;
            case ';':
              breakLoop1 = true;
              break;
            default:
              fatalError ("illegal character in character reference", c, null);
              breakLoop1 = true;
              break;
          }
          if (breakLoop1)
            break;
          value *= 16;
          value += n;
        }
      } else {
        bool breakLoop2 = false;
        while (true) {
          c = readCh ();
          switch (c) {
            case '0': case '1': case '2': case '3': case '4':
            case '5': case '6': case '7': case '8': case '9':
              value *= 10;
              value += c - '0';
              break;
            case ';':
              breakLoop2 = true;
              break;
            default:
              fatalError ("illegal character in character reference", c, null);
              breakLoop2 = true;
              break;
          }
          if (breakLoop2)
            break;
        }
      }

      // We have finished reading the reference
      entityBoundary = ENTITY_BOUNDARY_ENABLE;

      // check for character refs being legal Xml
      if ((value < 0x0020
        && ! (value == '\n' || value == '\t' || value == '\r'))
        || (value >= 0xD800 && value <= 0xDFFF)
        || value == 0xFFFE || value == 0xFFFF
        || value > 0x0010ffff)
        fatalError ("illegal Xml character reference U+"
          + ((int)value).ToString("X4"));

      // Check for surrogates: 00000000 0000xxxx yyyyyyyy zzzzzzzz
      //  (1101|10xx|xxyy|yyyy + 1101|11yy|zzzz|zzzz:
      if (value <= 0x0000ffff) {
        // no surrogates needed
        dataBufferAppend ((char) value);
      } else if (value <= 0x0010ffff) {
        value -= 0x10000;
        // > 16 bits, surrogate needed
        dataBufferAppend ((char) (0xd800 | (value >> 10)));
        dataBufferAppend ((char) (0xdc00 | (value & 0x0003ff)));
      } else {
        // too big for surrogate
        fatalError ("character reference " + value + " is too large for UTF-16",
          ((int)value).ToString (), null);
      }
      dataBufferFlush ();
    }


    /*
     * Parse and expand an entity reference.
     * <pre>
     * [68] EntityRef ::= '&' Name ';'
     * </pre>
     * <p>NOTE: the '&amp;' has already been read.
     * @param externalAllowed External entities are allowed here.
     */
    private void parseEntityRef (bool externalAllowed) {
      string name;

      // Read the name and closing ";"
      name = readNmtoken (true);
      require (';');

      // We have finished reading the reference
      entityBoundary = ENTITY_BOUNDARY_ENABLE;


      switch (getEntityType (name)) 
      {
        case ENTITY_UNDECLARED:
          // NOTE:  Xml REC describes amazingly convoluted handling for
          // this case.  Nothing as meaningful as being a WFness error
          // unless the processor might _legitimately_ not have seen a
          // declaration ... which is what this implements.
          string message = "reference to undeclared general entity " + name;
          
          //if (skippedPE && !docIsStandalone) //(hasExtSubset or ExtPE) 
          if (hasExtEntity && !docIsStandalone)
          {
            handler.verror (message);
            // we don't know this entity, and it might be external...
            if (externalAllowed)
              handler.skippedEntity (name);
          } 
          else
            fatalError (message);
          break;
        case ENTITY_INTERNAL:
          //OPTIMIZE too many lookups in the entity table
          if (docIsStandalone && getEntityDeclaredExternal (name))
            fatalError("reference to externally declared entity \"" + name + "\" when document is declared standalone");
          pushstring (name, getEntityValue (name));
          break;
        case ENTITY_TEXT:
          //OPTIMIZE too many lookups in the entity table
          if (docIsStandalone && getEntityDeclaredExternal (name))
            fatalError("reference to externally declared entity \"" + name + "\" when document is declared standalone");
          if (externalAllowed) 
          {
            pushURL (false, name, getEntityIds (name),
              null, null, null, true);
          } else {
            fatalError ("reference to external entity in attribute value.",
              name, null);
          }
          break;
        case ENTITY_NDATA:
          if (externalAllowed) {
            fatalError ("unparsed entity reference in content", name, null);
          } else {
            fatalError ("reference to external entity in attribute value.",
              name, null);
          }
          break;
        default:
          // .NET changed to error from encoding....
          fatalError ("unknown entity type for entity '" + name + "'");
          break;
      }
    }


    /*
     * Parse and expand a parameter entity reference.
     * <pre>
     * [69] PEReference ::= '%' Name ';'
     * </pre>
     * <p>NOTE: the '%' has already been read.
     */
    private void parsePEReference () {
      string name;

      name = "%" + readNmtoken (true);
      require (';');
      switch (getEntityType (name)) 
      {
        case ENTITY_UNDECLARED:
          // VC: Entity Declared
          handler.verror ("reference to undeclared parameter entity " + name);

          // we should disable handling of all subsequent declarations
          // unless this is a standalone document (info discarded)
          break;
        case ENTITY_INTERNAL:
          if (inLiteral)
            pushstring (name, getEntityValue (name));
          else
            pushstring (name, ' ' + getEntityValue (name) + ' ');
      
          break;
        case ENTITY_TEXT:
          if (!inLiteral)
            pushstring (null, " ");
          pushURL (true, name, getEntityIds (name), null, null, null, true);
          
          if (!inLiteral)
            pushstring (null, " ");
          break;
      }
    }

    /*
     * Parse an entity declaration.
     * <pre>
     * [70] EntityDecl ::= GEDecl | PEDecl
     * [71] GEDecl ::= '&lt;!ENTITY' S Name S EntityDef S? '&gt;'
     * [72] PEDecl ::= '&lt;!ENTITY' S '%' S Name S PEDef S? '&gt;'
     * [73] EntityDef ::= EntityValue | (ExternalID NDataDecl?)
     * [74] PEDef ::= EntityValue | ExternalID
     * [75] ExternalID ::= 'SYSTEM' S SystemLiteral
     *     | 'PUBLIC' S PubidLiteral S SystemLiteral
     * [76] NDataDecl ::= S 'NDATA' S Name
     * </pre>
     * <p>NOTE: the '&lt;!ENTITY' has already been read.
     */
    private void parseEntityDecl () {
      bool peFlag = false;

      // Check for a parameter entity.
      expandPE = false;
      requireWhitespace ();
      if (tryRead ('%')) {
        peFlag = true;
        requireWhitespace ();
      }
      expandPE = true;

      // Read the entity name, and prepend
      // '%' if necessary.
      bool savedAllowColon = allowColon;
      allowColon = false;
      string name = readNmtoken (true);
      allowColon = savedAllowColon;

      if (peFlag) {
        name = "%" + name;
      }

      // Read the entity value.
      requireWhitespace ();
      char c = readCh ();
      unread (c);
      if (c == '"' || c == '\'') {
        // Internal entity ... replacement text has expanded refs
        // to characters and PEs, but not to general entities
        string value = readLiteral (0);
        setInternalEntity (name, value);
      } else {
        // Read the external IDs
        string[] ids = readExternalIds (false, false);

        // Check for NDATA declaration.
        bool white = tryWhitespace ();
        if (!peFlag && tryRead ("NDATA")) {
          if (!white)
            fatalError ("whitespace required before NDATA");
          requireWhitespace ();
          string notationName = readNmtoken (true);
          if (!skippedPE) {
            setExternalEntity (name, ENTITY_NDATA, ids, notationName);
            handler.unparsedEntityDecl (name, ids, notationName);
          }
        } else if (!skippedPE) {
          setExternalEntity (name, ENTITY_TEXT, ids, null);
          handler.getDeclHandler ().ExternalEntityDecl (name, ids [0],
            handler.resolveURIs ()
            // FIXME: ASSUMES not skipped
            // "false" forces error on bad URI
            ? handler.absolutize (ids [2], ids [1], false)
            : ids [1]);
        }
      }

      // Finish the declaration.
      skipWhitespace ();
      require ('>');
    }


    /*
     * Parse a notation declaration.
     * <pre>
     * [82] NotationDecl ::= '&lt;!NOTATION' S Name S
     *  (ExternalID | PublicID) S? '&gt;'
     * [83] PublicID ::= 'PUBLIC' S PubidLiteral
     * </pre>
     * <P>NOTE: the '&lt;!NOTATION' has already been read.
     */
    private void parseNotationDecl () {
      string nname;
      string[] ids;


      requireWhitespace ();
      bool savedAllowColon = allowColon;
      allowColon = false;
      nname = readNmtoken (true);
      allowColon = savedAllowColon;

      requireWhitespace ();

      // Read the external identifiers.
      ids = readExternalIds (true, false);

      // Register the notation.
      setNotation (nname, ids);

      skipWhitespace ();
      require ('>');
    }


    /*
     * Parse character data.
     * <pre>
     * [14] CharData ::= [^&lt;&amp;]* - ([^&lt;&amp;]* ']]&gt;' [^&lt;&amp;]*)
     * </pre>
     */
    private void parseCharData () {
      char c;
      int state = 0;
      bool pureWhite = false;

      // assert (dataBufferPos == 0);

      // are we expecting pure whitespace?  it might be dirty...
      //FIXME .NET == removed semi colon from null test in following line...
      if (currentElementContent == CONTENT_ELEMENTS)
        pureWhite = true;

      // always report right out of readBuffer
      // to minimize (pointless) buffer copies
      while (true) {
        int lineAugment = 0;
        int columnAugment = 0;
        int i;

        bool breakLoop = false;
        for (i = readBufferPos; i < readBufferLength; i++) {
          switch (c = readBuffer [i]) {
            case '\n':
              lineAugment++;
              columnAugment = 0;
              // pureWhite unmodified
              break;
            case '\r': // should not happen!!
            case '\t':
            case ' ':
              // pureWhite unmodified
              columnAugment++;
              break;
            case '&':
            case '<':
              columnAugment++;
              // pureWhite unmodified
              // CLEAN end of text sequence
              state = 1;
              breakLoop = true;
              break;
            case ']':
              // that's not a whitespace char, and
              // can not terminate pure whitespace either
              pureWhite = false;
              if ((i + 2) < readBufferLength) {
                if (readBuffer [i + 1] == ']'
                  && readBuffer [i + 2] == '>') {
                  // ERROR end of text sequence
                  state = 2;
                  breakLoop = true;
                  break;
                }
              } else {
                // FIXME missing two end-of-buffer cases
              }
              columnAugment++;
              break;
            default:
              if (c < 0x0020 || c > 0xFFFD)
                fatalError ("illegal Xml character U+"
                  + ((int)c).ToString("X4"));
              // that's not a whitespace char
              pureWhite = false;
              columnAugment++;
              break;
          }
          if (breakLoop)
            break;
        }

        // report text thus far
        if (lineAugment > 0) {
          line += lineAugment;
          column = columnAugment;
        } else {
          column += columnAugment;
        }

        // report characters/whitspace
        int length = i - readBufferPos;

        if (length != 0) {
          if (pureWhite)
            handler.ignorableWhitespace (readBuffer,
              readBufferPos, length);
          else
            handler.charData (readBuffer, readBufferPos, length);
          readBufferPos = i;
        }
     
        if (state != 0)
          break;

        // fill next buffer from this entity, or
        // pop stack and continue with previous entity
        unread (readCh ());
      }

      // finish, maybe with error
      if (state != 1) // finish, no error
        fatalError ("character data may not contain ']]>'");
    }


    //////////////////////////////////////////////////////////////////////
    // High-level reading and scanning methods.
    //////////////////////////////////////////////////////////////////////

    /*
     * Require whitespace characters.
     */
    private void requireWhitespace () {
      char c = readCh ();
      if (isWhitespace (c)) {
        skipWhitespace ();
      } else {
        fatalError ("whitespace required", c, null);
      }
    }


    /*
     * Skip whitespace characters.
     * <pre>
     * [3] S ::= (#x20 | #x9 | #xd | #xa)+
     * </pre>
     */
    private void skipWhitespace () {
      // Start with a little cheat.  Most of
      // the time, the white space will fall
      // within the current read buffer; if
      // not, then fall through.
      if (USE_CHEATS) {
        int lineAugment = 0;
        int columnAugment = 0;

        bool breakLoop = false;
        for (int i = readBufferPos; i < readBufferLength; i++) {
          switch (readBuffer [i]) {
            case ' ':
            case '\t':
            case '\r':
              columnAugment++;
              break;
            case '\n':
              lineAugment++;
              columnAugment = 0;
              break;
            case '%':
              if (expandPE) {
                breakLoop = true;
                break;
              }
              // else fall through...
              goto default;
            default:
              readBufferPos = i;
              if (lineAugment > 0) {
                line += lineAugment;
                column = columnAugment;
              } else {
                column += columnAugment;
              }
              return;
          }
          if (breakLoop)
            break;
        }
      }

      // OK, do it the slow way.
      char c = readCh ();
      while (isWhitespace (c)) {
        c = readCh ();
      }
      unread (c);
    }


    /*
     * Read a name or (when parsing an enumeration) name token.
     * <pre>
     * [5] Name ::= (Letter | '_' | ':') (NameChar)*
     * [7] Nmtoken ::= (NameChar)+
     * </pre>
     */
    private string readNmtoken (bool isName) {
      char c;
      int colonCount = 0;

      if (USE_CHEATS) {
        bool breakLoop = false;
        for (int i = readBufferPos; i < readBufferLength; i++) {
          c = readBuffer [i];
          switch (c) {
            case '%':
              if (expandPE) {
                breakLoop = true;
                break;
              }
              // else fall through...
              goto case '<';

              // What may legitimately come AFTER a name/nmtoken?
            case '<': case '>': case '&':
            case ',': case '|': case '*': case '+': case '?':
            case ')':
            case '=':
            case '\'': case '"':
            case '[':
            case ' ': case '\t': case '\r': case '\n':
            case ';':
            case '/':
              int start = readBufferPos;
              if (i == start)
                fatalError ("name expected", readBuffer [i], null);
              readBufferPos = i;
              return intern (readBuffer, start, i - start);

            default:
              // FIXME ... per IBM's OASIS test submission, these:
              //   ?  U+06dd 
              // REJECT
              //   BaseChar U+0132 U+0133 U+013F U+0140 U+0149 U+017F U+01C4 U+01CC
              //  U+01F1 U+01F3 U+0E46 U+1011 U+1104 U+1108 U+110A U+110D
              //  U+113B U+113F U+1141 U+114D U+114F U+1151 U+1156 U+1162
              //  U+1164 U+1166 U+116B U+116F U+1174 U+119F U+11AC U+11B6
              //  U+11B9 U+11BB U+11C3 U+11F1 U+212F U+0587
              //   Combining U+309B

              //OPTIMIZE Name checks are not necessary for entity names (they must be declared)
              //OPTIMIZE Name checks are not necessary for xml elements and attributes in content if validating 
              if ((i == readBufferPos) && (isName)) 
              {
                if (c == ':') 
                {
                  if (!handler.xmlNames)
                    fatalError("names cannot begin with a colon character ':'");
                }
                else if ((!XmlUtils.isXmlLetter(c)) && (c != '_')) 
                {
                  fatalError("not a name start character, U+" + ((int)c).ToString("X4"));
                }
              } 
              else if (c == ':')
              {
                if (!allowColon)
                  fatalError("not a name character, U+" + ((int)c).ToString("X4"));
                if (colonCount != 0) 
                {
                  if (!handler.xmlNames)
                    fatalError("names cannot contain multiple colons");
                }
                else
                  colonCount++;
              }
              else if ((!XmlUtils.isXmlLetter(c)) && (!XmlUtils.isXmlDigit(c)) && 
                 (c != '.') && (c != '-') && (c != '_')  &&
                (!XmlUtils.isXmlCombiningChar(c)) && (!XmlUtils.isXmlExtender(c))) {
                fatalError("not a name character, U+" + ((int)c).ToString("X4"));
              }
              break;
          }
          if (breakLoop)
            break;
        }
      }

      nameBufferPos = 0;

      // Read the first character.
      while (true) {
        c = readCh ();
        switch (c) {
          case '%':
          case '<': case '>': case '&':
          case ',': case '|': case '*': case '+': case '?':
          case ')':
          case '=':
          case '\'': case '"':
          case '[':
          case ' ': case '\t': case '\n': case '\r':
          case ';':
          case '/':
            unread (c);
            if (nameBufferPos == 0) {
              fatalError ("name expected");
            }
            if ((isName) && (!XmlUtils.isXmlLetter(nameBuffer[0])) && (nameBuffer[0] != ':' || !allowColon) && 
                (nameBuffer[0] != '_')) 
              fatalError ("Not a name start character, U+"
                + ((int)nameBuffer [0]).ToString("X4"));
            string s = intern (nameBuffer, 0, nameBufferPos);
            nameBufferPos = 0;
            return s;
          default:
            if (((nameBufferPos != 0) || (!isName)) && (!XmlUtils.isXmlNameChar(c))) 
              fatalError ("Not a name character, U+" + ((int)c).ToString("X4"));
            if (nameBufferPos >= nameBuffer.Length)
              nameBuffer =
                extendCharArray (nameBuffer, nameBuffer.Length, nameBufferPos);
            nameBuffer [nameBufferPos++] = c;
            break;
        }
      }
    }

    /*
     * Read a literal.  With matching single or double quotes as
     * delimiters (and not embedded!) this is used to parse:
     * <pre>
     * [9] EntityValue ::= ... ([^%&amp;] | PEReference | Reference)* ...
     * [10] AttValue ::= ... ([^<&] | Reference)* ...
     * [11] SystemLiteral ::= ... (URLchar - "'")* ...
     * [12] PubidLiteral ::= ... (PubidChar - "'")* ...
     * </pre>
     * as well as the quoted strings in Xml and text declarations
     * (for version, encoding, and standalone) which have their
     * own constraints.
     */
    private string readLiteral (int flags) {
      char delim, c;
      long startLine = line;
      bool saved = expandPE;
      bool savedReport = doReport;

      // Find the first delimiter.
      delim = readCh ();
      if (delim != '"' && delim != '\'') {
        fatalError ("expected '\"' or \"'\"", delim, null);
        return null;
      }
      inLiteral = true;
      if ((flags & LIT_DISABLE_PE) != 0)
        expandPE = false;
      doReport = false;

      // Each level of input source has its own buffer; remember
      // ours, so we won't read the ending delimiter from any
      // other input source, regardless of entity processing.
      char[] ourBuf = readBuffer;

      // Read the literal.
      try {
        c = readCh ();
        bool continueLoop = false;
        while (! (c == delim && readBuffer == ourBuf)) {
          switch (c) {
            // attributes and public ids are normalized
            // in almost the same ways
            case '\n':
            case '\r':
              if ((flags & (LIT_ATTRIBUTE | LIT_PUBID)) != 0)
                c = ' ';
              break;
            case '\t':
              if ((flags & LIT_ATTRIBUTE) != 0)
                c = ' ';
              break;
            case '&':
              c = readCh ();
              // Char refs are expanded immediately, except for
              // all the cases where it's deferred.
              if (c == '#') {
                if ((flags & LIT_DISABLE_CREF) != 0) {
                  dataBufferAppend ('&');
                  //FIXME, do we need to add a # here also?
                  break;
                }
                parseCharRef ();

                // exotic WFness risk: this is an entity literal,
                // dataBuffer [dataBufferPos - 1] == '&', and
                // following chars are a _partial_ entity/char ref

                // It looks like an entity ref ...
              } else {
                unread (c);
                // Expand it?
                if ((flags & LIT_ENTITY_REF) > 0) {
                  parseEntityRef (false);

                  // Is it just data?
                } else if ((flags & LIT_DISABLE_EREF) != 0) {
                  dataBufferAppend ('&');

                  // OK, it will be an entity ref -- expanded later.
                } else {
                  string name = readNmtoken (true);
                  require (';');
                  dataBufferAppend ('&');
                  dataBufferAppend (name);
                  dataBufferAppend (';');
                }
              }
              c = readCh ();
              continueLoop = true;
              break;

            case '<':
              // and why?  Perhaps so "&foo;" expands the same
              // inside and outside an attribute?
              if ((flags & LIT_ATTRIBUTE) != 0)
                fatalError ("attribute values may not contain '<'");
              break;

              // We don't worry about case '%' and PE refs, readCh does.

            default:
              break;
          }
          if (continueLoop) {
            continueLoop = false;
            continue;
          }
          dataBufferAppend (c);
          c = readCh ();
        }
      } catch (AElfredEofException) {
        fatalError ("end of input while looking for delimiter (started on line "
          + startLine + ')', null, delim.ToString());
      }
      inLiteral = false;
      expandPE = saved;
      doReport = savedReport;

      // Normalise whitespace if necessary.
      if ((flags & LIT_NORMALIZE) > 0) {
        dataBufferNormalize ();
      }

      // Return the value.
      return dataBufferTostring ();
    }


    /*
     * Try reading external identifiers.
     * A system identifier is not required for notations.
     * @param inNotation Are we parsing a notation decl?
     * @param isSubset Parsing external subset decl (may be omitted)?
     * @return A three-member string array containing the identifiers,
     * or nulls. Order: public, system, baseURI.
     */
    private string[] readExternalIds (bool inNotation, bool isSubset) {
      char c;
      string[] ids = new string [3];
      int flags = LIT_DISABLE_CREF | LIT_DISABLE_PE | LIT_DISABLE_EREF;

      if (tryRead ("PUBLIC")) {
        requireWhitespace ();
        ids [0] = readLiteral (LIT_NORMALIZE | LIT_PUBID | flags);
        if (inNotation) {
          skipWhitespace ();
          c = readCh ();
          unread (c);
          if (c == '"' || c == '\'') {
            ids [1] = readLiteral (flags);
          }
        } else {
          requireWhitespace ();
          ids [1] = readLiteral (flags);
        }

        for (int i = 0; i < ids [0].Length; i++) {
          c = ids [0][i];
          if (c >= 'a' && c <= 'z')
            continue;
          if (c >= 'A' && c <= 'Z')
            continue;
          if (" \r\n0123456789-' ()+,./:=?;!*#@$_%".IndexOf (c) != -1)
            continue;
          fatalError ("illegal PUBLIC id character U+"
            + ((int)c).ToString("X4"));
        }
      } else if (tryRead ("SYSTEM")) {
        requireWhitespace ();
        ids [1] = readLiteral (flags);
      } else if (!isSubset) 
        fatalError ("missing SYSTEM or PUBLIC keyword");

      if (ids [1] != null) {
        if (ids [1].IndexOf ('#') != -1)
          handler.verror ("SYSTEM id has a URI fragment: " + ids [1]);
        ids [2] = handler.SystemId;
        if (ids [2] == null)
          handler.warn ("No base URI; hope URI is absolute: "
            + ids [1]);
      }

      return ids;
    }


    /*
     * Test if a character is whitespace.
     * <pre>
     * [3] S ::= (#x20 | #x9 | #xd | #xa)+
     * </pre>
     * @param c The character to test.
     * @return true if the character is whitespace.
     */
    private bool isWhitespace (char c) {
      if (c > 0x20)
        return false;
      if (c == 0x20 || c == 0x0a || c == 0x09 || c == 0x0d)
        return true;
      return false; // illegal ...
    }

    //////////////////////////////////////////////////////////////////////
    // Utility routines.
    //////////////////////////////////////////////////////////////////////


    /*
     * Add a character to the data buffer.
     */
    private void dataBufferAppend (char c) {    
      // Expand buffer if necessary.
      if (dataBufferPos >= dataBuffer.Length)
        dataBuffer =
          extendCharArray (dataBuffer, dataBuffer.Length, dataBufferPos);
      dataBuffer [dataBufferPos++] = c;
    }


    /*
     * Add a string to the data buffer.
     */
    private void dataBufferAppend (string s) {
      dataBufferAppend (s.ToCharArray (), 0, s.Length);
    }


    /*
     * Append (part of) a character array to the data buffer.
     */
    private void dataBufferAppend (char[] ch, int start, int length) {
      dataBuffer = extendCharArray (dataBuffer, dataBuffer.Length,
        dataBufferPos + length);

      System.Array.Copy (ch, start, dataBuffer, dataBufferPos, length);
      dataBufferPos += length;
    }


    /*
     * Normalise space characters in the data buffer.
     */
    private void dataBufferNormalize () {
      int i = 0;
      int j = 0;
      int end = dataBufferPos;

      // Skip spaces at the start.
      while (j < end && dataBuffer [j] == ' ') {
        j++;
      }

      // Skip whitespace at the end.
      while (end > j && dataBuffer [end - 1] == ' ') {
        end --;
      }

      // Start copying to the left.
      while (j < end) {

        char c = dataBuffer [j++];

        // Normalise all other spaces to
        // a single space.
        if (c == ' ') {
          while (j < end && dataBuffer [j++] == ' ')
            continue;
          dataBuffer [i++] = ' ';
          dataBuffer [i++] = dataBuffer [j - 1];
        } else {
          dataBuffer [i++] = c;
        }
      }

      // The new length is <= the old one.
      dataBufferPos = i;
    }


    /*
     * Convert the data buffer to a string.
     */
    private string dataBufferTostring () {
      string s = new string (dataBuffer, 0, dataBufferPos);
      dataBufferPos = 0;
      return s;
    }


    /*
     * Flush the contents of the data buffer to the handler, as
     * appropriate, and reset the buffer for new input.
     */
    private void dataBufferFlush () {
      if (currentElementContent == CONTENT_ELEMENTS
        && dataBufferPos > 0
        && !inCDATA
        ) {
        // We can't just trust the buffer to be whitespace, there
        // are (error) cases when it isn't
        for (int i = 0; i < dataBufferPos; i++) {
          if (!isWhitespace (dataBuffer [i])) {
            handler.charData (dataBuffer, 0, dataBufferPos);
            dataBufferPos = 0;
          }
        }
        if (dataBufferPos > 0) {
          handler.ignorableWhitespace (dataBuffer, 0, dataBufferPos);
          dataBufferPos = 0;
        }
      } else if (dataBufferPos > 0) {
        //!! Not sure about checking inLiteral, was causing spurious charData 
        //!! events inentityLiteral parsing
        if (!inLiteral && currentElement != null) 
        {
          handler.charData (dataBuffer, 0, dataBufferPos);
          dataBufferPos = 0;       
        }
      }
    }


    /*
     * Require a string to appear, or throw an exception.
     * <p><em>Precondition:</em> Entity expansion is not required.
     * <p><em>Precondition:</em> data buffer has no characters that
     * will get sent to the application.
     */
    private void require (string delim) {
      int length = delim.Length;
      char[] ch;
  
      if (length < dataBuffer.Length) {
        ch = dataBuffer;
        delim.CopyTo (0, ch, 0, length);
      } else
        ch = delim.ToCharArray ();

      if (USE_CHEATS
        && length <= (readBufferLength - readBufferPos)) {
        int offset = readBufferPos;

        for (int i = 0; i < length; i++, offset++)
          if (ch [i] != readBuffer [offset])
            fatalError ("required string", null, delim);
        readBufferPos = offset;
     
      } else {
        for (int i = 0; i < length; i++)
          require (ch [i]);
      }
    }


    /*
     * Require a character to appear, or throw an exception.
     */
    private void require (char delim) {
      char c = readCh ();

      if (c != delim) {
        fatalError ("required character", c, delim.ToString());
      }
    }


    /*
     * Create an interned string from a character array.
     * &AElig;lfred uses this method to create an interned version
     * of all names and name tokens, so that it can test equality
     * with <code>==</code> instead of <code>string.equals ()</code>.
     *
     * <p>This is much more efficient than constructing a non-interned
     * string first, and then interning it.
     *
     * @param ch an array of characters for building the string.
     * @param start the starting position in the array.
     * @param length the number of characters to place in the string.
     * @return an interned string.
     * @see #intern (string)
     * @see java.lang.string#intern
     */
    public string intern (char[] ch, int start, int length) {
      int index = 0;
      int hash = 0;
      object[] bucket;

      // Generate a hash code.  This is a widely used string hash,
      // often attributed to Brian Kernighan.
      for (int i = start; i < start + length; i++)
        hash = 31 * hash + ch [i];
      hash = (hash & 0x7fffffff) % SYMBOL_TABLE_LENGTH;

      // Get the bucket -- consists of {array,string} pairs
      if ((bucket = symbolTable [hash]) == null) {
        // first string in this bucket
        bucket = new object [8];

        // Search for a matching tuple, and
        // return the string if we find one.
      } else {
        while (index < bucket.Length) {
          char[] chFound = (char []) bucket [index];

          // Stop when we hit an empty entry.
          if (chFound == null)
            break;

          // If they're the same length, check for a match.
          if (chFound.Length == length) {
            for (int i = 0; i < chFound.Length; i++) {
              // continue search on failure
              if (ch [start + i] != chFound [i]) {
                break;
              } else if (i == length - 1) {
                // That's it, we have a match!
                return (string) bucket [index + 1];
              }
            }
          }
          index += 2;
        }
        // Not found -- we'll have to add it.

        // Do we have to grow the bucket?
        bucket = extendObjectArray (bucket, bucket.Length, index);
      }
      symbolTable [hash] = bucket;

      // OK, add it to the end of the bucket -- "local" interning.
      // Intern "globally" to let applications share interning benefits.
      // That is, "!=" and "==" work on our strings, not just equals().
      string s = string.Intern (new string(ch, start, length));
      bucket [index] = s.ToCharArray ();
      bucket [index + 1] = s;
      return s;
    }

    /*
     * Ensure the capacity of an object array, allocating a new one if
     * necessary.  Usually extends only for name hash collisions. 
     */
    private object[] extendObjectArray (object[] array, int currentSize, int requiredSize) {
      if (requiredSize < currentSize) {
        return array;
      } else {
        object[] newArray = null;
        int newSize = currentSize * 2;

        if (newSize <= requiredSize)
          newSize = requiredSize + 1;

        newArray = new object [newSize];
        System.Array.Copy (array, 0, newArray, 0, currentSize);
        return newArray;
      }
    }

    /*
         * Ensure the capacity of a character array, allocating a new one if
         * necessary.  Usually extends only for name hash collisions. 
         */
    private char[] extendCharArray (char[] array, int currentSize, int requiredSize) {
      if (requiredSize < currentSize) {
        return array;
      } else {
        char[] newArray = null;
        int newSize = currentSize * 2;

        if (newSize <= requiredSize)
          newSize = requiredSize + 1;

        newArray = new char [newSize];
        System.Array.Copy (array, 0, newArray, 0, currentSize);
        return newArray;
      }
    }

    //////////////////////////////////////////////////////////////////////
    // Xml query routines.
    //////////////////////////////////////////////////////////////////////

    internal int getParsedEntityType () { return entityType; }

    internal string getStandalone () { return standalone; }

    internal string getXmlVersion () 
    {
      if (xmlVersion == null)
        return "1.0";
      else
        return xmlVersion;
    }

    internal string getEncoding () 
    {
      if (encodingName != null)
        return encodingName;
      
      switch (this.encoding) 
      {
        case ENCODING_UTF_8:
          return "UTF-8";
        case ENCODING_ISO_8859_1:
          return "ISO-8859-1";
        case ENCODING_ASCII:
          return "ASCII";
        case ENCODING_UCS_2_12:
          return "UCS-2-12";
        case ENCODING_UCS_2_21:
          return "UCS-2-21";
        case ENCODING_UCS_4_1234:
          return "UCS-4-1234";
        case ENCODING_UCS_4_4321:
          return "UCS-4-4321";
        case ENCODING_UCS_4_2143:
          return "UCS-4-2143";
        case ENCODING_UCS_4_3412:
          return "UCS-4-3412";
        default:
          return null;
      }
    }

    //
    // Elements
    //

    private int getContentType (object[] element, int defaultType) {
      int retval;

      if (element == null)
        return defaultType;
      retval = ((int) element [0]);
      if (retval == CONTENT_UNDECLARED)
        retval = defaultType;
      return retval;
    }


    /*
     * Look up the content type of an element.
     * @param name The element type name.
     * @return An integer constant representing the content type.
     * @see #CONTENT_UNDECLARED
     * @see #CONTENT_ANY
     * @see #CONTENT_EMPTY
     * @see #CONTENT_MIXED
     * @see #CONTENT_ELEMENTS
     */
    public int getElementContentType (string name) {
      object[] element = (object []) elementInfo[name];
      return getContentType (element, CONTENT_UNDECLARED);
    }


    /*
     * Register an element.
     * Array format:
     *  [0] element type name
     *  [1] content model (mixed, elements only)
     *  [2] attribute hash table
     */
    private void setElement (string name, int contentType, string contentModel, Hashtable attributes) {
      if (skippedPE)
        return;

      object[] element = (object []) elementInfo[name];

      // first <!ELEMENT ...> or <!ATTLIST ...> for this type?
      if (element == null) {
        element = new object [3];
        element [0] = contentType;
        element [1] = contentModel;
        element [2] = attributes;
        elementInfo[name] = element;
        return;
      }

      // <!ELEMENT ...> declaration?
      if (contentType != CONTENT_UNDECLARED) {
        // ... following an associated <!ATTLIST ...>
        if (((int) element [0]) == CONTENT_UNDECLARED) {
          element [0] = contentType;
          element [1] = contentModel;
        } else
          // VC: Unique Element Type Declaration
          handler.verror ("multiple declarations for element type: "
            + name);
      }

        // first <!ATTLIST ...>, before <!ELEMENT ...> ?
      else if (attributes != null)
        element [2] = attributes;
    }


    /*
     * Look up the attribute hash table for an element.
     * The hash table is the second item in the element array.
     */
    private Hashtable getElementAttributes (string name) {
      object[] element = (object[]) elementInfo[name];
      if (element == null)
        return null;
      else
        return (Hashtable) element [2];
    }



    //
    // Attributes
    //

    /*
     * Get the declared attributes for an element type.
     * @param elname The name of the element type.
     * @return An Enumeration of all the attributes declared for
     *  a specific element type.  The results will be valid only
     *  after the DTD (if any) has been parsed.
     * @see #getAttributeType
     * @see #getAttributeEnumeration
     * @see #getAttributeDefaultValueType
     * @see #getAttributeDefaultValue
     * @see #getAttributeExpandedValue
     */
    private ICollection declaredAttributes (object[] element) {
      Hashtable attlist;

      if (element == null)
        return null;
      if ((attlist = (Hashtable) element [2]) == null)
        return null;
      return attlist.Keys;
    }

    /*
     * Get the declared attributes for an element type.
     * @param elname The name of the element type.
     * @return An Enumeration of all the attributes declared for
     *  a specific element type.  The results will be valid only
     *  after the DTD (if any) has been parsed.
     * @see #getAttributeType
     * @see #getAttributeEnumeration
     * @see #getAttributeDefaultValueType
     * @see #getAttributeDefaultValue
     * @see #getAttributeExpandedValue
     */
    public ICollection declaredAttributes (string elname) {
      return declaredAttributes ((object []) elementInfo[elname]);
    }


    /*
     * Retrieve the declared type of an attribute.
     * @param name The name of the associated element.
     * @param aname The name of the attribute.
     * @return An interend string denoting the type, or null
     * indicating an undeclared attribute.
     */
    public string getAttributeType (string name, string aname) {
      object[] attribute = getAttribute (name, aname);
      if (attribute == null) {
        return null;
      } else {
        return (string) attribute [0];
      }
    }


    /*
     * Retrieve the allowed values for an enumerated attribute type.
     * @param name The name of the associated element.
     * @param aname The name of the attribute.
     * @return A string containing the token list.
     */
    public string getAttributeEnumeration (string name, string aname) {
      object[] attribute = getAttribute (name, aname);
      if (attribute == null) {
        return null;
      } else {
        // assert:  attribute [0] is "ENUMERATION" or "NOTATION"
        return (string) attribute [3];
      }
    }


    /*
     * Retrieve the default value of a declared attribute.
     * @param name The name of the associated element.
     * @param aname The name of the attribute.
     * @return The default value, or null if the attribute was
     *  #IMPLIED or simply undeclared and unspecified.
     * @see #getAttributeExpandedValue
     */
    public string getAttributeDefaultValue (string name, string aname) {
      object[] attribute = getAttribute (name, aname);
      if (attribute == null) {
        return null;
      } else {
        return (string) attribute [1];
      }
    }

    /*

// FIXME:  Leaving this in, until W3C finally resolves the confusion
// between parts of the Xml 2nd REC about when entity declararations
// are guaranteed to be known.  Current code matches what section 5.1
// (conformance) describes, but some readings of the self-contradicting
// text in 4.1 (the "Entity Declared" WFC and VC) seem to expect that
// attribute expansion/normalization must be deferred in some cases
// (just TRY to identify them!).

     * Retrieve the expanded value of a declared attribute.
     * <p>General entities (and char refs) will be expanded (once).
     * @param name The name of the associated element.
     * @param aname The name of the attribute.
     * @return The expanded default value, or null if the attribute was
     *  #IMPLIED or simply undeclared
     * @see #getAttributeDefaultValue
    public string getAttributeExpandedValue (string name, string aname)
    throws Exception
    {
  object attribute[] = getAttribute (name, aname);

  if (attribute == null) {
      return null;
  } else if (attribute [4] == null && attribute [1] != null) {
      // we MUST use the same buf for both quotes else the literal
      // can't be properly terminated
      char buf [] = new char [1];
      int flags = LIT_ENTITY_REF | LIT_ATTRIBUTE;
      string type = getAttributeType (name, aname);

      if (type != "CDATA" && type != null)
    flags |= LIT_NORMALIZE;
      buf [0] = '"';
      pushCharArray (null, buf, 0, 1);
      pushstring (null, (string) attribute [1]);
      pushCharArray (null, buf, 0, 1);
      attribute [4] = readLiteral (flags);
  }
  return (string) attribute [4];
    }
     */

    /*
     * Retrieve the default value mode of a declared attribute.
     * @see #ATTRIBUTE_DEFAULT_SPECIFIED
     * @see #ATTRIBUTE_DEFAULT_IMPLIED
     * @see #ATTRIBUTE_DEFAULT_REQUIRED
     * @see #ATTRIBUTE_DEFAULT_FIXED
     */
    public int getAttributeDefaultValueType (string name, string aname) {
      object[] attribute = getAttribute (name, aname);
      if (attribute == null) {
        return ATTRIBUTE_DEFAULT_UNDECLARED;
      } else {
        return ((int) attribute [2]);
      }
    }


    /*
     * Register an attribute declaration for later retrieval.
     * Format:
     * - string type
     * - string default value
     * - int value type
     * - enumeration
     * - processed default value
     */
    private void setAttribute (string elName, string name, string type,
      string enumeration, string value, int valueType) {
      Hashtable attlist;

      if (skippedPE)
        return;

      // Create a new hashtable if necessary.
      attlist = getElementAttributes (elName);
      if (attlist == null)
        attlist = new Hashtable ();

      // ignore multiple attribute declarations!
      if (attlist[name] != null) {
        // warn ...
        return;
      } else {
        object[] attribute = new object [5];
        attribute [0] = type;
        attribute [1] = value;
        attribute [2] = valueType;
        attribute [3] = enumeration;
        attribute [4] = null;
        attlist[name] = attribute;

        // save; but don't overwrite any existing <!ELEMENT ...>
        setElement (elName, CONTENT_UNDECLARED, null, attlist);
      }
    }


    /*
     * Retrieve the array representing an attribute declaration.
     */
    private object[] getAttribute (string elName, string name) {
      Hashtable attlist;

      attlist = getElementAttributes (elName);
      if (attlist == null)
        return null;      
      return (object[]) attlist[name];
    }


    //
    // Entities
    //

    /*
     * Find the type of an entity.
     * @returns An integer constant representing the entity type.
     * @see #ENTITY_UNDECLARED
     * @see #ENTITY_INTERNAL
     * @see #ENTITY_NDATA
     * @see #ENTITY_TEXT
     */
    public int getEntityType (string ename) {
      object[] entity = (object[]) entityInfo[ename];
      if (entity == null) {
        return ENTITY_UNDECLARED;
      } else {
        return ((int) entity [0]);
      }
    }


    /*
     * Return an external entity's identifier array.
     * @param ename The name of the external entity.
     * @return Three element array containing (in order) the entity's
     * public identifier, system identifier, and base URI.  Null if
     *  the entity was not declared as an external entity.
     * @see #getEntityType
     */
    public string [] getEntityIds (string ename) {
      object[] entity = (object[]) entityInfo[ename];
      if (entity == null) {
        return null;
      } else {
        return (string []) entity [1];
      }
    }

    /*
     * Determine if an entity was declared in the external subset or in a parameter entity.
     * @param ename The name of the entity.
     * @return bool True if declared in the external subset or parameter entity.
     * @see #getEntityType
     */
    public bool getEntityDeclaredExternal (string ename) 
    {
      object[] entity = (object[]) entityInfo[ename];
      if (entity == null) 
      {
        return false;
      } 
      else 
      {
        return (bool) entity [2];
      }
    }


    /*
     * Return an internal entity's replacement text.
     * @param ename The name of the internal entity.
     * @return The entity's replacement text, or null if
     *  the entity was not declared as an internal entity.
     * @see #getEntityType
     */
    public string getEntityValue (string ename) 
    {
      object[] entity = (object[]) entityInfo[ename];
      if (entity == null) {
        return null;
      } else {
        return (string) entity [3];
      }
    }


    /*
     * Register an entity declaration for later retrieval.
     */
    private void setInternalEntity (string eName, string val) {
      if (skippedPE)
        return;

      if (entityInfo[eName] == null) {
        object[] entity = new object [5];
        entity [0] = ENTITY_INTERNAL;
        entity [2] = !inInternalSubset;
        entity [3] = val;
        entityInfo[eName] = entity;
      }
      if ("lt" == eName || "gt" == eName || "quot" == eName
        || "apos" == eName || "amp" == eName)
        return;
      handler.getDeclHandler ().InternalEntityDecl (eName, val);
    }


    /*
     * Register an external entity declaration for later retrieval.
     */
    private void setExternalEntity (string eName, int eClass,
      string[] ids, string nName) {
      if (entityInfo[eName] == null) {
        object[] entity = new object [5];
        entity [0] = eClass;
        entity [1] = ids;
        entity [2] = !inInternalSubset;
        // FIXME: shrink!!  [4] irrelevant given [0]
        entity [4] = nName;
        entityInfo[eName] = entity;
      }
    }


    //
    // Notations.
    //

    /*
     * Report a notation declaration, checking for duplicates.
     */
    private void setNotation (string nname, string[] ids) {
      if (skippedPE)
        return;

      handler.notationDecl (nname, ids);
      if (notationInfo[nname] == null)
        notationInfo[nname] = nname;
      else
        // VC: Unique Notation Name
        handler.verror ("Duplicate notation name decl: " + nname);
    }


    //
    // Location.
    //


    /*
     * Return the current line number.
     */
    public long getLineNumber () {
      return line;
    }


    /*
     * Return the current column number.
     */
    public long getColumnNumber () {
      return column;
    }


    //////////////////////////////////////////////////////////////////////
    // High-level I/O.
    //////////////////////////////////////////////////////////////////////


    /*
     * Read a single character from the readBuffer.
     * <p>The readDataChunk () method maintains the buffer.
     * <p>If we hit the end of an entity, try to pop the stack and
     * keep going.
     * <p> (This approach doesn't really enforce Xml's rules about
     * entity boundaries, but this is not currently a validating
     * parser).
     * <p>This routine also attempts to keep track of the current
     * position in external entities, but it's not entirely accurate.
     * @return The next available input character.
     * @see #unread (char)
     * @see #readDataChunk
     * @see #readBuffer
     * @see #line
     * @return The next character from the current input source.
     */
    private char readCh () {
      // As long as there's nothing in the
      // read buffer, try reading more data
      // (for an external entity) or popping
      // the entity stack (for either).
      while (readBufferPos >= readBufferLength) {
        switch (sourceType) {
          case INPUT_READER:
          case INPUT_STREAM:
            readDataChunk ();
            while (readBufferLength < 1) {
              popInput ();
              if (readBufferLength < 1) {
                readDataChunk ();
              }
            }
            break;

          default:

            popInput ();
            break;
        }
      }

      char c = readBuffer [readBufferPos++];

      if (c == '\n') {
        line++;
        column = 0;
      } else {
        if (c == '<') {
          /* the most common return to parseContent () ... NOP */
        } else if ((c < 0x0020 && (c != '\t') && (c != '\r')) || c > 0xFFFD)
          fatalError ("illegal Xml character U+"
            + ((int)c).ToString("X4"));

          // If we're in the DTD and in a context where PEs get expanded,
          // do so ... 1/14/2000 errata identify those contexts.  There
          // are also spots in the internal subset where PE refs are fatal
          // errors, hence yet another flag.
        else if (c == '%') {
          if (expandPE) {
            if (inDeclaration && inInternalSubset)
              fatalError ("PE reference within decl in internal subset.");
            parsePEReference ();
            inPeRefBetweenDecls = !inDeclaration;
            return readCh ();
          }
        }
        column++;
      }

      return c;
    }


    /*
     * Push a single character back onto the current input stream.
     * <p>This method usually pushes the character back onto
     * the readBuffer.
     * <p>I don't think that this would ever be called with 
     * readBufferPos = 0, because the methods always reads a character
     * before unreading it, but just in case, I've added a boundary
     * condition.
     * @param c The character to push back.
     * @see #readCh
     * @see #unread (char[])
     * @see #readBuffer
     */
    private void unread (char c) {
      // Normal condition.
      if (c == '\n') {
        line--;
        column = -1;
      }
      if (readBufferPos > 0) {
        readBuffer [--readBufferPos] = c;
      } else {
        pushstring (null, c.ToString());
      }
    }


    /*
     * Push a char array back onto the current input stream.
     * <p>NOTE: you must <em>never</em> push back characters that you
     * haven't actually read: use pushstring () instead.
     * @see #readCh
     * @see #unread (char)
     * @see #readBuffer
     * @see #pushstring
     */
    private void unread (char[] ch, int length) {
      for (int i = 0; i < length; i++) {
        if (ch [i] == '\n') {
          line--;
          column = -1;
        }
      }
      if (length < readBufferPos) {
        readBufferPos -= length;
      } else {
        pushCharArray (null, ch, 0, length);
      }
    }


    /*
     * Push, or skip, a new external input source.
     * The source will be some kind of parsed entity, such as a PE
     * (including the external DTD subset) or content for the body.
     *
     * @param url The java.net.URL object for the entity.
     * @see SaxDriver#resolveEntity
     * @see #pushstring
     * @see #sourceType
     * @see #pushInput
     * @see #detectEncoding
     * @see #sourceType
     * @see #readBuffer
     */
    private void pushURL (bool  isPE, string  ename, string[]  ids,  // public, system, baseURI
      TextReader  reader, Stream stream, string  urlEncodingName, bool  doResolve) {
      
      bool  ignoreEncoding;
      bool isDocument = false;
      string  systemId;
      InputSource source;

      // Check for entity recursion before  we get any further
      checkEntityRecursion(ename);

      if (!isPE)
        dataBufferFlush ();

      // .NET this is now recreated
      InputSource scratch = new InputSource();
      scratch.PublicId = ids [0];
      //!!SSID -K
      scratch.SystemId = handler.absolutize(handler.SystemId, ids [1], false);

      // See if we should skip or substitute the entity.
      // If we're not skipping, resolving reports startEntity()
      // and updates the (handler's) stack of URIs.
      if (doResolve) {
        // assert (stream == null && reader == null && urlEncodingName == null)
        source = handler.resolveEntity (isPE, ename, scratch, ids [2]);
        if (source == null) {
          handler.warn ("skipping entity: " + ename);
          handler.skippedEntity (ename);
          if (isPE)
            skippedPE = true;
          return;
        }

        // we might be using alternate IDs/encoding
        systemId = source.SystemId;
        if (systemId == null) {
          handler.warn ("missing system ID, using " + ids [1]);
          //!!SSID -K
          systemId = handler.absolutize(handler.SystemId, ids [1], false);
        }
      } else {
        // "[document]", or "[dtd]" via getExternalSubset()
        if (reader != null) 
          scratch = new InputSource<TextReader>(reader);
        else if (stream != null)
          scratch = new InputSource<Stream>(stream);
        else 
          scratch = new InputSource();
        // set the encoding
        scratch. Encoding = urlEncodingName;

        source = scratch;
        //!!SSID
        systemId = handler.absolutize(handler.SystemId, ids [1], false);
        isDocument = ("[document]" == ename);
        handler.startExternalEntity (ename, systemId, isDocument);
      }

      // we may have been given I/O streams directly
      if (source is InputSource<TextReader>) {
        // .NET not applicable if (source.getByteStream () != null)
        //   error ("InputSource has two streams!");
        reader = ((InputSource<TextReader>)source).Source;
      } else if (source is InputSource<Stream>) {
        urlEncodingName = source.Encoding;
        stream = ((InputSource<Stream>)source).Source;
      } else if (systemId == null)
        fatalError ("InputSource has no URI!");
      
      // .NET scratch the scratch after each use
      scratch = null;

      // Push the existing status.
      pushInput (ename);

      // We are no longer in the internal subset if this is a PE
      if (isPE) 
      {
        inInternalSubset = false;
        entityType = ENTITY_TYPE_EXT_PE;
      } 
      else if (!isDocument)
        entityType = ENTITY_TYPE_EXT_GE;
      else
        entityType = ENTITY_TYPE_DOCUMENT;


      // Create a new read buffer.
      // (Note the four-character margin)
      readBuffer = new char [READ_BUFFER_MAX + 4];
      readBufferPos = 0;
      readBufferLength = 0;
      readBufferOverflow = -1;
      inputStream = null;
      line = 1;
      column = 0;
      currentByteCount = 0;

      // If there's an explicit character stream, just
      // ignore encoding declarations.
      if (reader != null) {
        sourceType = INPUT_READER;
        this.reader = reader;
        // FIXME: This may be wrong, the reader may be using a 
        // different encoding paradigm. For now, just grab the label
        urlEncodingName = tryEncodingDecl (true);
        return;
      }
 
      // Else we handle the conversion, and need to ensure
      // it's done right.
      sourceType = INPUT_STREAM;
      if (stream != null) {
        inputStream = stream;
      } else {
        // We have to open our own stream to the URL.
        WebRequest url = WebRequest.Create (systemId);
        externalEntity = url.GetResponse();
        inputStream = externalEntity.GetResponseStream ();
      }

      // If we get to here, there must be
      // an InputStream available.
      // .NET this is now eliminated, may prove to optimize further later
      if (!inputStream.CanSeek) {
        inputStream = new BufferedStream (inputStream, READ_BUFFER_MAX);
      }

      // We need to call start document if we haven't
      if (inputStack.Count == 0) 
        handler.startDocument ();
      else
        hasExtEntity = true;

      // Get any external encoding label.
      if (urlEncodingName == null && externalEntity != null) 
      {
        // External labels can be untrustworthy; filesystems in
        // particular often have the wrong default for content
        // that wasn't locally originated.  Those we autodetect.
        if (!"file".Equals (externalEntity.ResponseUri.Scheme)) {
          int temp;

          // application/xml;charset=something;otherAttr=...
          // ... with many variants on 'something'
          urlEncodingName = externalEntity.ContentType;

          // MHK code (fix for Saxon 5.5.1/007):
          // protect against encoding==null
          if (urlEncodingName == null) {
            temp = -1;
          } else {
            temp = urlEncodingName.IndexOf ("charset");
          }

          // RFC 2376 sez MIME text defaults to ASCII, but since the
          // JDK will create a MIME type out of thin air, we always
          // autodetect when there's no explicit charset attribute.
          if (temp < 0)
            urlEncodingName = null; // autodetect
          else {
            // .NET FIX needed to trim off the mime name
            urlEncodingName = urlEncodingName.Substring(temp);
            // only this one attribute
            if ((temp = urlEncodingName.IndexOf (';')) > 0)
              urlEncodingName = urlEncodingName.Substring (0, temp);

            if ((temp = urlEncodingName.IndexOf ('=', temp + 7)) > 0) {
              urlEncodingName = urlEncodingName.Substring (temp + 1);

              // attributes can have comment fields (RFC 822)
              if ((temp = urlEncodingName.IndexOf ('(')) > 0)
                urlEncodingName = urlEncodingName.Substring (0, temp);
              // ... and values may be quoted
              if ((temp = urlEncodingName.IndexOf ('"')) > 0)
                urlEncodingName = urlEncodingName.Substring (temp + 1,
                  urlEncodingName.IndexOf ('"', temp + 2));
              urlEncodingName.Trim ();
            } else {
              handler.warn ("ignoring illegal MIME attribute: "
                + urlEncodingName);
              urlEncodingName = null;
            }
          }
        }
      }

      // if we got an external encoding label, use it ...
      if (urlEncodingName != null) {
        this.encoding = ENCODING_EXTERNAL;
        setupDecoding (urlEncodingName);
        ignoreEncoding = true;
 
        // ... else autodetect from first bytes.
      } else {
        detectEncoding ();
        ignoreEncoding = false;
      }

      // Read any Xml or text declaration.
      // If we autodetected, it may tell us the "real" encoding.
      try {
        // Capture the encoding
        urlEncodingName = tryEncodingDecl (ignoreEncoding);
      } catch (AElfredUnsupportedEncodingException x) {
        // .NET switched to the new AElfred model
        urlEncodingName = x.getEncoding();

        // if we don't handle the declared encoding,
        // try letting a TextReader do it
        try 
        {
          if (sourceType != INPUT_STREAM)
            throw x;

          //!! Does seeking always work?
          inputStream.Seek (0, SeekOrigin.Begin);
          readBufferPos = 0;
          readBufferLength = 0;
          readBufferOverflow = -1;
          currentByteCount = 0;
          line = 1;
          column = 0;

          System.Text.Encoding enc = System.Text.Encoding.GetEncoding(urlEncodingName);
          switch (this.encoding) 
          {
            case ENCODING_UTF_8:
            case ENCODING_ISO_8859_1:
            case ENCODING_ASCII:
              if (enc.GetByteCount("<") != 1)
                fatalError("invalid encoding switch, document is encoded using a single byte encoding but contains an encoding declaration for a multi-byte encoding");
              break;
            case ENCODING_UCS_2_12:
            case ENCODING_UCS_2_21:
              if (enc.GetByteCount("<") != 2)
                fatalError("invalid encoding switch, document is encoded using a two byte encoding but contains an encoding declaration for an alternate length encoding");
              break;
            case ENCODING_UCS_4_1234:
            case ENCODING_UCS_4_4321:
            case ENCODING_UCS_4_2143:
            case ENCODING_UCS_4_3412:
              if (enc.GetByteCount("<") != 4)
                fatalError("invalid encoding switch, document is encoded using a four byte encoding but contains an encoding declaration for an alternate length encoding");
              break;
            default:
              // External encoding declaration no fatal error
              break;
          }
          
          sourceType = INPUT_READER;
          this.reader = new StreamReader (inputStream, enc);
          inputStream = null;

          // Capture the encoding
          urlEncodingName = tryEncodingDecl (true);

        } 
        catch (ArgumentException) 
        { 
          // This *should* be a NotSupportedException from GetEncoding, however
          // GetEncoding instead returns the amorphous ArgumentException. On
          // Mono it seems that it is a NotSupported exception
          fatalError ("unsupported text encoding",
            urlEncodingName,
            null);
        } 
        catch (NotSupportedException) 
        { 
          // This *should* be a NotSupportedException from GetEncoding, however
          // GetEncoding instead returns the amorphous ArgumentException. On
          // Mono it seems that it is a NotSupported exception
          fatalError ("unsupported text encoding",
            urlEncodingName,
            null);
        } 
      }

      // Store this for later
      encodingName = urlEncodingName;
    }


    /*
     * Check for an encoding declaration.  This is the second part of the
     * Xml encoding autodetection algorithm, relying on detectEncoding to
     * get to the point that this part can read any encoding declaration
     * in the document (using only US-ASCII characters).
     *
     * <p> Because this part starts to fill parser buffers with this data,
     * it's tricky to setup a reader so that Java's built-in decoders can be
     * used for the character encodings that aren't built in to this parser
     * (such as EUC-JP, KOI8-R, Big5, etc).
     *
     * @return any encoding in the declaration, uppercased; or null
     * @see detectEncoding
     */
    private string tryEncodingDecl (bool ignoreEncoding) {
      // Read the Xml/text declaration.
      if (tryRead ("<?xml")) {
        if (tryWhitespace ()) {
          if (inputStack.Count > 0) {
            return parseTextDecl (ignoreEncoding);
          } else {
            return parseXmlDecl (ignoreEncoding);
          }
        } else {
          // <?xml-stylesheet ...?> or similar
          unread ('l');
          unread ('m');
          unread ('x');
          unread ('?');
          unread ('<');
        }
      }
      return null;
    }


    /*
     * Attempt to detect the encoding of an entity.
     * <p>The trick here (as suggested in the Xml standard) is that
     * any entity not in UTF-8, or in UCS-2 with a byte-order mark, 
     * <b>must</b> begin with an Xml declaration or an encoding
     * declaration; we simply have to look for "&lt;?xml" in various
     * encodings.
     * <p>This method has no way to distinguish among 8-bit encodings.
     * Instead, it sets up for UTF-8, then (possibly) revises its assumption
     * later in setupDecoding ().  Any ASCII-derived 8-bit encoding
     * should work, but most will be rejected later by setupDecoding ().
     * @see #tryEncoding (byte[], byte, byte, byte, byte)
     * @see #tryEncoding (byte[], byte, byte)
     * @see #setupDecoding
     */
    private void detectEncoding () {
      // Restart the signature
      signature = new byte [4];

      // Read the first four bytes for
      // autodetection.
      signatureLengthRead = inputStream.Read (signature, 0, 4);

      
      //
      // FIRST:  four byte encodings (who uses these?)
      //
      if (tryEncoding (signature, (byte) 0x00, (byte) 0x00,
        (byte) 0x00, (byte) 0x3c)) {
        // UCS-4 must begin with "<?xml"
        // 0x00 0x00 0x00 0x3c: UCS-4, big-endian (1234)
        // "UTF-32BE"
        encoding = ENCODING_UCS_4_1234;

      } else if (tryEncoding (signature, (byte) 0x3c, (byte) 0x00,
        (byte) 0x00, (byte) 0x00)) {
        // 0x3c 0x00 0x00 0x00: UCS-4, little-endian (4321)
        // "UTF-32LE"
        encoding = ENCODING_UCS_4_4321;

      } else if (tryEncoding (signature, (byte) 0x00, (byte) 0x00,
        (byte) 0x3c, (byte) 0x00)) {
        // 0x00 0x00 0x3c 0x00: UCS-4, unusual (2143)
        encoding = ENCODING_UCS_4_2143;

      } else if (tryEncoding (signature, (byte) 0x00, (byte) 0x3c,
        (byte) 0x00, (byte) 0x00)) {
        // 0x00 0x3c 0x00 0x00: UCS-4, unusual (3421)
        encoding = ENCODING_UCS_4_3412;

        // 00 00 fe ff UCS_4_1234 (with BOM)
        // ff fe 00 00 UCS_4_4321 (with BOM)
      }

        //
        // SECOND:  two byte encodings
        // note ... with 1/14/2000 errata the Xml spec identifies some
        // more "broken UTF-16" autodetection cases, with no Xml decl,
        // which we don't handle here (that's legal too).
        //
      else if (tryEncoding (signature, (byte) 0xfe, (byte) 0xff)) {
        // UCS-2 with a byte-order marker. (UTF-16)
        // 0xfe 0xff: UCS-2, big-endian (12)
        encoding = ENCODING_UCS_2_12;
        signature = new byte[2] {signature[2], signature[3]};
        signatureLengthRead -= 2;

      } else if (tryEncoding (signature, (byte) 0xff, (byte) 0xfe)) {
        // UCS-2 with a byte-order marker. (UTF-16)
        // 0xff 0xfe: UCS-2, little-endian (21)
        encoding = ENCODING_UCS_2_21;
        signature = new byte[2] {signature[2], signature[3]};
        signatureLengthRead -= 2;

      } 
      else if (tryEncoding (signature, (byte) 0x00, (byte) 0x3c,
        (byte) 0x00, (byte) 0x3f)) {
        // UTF-16BE (otherwise, malformed UTF-16)
        // 0x00 0x3c 0x00 0x3f: UCS-2, big-endian, no byte-order mark
        encoding = ENCODING_UCS_2_12;
        fatalError ("no byte-order mark for UCS-2 entity");

      } else if (tryEncoding (signature, (byte) 0x3c, (byte) 0x00,
        (byte) 0x3f, (byte) 0x00)) {
        // UTF-16LE (otherwise, malformed UTF-16)
        // 0x3c 0x00 0x3f 0x00: UCS-2, little-endian, no byte-order mark
        encoding = ENCODING_UCS_2_21;
        fatalError ("no byte-order mark for UCS-2 entity");
      }

        //
        // THIRD:  ASCII-derived encodings, fixed and variable lengths
        //
      else if (tryEncoding (signature, (byte) 0x3c, (byte) 0x3f,
        (byte) 0x78, (byte) 0x6d)) {
        // ASCII derived
        // 0x3c 0x3f 0x78 0x6d: UTF-8 or other 8-bit markup (read ENCODING)
        encoding = ENCODING_UTF_8;
        prefetchASCIIEncodingDecl ();

        
      } else if (signature [0] == (byte) 0xef
        && signature [1] == (byte) 0xbb
        && signature [2] == (byte) 0xbf) {
        // 0xef 0xbb 0xbf: UTF-8 BOM (not part of document text)
        // this un-needed notion slipped into Xml 2nd ed through a
        // "non-normative" erratum; now required by MSFT and UDDI,
        // and E22 made it normative.
        encoding = ENCODING_UTF_8;
        signature = new byte[1] {signature[3]};
        signatureLengthRead -= 3;

      } 
      else 
      {
        // 4c 6f a7 94 ... we don't understand EBCDIC flavors
        // ... but we COULD at least kick in some fixed code page

        // (default) UTF-8 without encoding/Xml declaration
        encoding = ENCODING_UTF_8;
      }
    }


    /*
     * Check for a four-byte signature.
     * <p>Utility routine for detectEncoding ().
     * <p>Always looks for some part of "<?Xml" in a specific encoding.
     * @param sig The first four bytes read.
     * @param b1 The first byte of the signature
     * @param b2 The second byte of the signature
     * @param b3 The third byte of the signature
     * @param b4 The fourth byte of the signature
     * @see #detectEncoding
     */
    private static bool tryEncoding (
      byte[] sig, byte b1, byte b2, byte b3, byte b4) {
      return (sig [0] == b1 && sig [1] == b2
        && sig [2] == b3 && sig [3] == b4);
    }


    /*
     * Check for a two-byte signature.
     * <p>Looks for a UCS-2 byte-order mark.
     * <p>Utility routine for detectEncoding ().
     * @param sig The first four bytes read.
     * @param b1 The first byte of the signature
     * @param b2 The second byte of the signature
     * @see #detectEncoding
     */
    private static bool tryEncoding (byte[] sig, byte b1, byte b2) {
      return ((sig [0] == b1) && (sig [1] == b2));
    }


    /*
     * This method pushes a string back onto input.
     * <p>It is useful either as the expansion of an internal entity, 
     * or for backtracking during the parse.
     * <p>Call pushCharArray () to do the actual work.
     * @param s The string to push back onto input.
     * @see #pushCharArray
     */
    private void pushstring (string ename, string s) {
      char[] ch = s.ToCharArray ();
      pushCharArray (ename, ch, 0, ch.Length);
    }


    /*
     * Push a new internal input source.
     * <p>This method is useful for expanding an internal entity,
     * or for unreading a string of characters.  It creates a new
     * readBuffer containing the characters in the array, instead
     * of characters converted from an input byte stream.
     * @param ch The char array to push.
     * @see #pushstring
     * @see #pushURL
     * @see #readBuffer
     * @see #sourceType
     * @see #pushInput
     */
    private void pushCharArray (string ename, char[] ch, int start, int length) {
      
      // Check for entity recursion before  we get any further
      checkEntityRecursion(ename);
      
      // Push the existing status
      pushInput (ename);
      
      if (ename != null && doReport) {
        dataBufferFlush ();
        handler.startInternalEntity (ename);
      }
      sourceType = INPUT_INTERNAL;
      readBuffer = ch;
      readBufferPos = start;
      readBufferLength = length;
      readBufferOverflow = -1;
    }

    /*
     * Check for a recursive reference to an entity.
     * <p>This method should be called before any call to
     * pushInput</p>
     * @param ename The entity name to check (may be null).
     * @see #pushURL
     * @see #pushCharArray
     * @see #pushInput
     */
    private void checkEntityRecursion (string ename) 
    {
      if (ename != null) 
      {
        // Check to see if we can cross this boundary?
        if (entityBoundary != ENTITY_BOUNDARY_ENABLE) 
          entityBoundaryError ();

        IEnumerator entities = entityStack.GetEnumerator();
        while (entities.MoveNext()) 
        {
          string e = (string) entities.Current;
          if (e != null && e == ename) 
          {
            fatalError ("recursive reference to entity", ename, null);
          }
        }
      }      
    }

    /*
     * Save the current input source onto the stack.
     * <p>This method saves all of the global variables associated with
     * the current input source, so that they can be restored when a new
     * input source has finished.  It also tests for entity recursion.
     * <p>The method saves the following global variables onto a stack
     * using a fixed-length array:
     * <ol>
     * <li>sourceType
     * <li>externalEntity
     * <li>readBuffer
     * <li>readBufferPos
     * <li>readBufferLength
     * <li>line
     * <li>encoding
     * </ol>
     * @param ename The name of the entity (if any) causing the new input.
     * @see #popInput
     * @see #sourceType
     * @see #externalEntity
     * @see #readBuffer
     * @see #readBufferPos
     * @see #readBufferLength
     * @see #line
     * @see #encoding
     */
    private void pushInput (string ename) {
      
      entityStack.Push (ename);

      // Don't bother if there is no current input.
      if (sourceType == INPUT_NONE) {
        return;
      }

      // Set up a snapshot of the current
      // input source.
      object[] input = new object [19];

      input [0] = sourceType;
      input [1] = externalEntity;
      input [2] = readBuffer;
      input [3] = readBufferPos;
      input [4] = readBufferLength;
      input [5] = line;
      input [6] = encoding;
      input [7] = encodingName;
      input [8] = readBufferOverflow;
      input [9] = inputStream;
      input [10] = currentByteCount;
      input [11] = column;
      input [12] = reader;
      input [13] = inInternalSubset;
      input [14] = inDeclaration;
      input [15] = inPeRefBetweenDecls;
      input [16] = entityBalance;
      input [17] = xmlVersion;
      input [18] = entityType;
      
      // Push it onto the stack.
      inputStack.Push (input);

      // Reset the balance for the new entity
      if (ename != null)
        entityBalance = 0;
    }


    /*
     * Restore a previous input source.
     * <p>This method restores all of the global variables associated with
     * the current input source.
     * @exception AElfredEofException
     *    If there are no more entries on the input stack.
     * @see #pushInput
     * @see #sourceType
     * @see #externalEntity
     * @see #readBuffer
     * @see #readBufferPos
     * @see #readBufferLength
     * @see #line
     * @see #encoding
     */
    private void popInput () {
      string ename = (string) entityStack.Pop ();

      // Check to see if we can cross this boundary?
      if (entityStack.Count != 0 && ename != null && entityBoundary != ENTITY_BOUNDARY_ENABLE) 
        entityBoundaryError ();

      if (ename != null && doReport)
        dataBufferFlush ();
      switch (sourceType) 
      {
        case INPUT_STREAM:
          handler.endExternalEntity (ename);
          inputStream.Close ();
          break;
        case INPUT_READER:
          handler.endExternalEntity (ename);
          reader.Close ();
          break;
        case INPUT_INTERNAL:
          if (ename != null && doReport)
            handler.endInternalEntity (ename);
          break;
      }

      // Throw an AElfredEofException if there
      // is nothing else to pop.
      if (inputStack.Count == 0) {
        throw new AElfredEofException ("end of input");
      }

      // Check for entityBalance here (if there have been equal start tags and end tags)
      if (ename != null && entityBalance != 0) 
        fatalError ("end of entity encountered before end tag", "", currentElement);

      object[] input = (object[]) inputStack.Pop ();

      sourceType = ((int) input [0]);
      externalEntity = (WebResponse) input [1];
      readBuffer = (char[]) input [2];
      readBufferPos = ((int) input [3]);
      readBufferLength = ((int) input [4]);
      line = ((long) input [5]);
      encoding = ((int) input [6]);
      encodingName = ((string) input [7]);
      readBufferOverflow = ((int) input [8]);
      inputStream = (Stream) input [9];
      currentByteCount = ((int) input [10]);
      column = ((long) input [11]);
      reader = (TextReader) input [12];
      inInternalSubset = (bool) input [13];
      inDeclaration = (bool) input [14];
      inPeRefBetweenDecls = (bool) input [15];
      entityBalance = (int) input [16];
      xmlVersion = (string) input [17];
    }


    /*
     * Return true if we can read the expected character.
     * <p>Note that the character will be removed from the input stream
     * on success, but will be put back on failure.  Do not attempt to
     * read the character again if the method succeeds.
     * @param delim The character that should appear next.  For a
     *       insensitive match, you must supply this in upper-case.
     * @return true if the character was successfully read, or false if
     *  it was not.
     * @see #tryRead (string)
     */
    private bool tryRead (char delim) {
      char c;

      // Read the character
      c = readCh ();

      // Test for a match, and push the character
      // back if the match fails.
      if (c == delim) {
        return true;
      } else {
        unread (c);
        return false;
      }
    }


    /*
     * Return true if we can read the expected string.
     * <p>This is simply a convenience method.
     * <p>Note that the string will be removed from the input stream
     * on success, but will be put back on failure.  Do not attempt to
     * read the string again if the method succeeds.
     * <p>This method will push back a character rather than an
     * array whenever possible (probably the majority of cases).
     * @param delim The string that should appear next.
     * @return true if the string was successfully read, or false if
     *  it was not.
     * @see #tryRead (char)
     */
    private bool tryRead (string delim) {
      return tryRead (delim.ToCharArray ());
    }

    private bool tryRead (char[] ch) {
      char c;

      // Compare the input, character-
      // by character.

      for (int i = 0; i < ch.Length; i++) {
        c = readCh ();
        if (c != ch [i]) {
          unread (c);
          if (i != 0) {
            unread (ch, i);
          }
          return false;
        }
      }
      return true;
    }



    /*
     * Return true if we can read some whitespace.
     * <p>This is simply a convenience method.
     * <p>This method will push back a character rather than an
     * array whenever possible (probably the majority of cases).
     * @return true if whitespace was found.
     */
    private bool tryWhitespace () {
      char c;
      c = readCh ();
      if (isWhitespace (c)) {
        skipWhitespace ();
        return true;
      } else {
        unread (c);
        return false;
      }
    }


    /*
     * Read all data until we find the specified string.
     * This is useful for scanning CDATA sections and PIs.
     * <p>This is inefficient right now, since it calls tryRead ()
     * for every character.
     * @param delim The string delimiter
     * @see #tryRead (string, bool)
     * @see #readCh
     */
    private void parseUntil (string delim) {
      parseUntil (delim.ToCharArray ());
    }

    private void parseUntil (char[] delim) {
      char c;
      long startLine = line;

      try {
        while (!tryRead (delim)) {
          c = readCh ();
          dataBufferAppend (c);
        }
      } catch (AElfredEofException) {
        fatalError ("end of input while looking for delimiter "
            + "(started on line " + startLine
            + ')', null, new string (delim));
      }
    }


    //////////////////////////////////////////////////////////////////////
    // Low-level I/O.
    //////////////////////////////////////////////////////////////////////


    /*
     * Prefetch US-ASCII Xml/text decl from input stream into read buffer.
     * Doesn't buffer more than absolutely needed, so that when an encoding
     * decl says we need to create an InputStreamReader, we can discard our
     * buffer and reset().  Caller knows the first chars of the decl exist
     * in the input stream.
     */
    private void prefetchASCIIEncodingDecl () {
      int ch;
      readBufferPos = readBufferLength = 0;

      // .NET marking eliminated
      // inputStream.mark (readBuffer.Length);
      
      // Need to add the signature back
      if (signature != null) 
      {
        for (int i = 0; i < signatureLengthRead; i++) 
        {
          readBuffer [readBufferLength++] = (char) signature[i];
        }
        signature = null;
      }
        
      while (true) {
        ch = inputStream.ReadByte ();
        readBuffer [readBufferLength++] = (char) ch;
        switch (ch) {
          case (int) '>':
            return;
          case -1:
            fatalError ("file ends before end of Xml or encoding declaration.",
              null, "?>");
            break;
        }
        if (readBuffer.Length == readBufferLength)
          fatalError ("unfinished Xml or encoding declaration");
      }
    }

    /*
     * Read a chunk of data from an external input source.
     * <p>This is simply a front-end that fills the rawReadBuffer
     * with bytes, then calls the appropriate encoding handler.
     * @see #encoding
     * @see #rawReadBuffer
     * @see #readBuffer
     * @see #filterCR
     * @see #copyUtf8ReadBuffer
     * @see #copyIso8859_1ReadBuffer
     * @see #copyUcs_2ReadBuffer
     * @see #copyUcs_4ReadBuffer
     */
    private void readDataChunk () {
      int count;

      // See if we have any overflow (filterCR sets for CR at end)
      if (readBufferOverflow > -1) {
        readBuffer [0] = (char) readBufferOverflow;
        readBufferOverflow = -1;
        readBufferPos = 1;
        sawCR = true;
      } else {
        readBufferPos = 0;
        sawCR = false;
      }

      // input from a character stream.
      if (sourceType == INPUT_READER) {
        count = reader.Read (readBuffer,
          readBufferPos, READ_BUFFER_MAX - readBufferPos);
        if (count < 0)
          readBufferLength = readBufferPos;
        else
          readBufferLength = readBufferPos + count;
        if (readBufferLength > 0)
          filterCR (count >= 0);
        sawCR = false;
        return;
      }

      // Read as many bytes as possible into the raw buffer.
      if (signature != null) 
      {
        for (int i = 0; i < signatureLengthRead; i++) 
        {
          rawReadBuffer [i] = signature[i];
        }
        signature = null;
        count = signatureLengthRead;
        count += inputStream.Read (rawReadBuffer, count, READ_BUFFER_MAX-count);
      } 
      else 
        count = inputStream.Read (rawReadBuffer, 0, READ_BUFFER_MAX);

      // Dispatch to an encoding-specific reader method to populate
      // the readBuffer.  In most parser speed profiles, these routines
      // show up at the top of the CPU usage chart.
      if (count > 0) {
        switch (encoding) {
            // one byte builtins
          case ENCODING_ASCII:
            copyIso8859_1ReadBuffer (count, (char) 0x0080);
            break;
          case ENCODING_UTF_8:
            copyUtf8ReadBuffer (count);
            break;
          case ENCODING_ISO_8859_1:
            copyIso8859_1ReadBuffer (count, (char) 0);
            break;

            // two byte builtins
          case ENCODING_UCS_2_12:
            copyUcs2ReadBuffer (count, 8, 0);
            break;
          case ENCODING_UCS_2_21:
            copyUcs2ReadBuffer (count, 0, 8);
            break;

            // four byte builtins
          case ENCODING_UCS_4_1234:
            copyUcs4ReadBuffer (count, 24, 16, 8, 0);
            break;
          case ENCODING_UCS_4_4321:
            copyUcs4ReadBuffer (count, 0, 8, 16, 24);
            break;
          case ENCODING_UCS_4_2143:
            copyUcs4ReadBuffer (count, 16, 24, 0, 8);
            break;
          case ENCODING_UCS_4_3412:
            copyUcs4ReadBuffer (count, 8, 0, 24, 16);
            break;
        }
      } else
        readBufferLength = readBufferPos;

      readBufferPos = 0;

      // Filter out all carriage returns if we've seen any
      // (including any saved from a previous read)
      if (sawCR) {
        //.NET changed from count >= 0 to fix James Clark's valid-ext-sa-004
        filterCR (count >= READ_BUFFER_MAX);
        sawCR = false;

        // must actively report EOF, lest some CRs get lost.
        if (readBufferLength == 0 && count >= 0)
          readDataChunk ();
      }

      if (count > 0)
        currentByteCount += count;
    }


    /*
     * Filter carriage returns in the read buffer.
     * CRLF becomes LF; CR becomes LF.
     * @param moreData true iff more data might come from the same source
     * @see #readDataChunk
     * @see #readBuffer
     * @see #readBufferOverflow
     */
    private void filterCR (bool moreData) {
      int i, j;

      readBufferOverflow = -1;

      bool breakLoop = false;
      for (i = j = readBufferPos; j < readBufferLength; i++, j++) {
        switch (readBuffer [j]) {
          case '\r':
            if (j == readBufferLength - 1) {
              if (moreData) {
                readBufferOverflow = '\r';
                readBufferLength--;
              } else  // CR at end of buffer
                readBuffer [i++] = '\n';
              breakLoop = true;
              break;
            } else if (readBuffer [j + 1] == '\n') {
              j++;
            }
            readBuffer [i] = '\n';
            break;

          case '\n':
          default:
            readBuffer [i] = readBuffer [j];
            break;
        }
        if (breakLoop)
          break;
      }
      readBufferLength = i;
    }

    /*
     * Convert a buffer of UTF-8-encoded bytes into UTF-16 characters.
     * <p>When readDataChunk () calls this method, the raw bytes are in 
     * rawReadBuffer, and the final characters will appear in 
     * readBuffer.
     * <p>Note that as of Unicode 3.1, good practice became a requirement,
     * so that each Unicode character has exactly one UTF-8 representation.
     * @param count The number of bytes to convert.
     * @see #readDataChunk
     * @see #rawReadBuffer
     * @see #readBuffer
     * @see #getNextUtf8Byte
     */
    private void copyUtf8ReadBuffer (int count) {
      int i = 0;
      int j = readBufferPos;
      byte b1;
      char c = '\0';

      /*
      // check once, so the runtime won't (if it's smart enough)
      if (count < 0 || count > rawReadBuffer.Length)
          throw new ArrayIndexOutOfBoundsException (Integer.tostring (count));
      */

      while (i < count) {
        b1 = rawReadBuffer [i++];

        // Determine whether we are dealing
        // with a one-, two-, three-, or four-
        // byte sequence.
        if ((b1 & 0x80) == 0x80) { 
          if ((b1 & 0xe0) == 0xc0) {
            // 2-byte sequence: 00000yyyyyxxxxxx = 110yyyyy 10xxxxxx
            c = (char) (((b1 & 0x1f) << 6)
              | getNextUtf8Byte (i++, count));
            if (c < 0x0080)
              encodingError ("Illegal two byte UTF-8 sequence",
                c, 0);
          } else if ((b1 & 0xf0) == 0xe0) {
            // 3-byte sequence:
            // zzzzyyyyyyxxxxxx = 1110zzzz 10yyyyyy 10xxxxxx
            // most CJKV characters
            c = (char) (((b1 & 0x0f) << 12) |
              (getNextUtf8Byte (i++, count) << 6) |
              getNextUtf8Byte (i++, count));
            if (c < 0x0800 || (c >= 0xd800 && c <= 0xdfff))
              encodingError ("Illegal three byte UTF-8 sequence",
                c, 0);
          } else if ((b1 & 0xf8) == 0xf0) {
            // 4-byte sequence: 11101110wwwwzzzzyy + 110111yyyyxxxxxx
            //     = 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx
            // (uuuuu = wwww + 1)
            // "Surrogate Pairs" ... from the "Astral Planes"
            // Unicode 3.1 assigned the first characters there
            int iso646 = b1 & 07;
            iso646 = (iso646 << 6) + getNextUtf8Byte (i++, count);
            iso646 = (iso646 << 6) + getNextUtf8Byte (i++, count);
            iso646 = (iso646 << 6) + getNextUtf8Byte (i++, count);

            if (iso646 <= 0xffff) {
              encodingError ("Illegal four byte UTF-8 sequence",
                iso646, 0);
            } else {
              if (iso646 > 0x0010ffff)
                encodingError (
                  "UTF-8 value out of range for Unicode",
                  iso646, 0);
              iso646 -= 0x010000;
              readBuffer [j++] = (char) (0xd800 | (iso646 >> 10));
              readBuffer [j++] = (char) (0xdc00 | (iso646 & 0x03ff));
              continue;
            }
          } else {
            // The five and six byte encodings aren't supported;
            // they exceed the Unicode (and Xml) range.
            encodingError (
              "unsupported five or six byte UTF-8 sequence",
              0xff & b1, i);
            // NOTREACHED
            c = '\0';
          }
        } else {
          // 1-byte sequence: 000000000xxxxxxx = 0xxxxxxx
          // (US-ASCII character, "common" case, one branch to here)
          c = (char) b1;
        }
        readBuffer [j++] = c;
        if (c == '\r')
          sawCR = true;
      }
      // How many characters have we read?
      readBufferLength = j;
    }


    /*
     * Return the next byte value in a UTF-8 sequence.
     * If it is not possible to get a byte from the current
     * entity, throw an exception.
     * @param pos The current position in the rawReadBuffer.
     * @param count The number of bytes in the rawReadBuffer
     * @return The significant six bits of a non-initial byte in
     *  a UTF-8 sequence.
     * @exception AElfredEofException If the sequence is incomplete.
     */
    private int getNextUtf8Byte (int pos, int count) {
      int val;

      // Take a character from the buffer
      // or from the actual input stream.
      if (pos < count) {
        val = rawReadBuffer [pos];
      } else {
        val = inputStream.ReadByte ();
        if (val == -1) {
          encodingError ("unfinished multi-byte UTF-8 sequence at EOF",
            -1, pos);
        }
      }

      // Check for the correct bits at the start.
      if ((val & 0xc0) != 0x80) {
        encodingError ("bad continuation of multi-byte UTF-8 sequence",
          val, pos + 1);
      }

      // Return the significant bits.
      return (val & 0x3f);
    }


    /*
     * Convert a buffer of US-ASCII or ISO-8859-1-encoded bytes into
     * UTF-16 characters.
     *
     * <p>When readDataChunk () calls this method, the raw bytes are in 
     * rawReadBuffer, and the final characters will appear in 
     * readBuffer.
     *
     * @param count The number of bytes to convert.
     * @param mask For ASCII conversion, 0x7f; else, 0xff.
     * @see #readDataChunk
     * @see #rawReadBuffer
     * @see #readBuffer
     */
    private void copyIso8859_1ReadBuffer (int count, char mask) {
      int i, j;
      for (i = 0, j = readBufferPos; i < count; i++, j++) {
        char c = (char) (rawReadBuffer [i] & 0xff);
        if ((c & mask) != 0)
          // .NET was CharConversionException
          throw new Exception ("non-ASCII character U+" + ((int)c).ToString("X4"));
        readBuffer [j] = c;
        if (c == '\r') {
          sawCR = true;
        }
      }
      readBufferLength = j;
    }


    /*
     * Convert a buffer of UCS-2-encoded bytes into UTF-16 characters
     * (as used in Java string manipulation).
     *
     * <p>When readDataChunk () calls this method, the raw bytes are in 
     * rawReadBuffer, and the final characters will appear in 
     * readBuffer.
     * @param count The number of bytes to convert.
     * @param shift1 The number of bits to shift byte 1.
     * @param shift2 The number of bits to shift byte 2
     * @see #readDataChunk
     * @see #rawReadBuffer
     * @see #readBuffer
     */
    private void copyUcs2ReadBuffer (int count, int shift1, int shift2) {
      int j = readBufferPos;

      if (count > 0 && (count % 2) != 0) {
        encodingError ("odd number of bytes in UCS-2 encoding", -1, count);
      }
      // The loops are faster with less internal branching; hence two
      if (shift1 == 0) { // "UTF-16-LE"
        for (int i = 0; i < count; i += 2) {
          char c = (char) (rawReadBuffer [i + 1] << 8);
          c |= (char)(0xff & rawReadBuffer [i]);
          readBuffer [j++] = c;
          if (c == '\r')
            sawCR = true;
        }
      } else { // "UTF-16-BE"
        for (int i = 0; i < count; i += 2) {
          char c = (char) (rawReadBuffer [i] << 8);
          c |= (char)(0xff & rawReadBuffer [i + 1]);
          readBuffer [j++] = c;
          if (c == '\r')
            sawCR = true;
        }
      }
      readBufferLength = j;
    }


    /*
     * Convert a buffer of UCS-4-encoded bytes into UTF-16 characters.
     *
     * <p>When readDataChunk () calls this method, the raw bytes are in 
     * rawReadBuffer, and the final characters will appear in 
     * readBuffer.
     * <p>Java has Unicode chars, and this routine uses surrogate pairs
     * for ISO-10646 values between 0x00010000 and 0x000fffff.  An
     * exception is thrown if the ISO-10646 character has no Unicode
     * representation.
     *
     * @param count The number of bytes to convert.
     * @param shift1 The number of bits to shift byte 1.
     * @param shift2 The number of bits to shift byte 2
     * @param shift3 The number of bits to shift byte 2
     * @param shift4 The number of bits to shift byte 2
     * @see #readDataChunk
     * @see #rawReadBuffer
     * @see #readBuffer
     */
    private void copyUcs4ReadBuffer (int count, int shift1, int shift2,
      int shift3, int shift4) {
      int j = readBufferPos;

      if (count > 0 && (count % 4) != 0) {
        encodingError (
          "number of bytes in UCS-4 encoding not divisible by 4",
          -1, count);
      }
      for (int i = 0; i < count; i += 4) {
        int value = (((rawReadBuffer [i] & 0xff) << shift1) |
          ((rawReadBuffer [i + 1] & 0xff) << shift2) |
          ((rawReadBuffer [i + 2] & 0xff) << shift3) |
          ((rawReadBuffer [i + 3] & 0xff) << shift4));
        if (value < 0x0000ffff) {
          readBuffer [j++] = (char) value;
          if (value == (int) '\r') {
            sawCR = true;
          }
        } else if (value < 0x0010ffff) {
          value -= 0x010000;
          readBuffer [j++] = (char) (0xd8 | ((value >> 10) & 0x03ff));
          readBuffer [j++] = (char) (0xdc | (value & 0x03ff));
        } else {
          encodingError ("UCS-4 value out of range for Unicode",
            value, i);
        }
      }
      readBufferLength = j;
    }


    /*
     * Report a character encoding error.
     */
    private void encodingError (string message, int evalue, int offset) {
      if (evalue != -1)
        message = message + " (character code: 0x" +
          ((int)evalue).ToString("X4") + ')';
      fatalError (message);
    }


    //////////////////////////////////////////////////////////////////////
    // Local Variables.
    //////////////////////////////////////////////////////////////////////

    /*
     * Re-initialize the variables for each parse.
     */
    private void initializeVariables () {
      // First line
      line = 1;
      column = 0;

      // Set up the buffers for data and names
      dataBufferPos = 0;
      dataBuffer = new char [DATA_BUFFER_INITIAL];
      nameBufferPos = 0;
      nameBuffer = new char [NAME_BUFFER_INITIAL];

      // Set up the DTD hash tables
      elementInfo = new Hashtable ();
      entityInfo = new Hashtable ();
      notationInfo = new Hashtable ();
      skippedPE = false;

      // Set up the variables for the current
      // element context.
      currentElement = null;
      currentElementContent = CONTENT_UNDECLARED;

      // Set up the input variables
      sourceType = INPUT_NONE;
      inputStack = new Stack ();
      entityStack = new Stack ();
      entityType = ENTITY_TYPE_NONE;
      externalEntity = null;
      tagAttributePos = 0;
      tagAttributes = new string [100];
      rawReadBuffer = new byte [READ_BUFFER_MAX];
      readBufferOverflow = -1;

      encoding = 0;
      encodingName = null;

      entityBoundary = ENTITY_BOUNDARY_ENABLE;
      entityBalance = 0;
      xmlVersion = null;

      inLiteral = false;
      expandPE = false;
      inInternalSubset = false;
      inDeclaration = false;
      inPeRefBetweenDecls = false;

      standalone = null;
      docIsStandalone = false;
      hasExtEntity = false;

      doReport = false;
      allowColon = true;

      inCDATA = false;

      symbolTable = new object [SYMBOL_TABLE_LENGTH][];
    }


    //
    // The current Xml handler interface.
    //
    private SaxDriver handler;

    //
    // I/O information.
    //
    private TextReader reader;  // current reader
    private Stream inputStream;   // current input stream
    private long  line;   // current line number
    private long  column;  // current column number
    private int  sourceType;  // type of input source
    private Stack inputStack;  // stack of input soruces
    private WebResponse externalEntity; // current external entity
    private int entityType; // current entity type
    private string encodingName;  // current character encoding name
    private int  encoding;  // current character encoding
    private int  currentByteCount; // bytes read from current source

    //
    // Buffers for decoded but unparsed character input.
    //
    private char[] readBuffer;
    private int  readBufferPos;
    private int  readBufferLength;
    private int  readBufferOverflow;  // overflow from last data chunk.


    //
    // Buffer for undecoded raw byte input.
    //
    private const int READ_BUFFER_MAX = 16384;
    private byte[] rawReadBuffer;


    //
    // Buffer for attribute values, char refs, DTD stuff.
    //
    private static int DATA_BUFFER_INITIAL = 4096;
    private char[] dataBuffer;
    private int  dataBufferPos;

    //
    // Buffer for parsed names.
    //
    private static int NAME_BUFFER_INITIAL = 1024;
    private char[] nameBuffer;
    private int  nameBufferPos;

    //
    // Save any standalone flag
    //
    private string standalone;
    private bool docIsStandalone;
    private bool hasExtEntity;

    //
    // Hashtables for DTD information on elements, entities, and notations.
    // Populated until we start ignoring decls (because of skipping a PE)
    //
    private Hashtable elementInfo;
    private Hashtable entityInfo;
    private Hashtable notationInfo;
    private bool skippedPE;


    //
    // Element type currently in force.
    //
    private string currentElement;
    private int  currentElementContent;

    //
    // Stack of entity names, to detect recursion.
    //
    private Stack entityStack;


    //
    // Entity boundary checking
    //
    private int entityBoundary;
    private int entityBalance;

    //
    // XML Version checking
    private string xmlVersion;

    //
    // PE expansion is enabled in most chunks of the DTD, not all.
    // When it's enabled, literals are treated differently.
    //
    private bool inLiteral;
    private bool expandPE;
    private bool inInternalSubset;
    private bool inDeclaration;
    private bool inPeRefBetweenDecls;

    //
    // can't report entity expansion inside two constructs:
    // - attribute expansions (internal entities only)
    // - markup declarations (parameter entities only)
    //
    private bool doReport;

    //
    // allows control of name character matching for notations entity names and the like
    private bool allowColon;

    //
    // Symbol table, for caching interned names.
    //
    // These show up wherever Xml names or nmtokens are used:  naming elements,
    // attributes, PIs, notations, entities, and enumerated attribute values.
    //
    // NOTE:  This hashtable doesn't grow.  The default size is intended to be
    // rather large for most documents.  Example:  one snapshot of the DocBook
    // Xml 4.1 DTD used only about 350 such names.  As a rule, only pathological
    // documents (ones that don't reuse names) should ever see much collision.
    //
    // Be sure that SYMBOL_TABLE_LENGTH always stays prime, for best hashing.
    // "2039" keeps the hash table size at about two memory pages on typical
    // 32 bit hardware.
    //
    private const int SYMBOL_TABLE_LENGTH = 2039;

    private object[][] symbolTable;

    //
    // Hash table of attributes found in current start tag.
    //
    private string[] tagAttributes;
    private int  tagAttributePos;

    //
    // Utility flag: have we noticed a CR while reading the last
    // data chunk?  If so, we will have to go back and normalise
    // CR or CR/LF line ends.
    //
    private bool sawCR;

    //
    // Utility flag: are we in CDATA?  If so, whitespace isn't ignorable.
    // 
    private bool inCDATA;


  } // End XmlParser


}


/* Look for these comments:
//FIXME = code needs to be modified
// Current bug in detectEncoding requires inputStream.Seek
//.NET
//OPTIMIZE
//ERRORS, can try...catch's be placed around productions that might produce
// generic errors such as premature EOF or end of file encountered... they
// could trap the exception (of a specific type) and re-raise it with a more
// descriptive error message, and required character.... expected:
//!!
//OPTIMIZE is inInternalSubset faster or slower than checking the entityStack.Count == 0,
// In general it is not needed with inMarkupDecl except for ent boundaries?, does it need to 
// be stored in the input stack?
*/


