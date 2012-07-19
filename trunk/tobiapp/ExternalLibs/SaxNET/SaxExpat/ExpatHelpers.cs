/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using Kds.Text;

namespace Kds.Xml.Expat
{
  /**<summary>Interop helper class for reading pinned data from TextReader.
   * To be used with <see cref="ReadBuffer">ReadBuffer</see> delegate definition.</summary>
   */
  public class TextBufferReader
  {
    private TextReader reader;
    private char[] charBuffer;

    public TextBufferReader(TextReader reader, int charBufferSize)
    {
      if (reader == null)
        throw new ArgumentNullException("reader");
      this.reader = reader;
      this.charBuffer = new char[charBufferSize];
    }

    public TextBufferReader(TextReader reader): this(reader, 0) { }

    public TextReader Reader
    {
      get { return reader; }
    }

    /// <summary><see cref="ReadBuffer"/> implementation for TextReader.</summary>
    public int Read(int count, out GCHandle bufferHandle)
    {
      if (count % 2 != 0)  //kw error message - resource string
        throw new ArgumentException("Cannot read odd number of bytes from TextReader", "count");
      if (count == 0) {
        bufferHandle = ExpatUtils.EmptyHandle;
        return 0;
      }
      int charCount = count >> 1;
      if (charBuffer.Length < charCount)
        Array.Resize<char>(ref charBuffer, charCount);
      int result = reader.Read(charBuffer, 0, charCount) << 1;
      bufferHandle = GCHandle.Alloc(charBuffer, GCHandleType.Pinned);
      return result;
    }
  }

  /**<summary>Interop helper class for reading pinned data from Stream.
   * To be used with <see cref="ReadBuffer"/> delegate type.</summary>
   */
  public class StreamBufferReader
  {
    private Stream stream;
    private byte[] byteBuffer;

    public StreamBufferReader(Stream stream, int bufferSize)
    {
      if (stream == null)
        throw new ArgumentNullException("stream");
      this.stream = stream;
      byteBuffer = new byte[bufferSize];
    }

    public StreamBufferReader(Stream stream): this(stream, 0) { }

    public Stream Stream
    {
      get { return stream; }
    }

    /// <summary><see cref="ReadBuffer"/> implementation for Stream.</summary>
    public int Read(int count, out GCHandle bufferHandle)
    {
      if (count == 0) {
        bufferHandle = ExpatUtils.EmptyHandle;
        return 0;
      }
      if (byteBuffer.Length < count)
        Array.Resize<byte>(ref byteBuffer, count);
      int result = stream.Read(byteBuffer, 0, count);
      bufferHandle = GCHandle.Alloc(byteBuffer, GCHandleType.Pinned);
      return result;
    }
  }

  /**<summary>Interop helper class for reading pinned data from string.
   * To be used with <see cref="ReadBuffer"/> delegate type.</summary>
   */
  public class StringBufferReader
  {
    private string str;
    private int pos = 0;

    public StringBufferReader(string str)
    {
      if (str == null)
        throw new ArgumentNullException("str");
      this.str = str;
    }

    public string String
    {
      get { return str; }
    }

    /// <summary>Position of first unread character.</summary>
    public int Pos
    {
      get { return pos; }
    }

    /// <summary><see cref="ReadBuffer"/> implementation for string.</summary>
    public int Read(int count, out GCHandle bufferHandle)
    {
      int result = str.Length - pos;
      if (result > count)
        result = count;
      if (result == 0)
        bufferHandle = ExpatUtils.EmptyHandle;
      else {
        string bufStr;
        if (pos > 0)
          bufStr = String.Substring(pos, result);
        else
          bufStr = str;
        bufferHandle = GCHandle.Alloc(bufStr, GCHandleType.Pinned);
        pos += result;
      }
      return result;
    }
  }

  /**<summary>Provides helper routines for converting between managed and
   * unmanaged data structures and adapting the Expat API for use with standard
   * .NET framework classes. Also defines some Expat related constants.</summary>
   */
  public static class ExpatUtils
  {
    private const char NullChar = (char)0;
    private const char Colon = ':';
    private const char Equal = '=';

    public const char NSSep = '\x1F';      // ASCII US (unit separator)
    public const char ContextSep = '\x0C'; // ASCII DC2 (device control 2)
    public const int DefaultReadBufSize = 16384;

    // EmptyHandle = (GCHandle)IntPtr.Zero; gives a "Handle not initialized" error
    public static readonly GCHandle EmptyHandle = GCHandle.Alloc(null);

    /* Prerequisiste: name != null - for all SaxParseNameXXX routines
     * Note: expect namespace triplet (see Expat docs) in name argument
     *   only in case of prefix encountered
     */

    /// <summary>Parses Expat name into local name, namespace URI and qualified name.</summary>
    /// <remarks>If there is no namespace then the <c>uri</c> parameter
    /// will be set to the empty string.</remarks>
    /// <param name="name">Element or attribute name as returned by Expat. Must not be <c>null</c>.</param>
    /// <param name="strTable">Output strings are "interned" in this string table.</param>
    /// <param name="uri">Namespace URI of name. Never <c>null</c>.</param>
    /// <param name="locName">Local part of name. Never <c>null</c>.</param>
    /// <param name="qName">Qualified (prefixed) name. Never <c>null</c>.</param>
    public static unsafe void
    ParseNameSax(char* name,
                 StringTable strTable,
                 out string uri,
                 out string locName,
                 out string qName)
    {
      char* tmpStr = name;
      int tmpLen;

      // look for end of Uri: first NS separator
      while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
      tmpLen = unchecked((int)(tmpStr - name));

      if (*tmpStr == NullChar) {  // not a qualified name
        uri = String.Empty;
        locName = strTable.Intern(name, tmpLen);
        qName = locName;
      }
      else {             // a namespace separator was found
        uri = strTable.Intern(name, tmpLen);
        tmpStr++;

        name = tmpStr;  // save start of local name
        // look for end of local name: second NS separator could be found
        while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
        tmpLen = unchecked((int)(tmpStr - name));
        locName = strTable.Intern(name, tmpLen);

        if (*tmpStr == NullChar) {  // no prefix
          qName = locName;
        }
        else {            // we have a prefix
          tmpStr++;
          char* prefix = tmpStr;  // save start of prefix
          while (*tmpStr != NullChar) tmpStr++;
          // use saved length of local name to calculate length of QName
          tmpLen = unchecked((int)(tmpStr - prefix) + tmpLen + 1);
          char* qNameBuf = stackalloc char[tmpLen];
          tmpStr = qNameBuf;
          // first, copy prefix to QName buffer
          while (*prefix != NullChar) *tmpStr++ = *prefix++;
          // then, copy colon
          *tmpStr++ = Colon;
          // finally, copy local name - name variable still poinst to it
          while (*name != NSSep) *tmpStr++ = *name++;
          qName = strTable.Intern(qNameBuf, tmpLen);
        }
      }
    }

    /// <summary>Parses Expat name into local name, namespace URI and prefix.</summary>
    /// <remarks>If there is no namespace then the <c>uri</c> and <c>prefix</c>
    /// parameters will be set to the empty string. For the default namespace,
    /// the <c>prefix</c> parameter will be returned as the empty string.</remarks>
    /// <param name="name">Element or attribute name as returned by Expat. Must not be <c>null</c>.</param>
    /// <param name="strTable">Output strings are "interned" in this string table.</param>
    /// <param name="uri">Namespace URI of name. Never <c>null</c>.</param>
    /// <param name="locName">Local part of name. Never <c>null</c>.</param>
    /// <param name="prefix">Namespace prefix of name. Never <c>null</c>.</param>
    public static unsafe void
    ParseName(char* name,
              StringTable strTable,
              out string uri,
              out string locName,
              out string prefix)
    {
      char* tmpStr = name;
      int tmpLen;

      // look for end of Uri: first NS separator
      while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
      tmpLen = unchecked((int)(tmpStr - name));

      if (*tmpStr == NullChar) {  // not a qualified name
        uri = String.Empty;
        locName = strTable.Intern(name, tmpLen);
        prefix = String.Empty;
      }
      else {             // a namespace separator was found
        uri = strTable.Intern(name, tmpLen);
        tmpStr++;

        name = tmpStr;  // save start of local name
        // look for end of local name: second NS separator could be found
        while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
        tmpLen = unchecked((int)(tmpStr - name));
        locName = strTable.Intern(name, tmpLen);

        if (*tmpStr == NullChar) {  // no prefix
          prefix = String.Empty;
        }
        else {            // we have a prefix
          tmpStr++;
          prefix = strTable.Intern(tmpStr);
        }
      }
    }

    /// <summary>Parses Expat name into local name and namespace URI.</summary>
    /// <remarks>If there is no namespace then the <c>uri</c> parameter
    /// will be set to the empty string.</remarks>
    /// <param name="name">Element or attribute name as returned by Expat. Must not be <c>null</c>.</param>
    /// <param name="strTable">Output strings are "interned" in this string table.</param>
    /// <param name="uri">Namespace URI of name. Never <c>null</c>.</param>
    /// <param name="locName">Local part of name. Never <c>null</c>.</param>
    public static unsafe void
    ParseNameUriLocal(char* name,
                      StringTable strTable,
                      out string uri,
                      out string locName)
    {
      char* tmpStr = name;
      int tmpLen;

      // look for end of Uri: first NS separator
      while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
      tmpLen = unchecked((int)(tmpStr - name));

      if (*tmpStr == NullChar) {  // no Uri found
        uri = String.Empty;
        locName = strTable.Intern(name, tmpLen);
      }
      else {             // a namespace separator was found
        uri = strTable.Intern(name, tmpLen);
        tmpStr++;
        name = tmpStr;  // save start of local name
        // look for end of local name: second NS separator could be found
        while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
        tmpLen = unchecked((int)(tmpStr - name));
        locName = strTable.Intern(name, tmpLen);
      }
    }

    /// <summary>Parses Expat name into namespace URI.</summary>
    /// <remarks>If there is no namespace then the return value
    /// is an the empty string.</remarks>
    /// <param name="name">Element or attribute name as returned by Expat. Must not be <c>null</c>.</param>
    /// <param name="strTable">Output strings are "interned" in this string table.</param>
    /// <returns>Namespace URI of name. Never <c>null</c>.</returns>
    public static unsafe string
      ParseNameUri(char* name, StringTable strTable)
    {
      string uri;
      char* tmpStr = name;
      // look for end of Uri: first NS separator
      while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
      if (*tmpStr == NullChar)  // no Uri found
        uri = String.Empty;
      else               // a namespace separator was found
        uri = strTable.Intern(name, unchecked((int)(tmpStr - name)));
      return uri;
    }

    /// <summary>Parses Expat name into local name.</summary>
    /// <param name="name">Element or attribute name as returned by Expat. Must not be <c>null</c>.</param>
    /// <param name="strTable">Output strings are "interned" in this string table.</param>
    /// <returns>Local part of name. Never <c>null</c>.</returns>
    public static unsafe string
      ParseNameLocal(char* name, StringTable strTable)
    {
      string locName;
      char* tmpStr = name;
      // look for end of Uri: first NS separator
      while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
      if (*tmpStr != NullChar) {  // a namespace separator was found
        tmpStr++;
        name = tmpStr;  // save start of local name
        // look for end of local name: second NS separator could be found
        while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
      }
      locName = strTable.Intern(name, unchecked((int)(tmpStr - name)));
      return locName;
    }

    /// <summary>Parses Expat name into qualified name.</summary>
    /// <param name="name">Element or attribute name as returned by Expat. Must not be <c>null</c>.</param>
    /// <param name="strTable">Output strings are "interned" in this string table.</param>
    /// <returns>Qualified (prefixed) name. Never <c>null</c>.</returns>
    public static unsafe string
      ParseNameQual(char* name, StringTable strTable)
    {
      string qName;
      char* tmpStr = name;
      int tmpLen;

      // look for end of Uri: first NS separator
      while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;

      if (*tmpStr == NullChar) {  // not a qualified name, QName = local name
        tmpLen = unchecked((int)(tmpStr - name));
        qName = strTable.Intern(name, tmpLen);
      }
      else {             // a namespace separator was found
        tmpStr++;
        name = tmpStr;  // save start of local name
        // look for end of local name: second NS separator could be found
        while (*tmpStr != NSSep && *tmpStr != NullChar) tmpStr++;
        tmpLen = unchecked((int)(tmpStr - name));  // length of local name

        if (*tmpStr == NullChar) {  // no prefix, QName = local name
          qName = strTable.Intern(name, tmpLen);
        }
        else {               // we have a prefix
          tmpStr++;
          char* prefix = tmpStr;  // save start of prefix
          while (*tmpStr != NullChar) tmpStr++;
          // use saved length of local name to calculate length of QName
          tmpLen = unchecked((int)(tmpStr - prefix) + tmpLen + 1);
          char* qNameBuf = stackalloc char[tmpLen];
          tmpStr = qNameBuf;
          // first, copy prefix to QName buffer
          while (*prefix != NullChar) *tmpStr++ = *prefix++;
          // then, copy colon
          *tmpStr++ = Colon;
          // finally, copy local name - name variable still poinst to it
          while (*name != NSSep) *tmpStr++ = *name++;
          qName = strTable.Intern(qNameBuf, tmpLen);
        }
      }
      return qName;
    }

    /// <summary>Parses XML data provided by a <see cref="ReadBuffer"/> delegate.</summary>
    /// <param name="parser">Expat parser instance.</param>
    /// <param name="read">Source of input data.</param>
    /// <param name="bufferSize">Buffer size to use for reading.</param>
    /// <param name="error">Error returned by parser.</param>
    /// <returns>Completion status of parsing (finished, failed, suspended or aborted).</returns>
    public static unsafe ParseStatus
      Parse(XMLParser* parser, ReadBuffer read, int bufferSize, out XMLError error)
    {
      Debug.Assert(read != null && bufferSize > 0);
      bool isFinal = false;
      GCHandle bufferHandle;

      while (!isFinal) {
        XMLStatus status;
        int count = read(bufferSize, out bufferHandle);
        try {
          isFinal = count == 0;
          IntPtr bufPtr = bufferHandle.AddrOfPinnedObject();
          status = LibExpat.XMLParse(parser, (byte*)bufPtr, count, isFinal ? 1 : 0);
        }
        finally {
          if (!isFinal)  // if count == 0 then GCHandle was not pinned
            bufferHandle.Free();
        }

        switch (status) {
          case XMLStatus.OK:
            continue;
          case XMLStatus.ERROR:
            error = LibExpat.XMLGetErrorCode(parser);
#if EXPAT_1_95_8_UP
            if (error == XMLError.ABORTED)
              return ParseStatus.Aborted;
#endif
            return ParseStatus.FatalError;
#if EXPAT_1_95_8_UP
          case XMLStatus.SUSPENDED:
            error = XMLError.NONE;
            return ParseStatus.Suspended;
#endif
        }
      }
      error = XMLError.NONE;
      return ParseStatus.Finished;
    }

    /// <summary>Parses XML data provided by a <see cref="ReadBuffer"/> delegate.</summary>
    /// <param name="parser">Expat parser instance.</param>
    /// <param name="read">Source of input data.</param>
    /// <param name="error">Error returned by parser.</param>
    /// <returns>Completion status of parsing (finished, failed, suspended or aborted).</returns>
    public static unsafe ParseStatus
      Parse(XMLParser* parser, ReadBuffer read, out XMLError error)
    {
      return Parse(parser, read, DefaultReadBufSize, out error);
    }

#if EXPAT_1_95_8_UP
    /// <summary>Resumes parsing the current buffer when parsing was suspended.</summary>
    /// <param name="parser">Expat parser instance.</param>
    /// <param name="parseStatus">Completion status of parsing.
    /// Meaningful only when <c>true</c> is returned.</param>
    /// <param name="error">Error status of parsing.
    /// Meaningful only when <c>true</c> is returned.</param>
    /// <returns><c>false</c> when another buffer needs to be started for parsing,
    /// <c>true</c> otherwise. That is, when <c>true</c>, either the last buffer
    /// was finished successfully, a fatal error was encountered, parsing was aborted,
    /// or parsing was suspended again.</returns>
    public static unsafe bool
      ResumeParsingBuffer(XMLParser* parser, ref ParseStatus parseStatus, out XMLError error)
    {
      bool result = true;
      XMLStatus status = LibExpat.XMLResumeParser(parser);
      switch (status) {
        case XMLStatus.OK:
          error = XMLError.NONE;
          XMLParsingStatus pStatus;
          LibExpat.XMLGetParsingStatus(parser, out pStatus);
          if (pStatus.FinalBuffer == XMLBool.FALSE)
            result = false;
          else
            parseStatus = ParseStatus.Finished;
          break;
        case XMLStatus.ERROR:
          error = LibExpat.XMLGetErrorCode(parser);
          if (error == XMLError.ABORTED)
            parseStatus = ParseStatus.Aborted;
          else
            parseStatus = ParseStatus.FatalError;
          break;
        case XMLStatus.SUSPENDED:
          error = XMLError.NONE;
          parseStatus = ParseStatus.Suspended;
          break;
        default:
          error = XMLError.NONE;
          break;
      }
      return result;
    }

    /// <overloads>
    /// <summary>Resumes parsing when parsing was previously suspended.</summary>
    /// </overloads>
    /// <param name="parser">Expat parser instance.</param>
    /// <param name="read">Source of input data.</param>
    /// <param name="bufferSize">Buffer size to use for reading.</param>
    /// <param name="error">Error returned by parser.</param>
    /// <returns>Completion status of parsing (finished, failed, suspended or aborted).</returns>
    public static unsafe ParseStatus
      ResumeParsing(XMLParser* parser, ReadBuffer read, int bufferSize, out XMLError error)
    {
      ParseStatus parseStatus = ParseStatus.Finished;
      if (!ResumeParsingBuffer(parser, ref parseStatus, out error))
        parseStatus = Parse(parser, read, bufferSize, out error);
      return parseStatus;
    }

    /// <param name="parser">Expat parser instance.</param>
    /// <param name="read">Source of input data.</param>
    /// <param name="error">Error returned by parser.</param>
    /// <returns>Completion status of parsing (finished, failed, suspended or aborted).</returns>
    public static unsafe ParseStatus
      ResumeParsing(XMLParser* parser, ReadBuffer read, out XMLError error)
    {
      ParseStatus parseStatus = ParseStatus.Finished;
      if (!ResumeParsingBuffer(parser, ref parseStatus, out error))
        parseStatus = Parse(parser, read, out error);
      return parseStatus;
    }
#endif

    /// <summary>Default implementation for resolving an URI to a Stream.</summary>
    /// <param name="baseUri">Base URI to use if uriStr is a relative URI.</param>
    /// <param name="uriStr"> URI to be resolved.</param>
    /// <param name="timeout">Maximum time after which to return (with an error)</param>
    /// <param name="ignoreTimeoutError">Pass <c>true</c> if a <see cref="NotSupportedException"/>
    /// should be ignored when setting the value of <see cref="WebRequest.Timeout"/>.</param>
    /// <returns>Stream for URI.</returns>
    public static Stream UriToStream(Uri baseUri, string uriStr, int timeout, bool ignoreTimeoutError)
    {
      //kw what about detecting an encoding?
      Uri uri = null;
      Stream result = null;
      try {
        if (baseUri == null)
          uri = new Uri(uriStr);
        else
          uri = new Uri(baseUri, uriStr);
      }
      catch (UriFormatException) {
        // not an URI, we assume it is a file name
        result = new FileStream(uriStr, FileMode.Open, FileAccess.Read);
      }
      if (result == null) {
        WebRequest request = WebRequest.Create(uri);
        try {
          request.Timeout = timeout;
        }
        catch (NotSupportedException) {
          if (!ignoreTimeoutError)
            throw;
        }
        WebResponse response = request.GetResponse();
        result = response.GetResponseStream();
      }
      return result;
    }

    /// <summary>Extracts entity name from Expat context string.</summary>
    /// <param name="context">Expat specific structure. See expat.h for documentation.
    /// Must be != <c>null</c> and null-terminated, which is not checked.</param>
    /// <param name="strTable">String table for interning the entity name.</param>
    /// <returns>The extracted entity name, or <c>null</c> if the context string
    /// did not contain a name.</returns>
    public static unsafe string GetEntityName(char* context, StringTable strTable)
    {
      for (; ; ) {
        bool isName = true;
        char* name = context;
        char ctx = *context;
        while (ctx != ExpatUtils.NullChar && ctx != ExpatUtils.ContextSep) {
          // if we have an "=", we don't have a name
          if (ctx == ExpatUtils.Equal) {
            // skip to end of token
            do {
              ctx = *(++context);
            } while (ctx != ExpatUtils.NullChar && ctx != ExpatUtils.ContextSep);
            isName = false;
            break;
          }
          ctx = *(++context);
        }
        // at this point, *context == Constants.NullChar or ExpatUtils.ContextSep
        if (isName) {
          string result = strTable.Intern(name, unchecked((int)(context - name)));
          return result;
        }
        else if (ctx == ExpatUtils.NullChar)
          break;
        else
          context++;
      }
      return null;
    }
  }

  /**<summary>Resolves external identifier to a stream.</summary>
   * <param name="baseUri">Base URI for system identifer. Can be <c>null</c>.</param>
   * <param name="systemId">System identifier. Must not be <c>null</c>.</param>
   * <param name="publicId">Public identifier. Can be <c>null</c>.</param>
   * <param name="encoding">Encoding of stream data. Can be <c>null</c>.</param>
   * <returns>Stream for external id, <c>null</c> if it could not be resolved.</returns>
   */
  public delegate Stream ResolveExternalId(Uri baseUri, string systemId, string publicId, ref string encoding);

  /**<summary>Provides stream for external DTD subset when requested.</summary>
   * <param name="encoding">Encoding of stream data. Can be <c>null</c>.</param>
   * <returns>Stream for external DTD subset. <c>null</c> if none available.</returns>
   */
  public delegate Stream ResolveForeignDtd(ref string encoding);

  /**<summary>Subclass of <see cref="ExpatParser&lt;X, E, U>">ExpatParser</see> that
   * provides entity resolver events together with a built-in default resolver.</summary>
   *<remarks>To be used with <see cref="StdEntityParseContext&lt;E, X, U>"/>.</remarks>
   */
  public class StdExpatParser<X, E, U>: ExpatParser<X, E, U>
    where X: StdExpatParser<X, E, U>
    where E: StdEntityParseContext<E, X, U>, new()
    where U: class
  {
    private int uriTimeout = 6000;
    private bool ignoreTimeoutError = true;

    internal unsafe Stream Resolve(char* baseUri, char* systemId, char* publicId, out string encoding)
    {
      const char NullChar = (char)0;
      Stream stream = null;
      string sysIdStr = null;
      encoding = null;

      // normalize empty string
      if (systemId != null && *systemId != NullChar)
        sysIdStr = new string(systemId);
      // do we have a foreign DTD?
      if (sysIdStr == null) {
        if (OnResolveForeignDtd != null)
          stream = OnResolveForeignDtd(ref encoding);
      }
      else {
        string baseUriStr = null;
        if (baseUri != null && *baseUri != NullChar)
          baseUriStr = new string(baseUri);
        Uri bsUri = null;
        if (baseUriStr != null)
          bsUri = new Uri(baseUriStr);
        if (OnResolveExternalId != null) {
          string pubIdStr = null;
          // normalize empty string
          if (publicId != null && *publicId != NullChar)
            pubIdStr = new string(publicId);
          stream = OnResolveExternalId(bsUri, sysIdStr, pubIdStr, ref encoding);
        }
        else
          stream = ExpatUtils.UriToStream(bsUri, sysIdStr, UriTimeout, IgnoreTimeoutError);
      }
      return stream;
    }

    public StdExpatParser(string encoding, bool namespaces, U userData):
      base(encoding, namespaces, userData) { }

    /// <summary>Called when an external identifier needs to be resolved to a stream.</summary>
    public event ResolveExternalId OnResolveExternalId;

    /// <summary>Called when an external DTD subset could be provided by the application.</summary>
    public event ResolveForeignDtd OnResolveForeignDtd;

    /// <summary>Timeout for <see cref="WebRequest.GetResponse"/>.</summary>
    public int UriTimeout
    {
      get { return uriTimeout; }
      set { uriTimeout = value; }
    }

    /// <summary>Indicates if a <see cref="NotSupportedException"/> should be ignored
    /// when setting the value of <see cref="WebRequest.Timeout"/>.</summary>
    public bool IgnoreTimeoutError
    {
      get { return ignoreTimeoutError; }
      set { ignoreTimeoutError = value; }
    }
  }

  /// <summary>Subclass of <see cref="EntityParseContext&lt;E, X, U>">EntityParseContext</see>
  /// that cooperates with <see cref="StdExpatParser&lt;X, E, U>"/> to resolve an external
  /// entity identifier, or an external subset request, to a <see cref="Stream"/>.</summary>
  /// <remarks><see cref="Start"/>, <see cref="Cleanup"/> and <see cref="Reset"/> have been
  /// overridden accordingly.</remarks>
  public class StdEntityParseContext<E, X, U>: EntityParseContext<E, X, U>
    where E: StdEntityParseContext<E, X, U>, new()
    where X: StdExpatParser<X, E, U>
    where U: class
  {
    private Stream stream = null;

    public StdEntityParseContext() { }

    /// <summary>Called when external entity reference or a request for an external DTD
    /// subset is encountered. This implementation supports resolving the external
    /// identifier or DTD subset request to a <see cref="Stream"/>.
    /// See <see cref="EntityParseContext&lt;E, X, U>.Start"/>.</summary>
    /// <param name="context">Expat specific parsing context structure.</param>
    /// <param name="baseUri">Base URI. Can be <c>null</c>.</param>
    /// <param name="systemId">System identifier for entity. <c>null</c> for foreign DTD.</param>
    /// <param name="publicId">Public identifier for entity.</param>
    /// <param name="read"><see cref="StreamBufferReader"/> for the entity's data.</param>
    /// <param name="encoding">Encoding of the entity. Always <c>null</c> in this implementation.</param>
    /// <returns><c>false</c>, if the entity should be skipped, <c>true</c> otherwise.</returns>
    protected override unsafe bool Start(
      char* context, 
      char* baseUri, 
      char* systemId, 
      char* publicId, 
      out ReadBuffer read, 
      out string encoding)
    {
      // Debug.Assert(Parent != null && systemId != null);  // external subset only for root entity
      stream = parser.Resolve(baseUri, systemId, publicId, out encoding);
      bool result = stream != null;
      if (result)
        read = new StreamBufferReader(stream).Read;
      else
        read = null;
      return result;
    }

    /// <summary>Cleans up unmanaged resources.</summary>
    /// <remarks>Called at end of child entity, after
    /// <see cref="EntityParseContext&lt;E, X, U>.Finish"/> and before
    /// <see cref="Reset"/>. Gets called for root entity only when the
    /// <see cref="StdExpatParser&lt;X, E, U>">StdExpatParser</see> instance
    /// that owns it gets disposed.</remarks>
    protected internal override void Cleanup()
    {
      base.Cleanup();
      if (stream != null)
        // the root entity uses a ReadBuffer, without creating or being passed a stream
        stream.Close();
    }

    /// <summary>Resets state of instance to be used again.</summary>
    /// <remarks>Called at end of parsing entity, after
    /// <see cref="EntityParseContext&lt;E, X, U>.Finish"/> and <see cref="Cleanup"/>
    /// are called. Also called for root entity, unlike
    /// <see cref="EntityParseContext&lt;E, X, U>.Finish"/> and <see cref="Cleanup"/>.</remarks>
    protected internal override void Reset()
    {
      base.Reset();
      // don't clear Stream for root entity, it might get re-used
      if (Parent != null)
        stream = null;
    }
  }

  /**<summary>Convenience declaration, for use with <see cref="StdExpatParser"/>.</summary> */
  public class StdEntityParseContext: StdEntityParseContext<StdEntityParseContext, StdExpatParser, object>
  {
    // for use with StdExpatParser
    public StdEntityParseContext() { }
  }

  /**<summary>Convenience declaration, so that a basic 
   * <see cref="ExpatParser&lt;X, E, U>">ExpatParser</see> can be used without having to declare
   * a subclass of <see cref="EntityParseContext&lt;E, X, U>">EntityParseContext</see>.</summary>
   */
  public class StdExpatParser: StdExpatParser<StdExpatParser, StdEntityParseContext, object>
  {
    public StdExpatParser(string encoding, bool namespaces, object userData):
      base(encoding, namespaces, userData) { }
  }

}