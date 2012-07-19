/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Kds.Xml.Expat
{
  /**<summary>Defines compile constants for interop with the Expat Dll.</summary>
   * <remarks><para>This interface to the Expat XML parser can only work
   * with a version of Expat that was compiled for UTF-16 output. It is
   * commonly named libexpatw.dll (Windows) or libexpatw.so (Linux).</para>
   * <para>This interface works with the Expat library version 1.95.7.
   * To enable the features of a later version, specific conditional compile
   * symbols must be defined. Currently only the symbols EXPAT_1_95_8_UP and
   * EXPAT_2_0_UP are recognized. EXPAT_1_95_8_UP enables the suspend/resume
   * functionality introduced with Expat 1.95.8. EXPAT_2_0_UP enables
   * large line- and column numbers supported in Expat 2.0.</para></remarks>
   */
  internal class Compile
  {
    private Compile() {}

    /// <summary>Constant for adjusting the alignment of struct and class
    /// members to be compatible with the Expat library. Its value is
    /// determined by which of these conditional compile symbols is defined:
    /// EXPAT_PACK, EXPAT_PACK2, EXPAT_PACK4, EXPAT_PACK8 or EXPAT_PACK16.</summary>
    /// <remarks>The symbols are checked in the order listed above, whichever
    /// is defined first.</remarks>
    public const int PackSize =
    #if EXPAT_PACK
      1;
    #elif EXPAT_PACK2
      2;
    #elif EXPAT_PACK4
      4;
    #elif EXPAT_PACK8
      8;
    #elif EXPAT_PACK16
      16;
    #else
      0;
    #endif

    /// <summary>Constant for adjusting to the calling convention used by
    /// the Expat library. Its value depends on which of these conditional
    /// symbols is defined: EXPAT_STDCALL, EXPAT_CDECL or EXPAT_WINAPI.</summary>
    /// <remarks>The symbols are checked in the order listed above,
    /// whichever is defined first.</remarks>
    public const CallingConvention CallConv =
    #if EXPAT_STDCALL
      CallingConvention.StdCall;
    #elif EXPAT_CDECL
      CallingConvention.Cdecl;
    #elif EXPAT_WINAPI
      CallingConvention.WinApi;
    #else
      CallingConvention.Cdecl;
    #endif
  }

  /**<summary>Represents the parser instance, which is implemented as a
   * struct of type XML_Parser in the Expat library.</summary>
   * <remarks>Only the UserData member is defined to be at a fixed offset
   * and therefore accessible to the calling application.</remarks>
   */
  [StructLayout(LayoutKind.Sequential, Pack=Compile.PackSize)]
  public struct XMLParser {
    public IntPtr UserData;
  }

  /**<summary>Boolean type use in Expat.</summary>
   * <remarks>XMLBool reflects the boolean value semantics of C, that is,
   * anything != XMLBool.FALSE should be considered true, or in other words:
   * do not check for equality with XMLBool.TRUE.</remarks>
   */
  public enum XMLBool: byte {
    FALSE = 0,
    TRUE = 1
  }

  /**<summary>Represents XML_Status enum in Expat library.</summary>
   * <remarks>4 bytes wide for compatibiltiy with C.</remarks>
   */
  public enum XMLStatus: int {
    ERROR = 0,
    OK
  #if EXPAT_1_95_8_UP
    ,
    SUSPENDED
  #endif
  };

  /**<summary>Represents XML_ParamEntityParsing enum in Expat library.</summary> */
  public enum XMLParamEntityParsing: int {
    NEVER = 0,
    UNLESS_STANDALONE,
    ALWAYS
  };

  /**<summary>Represents XML_FeatureEnum enum in Expat library.</summary> */
  public enum XMLFeatureEnum: int {
    END = 0,
    UNICODE,
    UNICODE_WCHAR_T,
    DTD,
    CONTEXT_BYTES,
    MIN_SIZE,
    SIZEOF_XML_CHAR,
    SIZEOF_XML_LCHAR
    /* Additional features must be added to the end of this enum. */
  };

  /**<summary>Represents XML_Feature struct in Expat library.</summary> */
  [StructLayout(LayoutKind.Sequential, Pack=Compile.PackSize)]
  public struct XMLFeature {
    public XMLFeatureEnum Feature;
    public unsafe char* Name;
    public int Value;
  };

  /**<summary>Represents XML_Expat_Version struct in Expat library.</summary> */
  [StructLayout(LayoutKind.Sequential, Pack=Compile.PackSize)]
  public struct XMLExpatVer {
    public int Major;
    public int Minor;
    public int Micro;
  };

#if EXPAT_1_95_8_UP
  /**<summary>Represents XML_Parsing enum in Expat library.</summary> */
  public enum XMLParsing: int {
    INITIALIZED,
    PARSING,
    FINISHED,
    SUSPENDED
  };

  /**<summary>Represents XML_ParsingStatus struct in Expat library.</summary> */
  [StructLayout(LayoutKind.Sequential, Pack=Compile.PackSize)]
  public struct XMLParsingStatus {
    public XMLParsing Parsing;
    public XMLBool FinalBuffer;
  }
#endif

  /**<summary>Represents XML_Error enum in Expat library.</summary>
   * <remarks>Must be in sync with <see cref="XMLErrorSet"/>.</remarks>
   */
  public enum XMLError: int {
    NONE,
    NO_MEMORY,
    SYNTAX,
    NO_ELEMENTS,
    INVALID_TOKEN,
    UNCLOSED_TOKEN,
    PARTIAL_CHAR,
    TAG_MISMATCH,
    DUPLICATE_ATTRIBUTE,
    JUNK_AFTER_DOC_ELEMENT,
    PARAM_ENTITY_REF,
    UNDEFINED_ENTITY,
    RECURSIVE_ENTITY_REF,
    ASYNC_ENTITY,
    BAD_CHAR_REF,
    BINARY_ENTITY_REF,
    ATTRIBUTE_EXTERNAL_ENTITY_REF,
    MISPLACED_XML_PI,
    UNKNOWN_ENCODING,
    INCORRECT_ENCODING,
    UNCLOSED_CDATA_SECTION,
    EXTERNAL_ENTITY_HANDLING,
    NOT_STANDALONE,
    UNEXPECTED_STATE,
    ENTITY_DECLARED_IN_PE,
    FEATURE_REQUIRES_XML_DTD,
    CANT_CHANGE_FEATURE_ONCE_PARSING,
    UNBOUND_PREFIX
  #if EXPAT_1_95_8_UP
    ,
    UNDECLARING_PREFIX,
    INCOMPLETE_PE,
    XML_DECL,
    TEXT_DECL,
    PUBLICID,
    SUSPENDED,
    NOT_SUSPENDED,
    ABORTED,
    FINISHED,
    SUSPEND_PE,
    RESERVED_PREFIX_XML,
    RESERVED_PREFIX_XMLNS,
    RESERVED_NAMESPACE_URI
  #endif
  };

  /**<summary>Bit set representation of <see cref="XMLError"/>.</summary>
   * <remarks>Useful when testing for groups of errors. Must be kept
   * in sync with <see cref="XMLError"/>.</remarks>
   */
  [Flags]
  public enum XMLErrorSet: long {
    // "set of XMLError" - *must* be in sync with the XMLError enumeration
    NONE = (long)1 << XMLError.NONE,
    NO_MEMORY = (long)1 << XMLError.NO_MEMORY,
    SYNTAX = (long)1 << XMLError.SYNTAX,
    NO_ELEMENTS = (long)1 << XMLError.NO_ELEMENTS,
    INVALID_TOKEN = (long)1 << XMLError.INVALID_TOKEN,
    UNCLOSED_TOKEN = (long)1 << XMLError.UNCLOSED_TOKEN,
    PARTIAL_CHAR = (long)1 << XMLError.PARTIAL_CHAR,
    TAG_MISMATCH = (long)1 << XMLError.TAG_MISMATCH,
    DUPLICATE_ATTRIBUTE = (long)1 << XMLError.DUPLICATE_ATTRIBUTE,
    JUNK_AFTER_DOC_ELEMENT = (long)1 << XMLError.JUNK_AFTER_DOC_ELEMENT,
    PARAM_ENTITY_REF = (long)1 << XMLError.PARAM_ENTITY_REF,
    UNDEFINED_ENTITY = (long)1 << XMLError.UNDEFINED_ENTITY,
    RECURSIVE_ENTITY_REF = (long)1 << XMLError.RECURSIVE_ENTITY_REF,
    ASYNC_ENTITY = (long)1 << XMLError.ASYNC_ENTITY,
    BAD_CHAR_REF = (long)1 << XMLError.BAD_CHAR_REF,
    BINARY_ENTITY_REF = (long)1 << XMLError.BINARY_ENTITY_REF,
    ATTRIBUTE_EXTERNAL_ENTITY_REF = (long)1 << XMLError.ATTRIBUTE_EXTERNAL_ENTITY_REF,
    MISPLACED_XML_PI = (long)1 << XMLError.MISPLACED_XML_PI,
    UNKNOWN_ENCODING = (long)1 << XMLError.UNKNOWN_ENCODING,
    INCORRECT_ENCODING = (long)1 << XMLError.INCORRECT_ENCODING,
    UNCLOSED_CDATA_SECTION = (long)1 << XMLError.UNCLOSED_CDATA_SECTION,
    EXTERNAL_ENTITY_HANDLING = (long)1 << XMLError.EXTERNAL_ENTITY_HANDLING,
    NOT_STANDALONE = (long)1 << XMLError.NOT_STANDALONE,
    UNEXPECTED_STATE = (long)1 << XMLError.UNEXPECTED_STATE,
    ENTITY_DECLARED_IN_PE = (long)1 << XMLError.ENTITY_DECLARED_IN_PE,
    FEATURE_REQUIRES_XML_DTD = (long)1 << XMLError.FEATURE_REQUIRES_XML_DTD,
    CANT_CHANGE_FEATURE_ONCE_PARSING = (long)1 << XMLError.CANT_CHANGE_FEATURE_ONCE_PARSING,
    UNBOUND_PREFIX = (long)1 << XMLError.UNBOUND_PREFIX,
  #if EXPAT_1_95_8_UP
    UNDECLARING_PREFIX = (long)1 << XMLError.UNDECLARING_PREFIX,
    INCOMPLETE_PE = (long)1 << XMLError.INCOMPLETE_PE,
    XML_DECL = (long)1 << XMLError.XML_DECL,
    TEXT_DECL = (long)1 << XMLError.TEXT_DECL,
    PUBLICID = (long)1 << XMLError.PUBLICID,
    SUSPENDED = (long)1 << XMLError.SUSPENDED,
    NOT_SUSPENDED = (long)1 << XMLError.NOT_SUSPENDED,
    ABORTED = (long)1 << XMLError.ABORTED,
    FINISHED = (long)1 << XMLError.FINISHED,
    SUSPEND_PE = (long)1 << XMLError.SUSPEND_PE,
    RESERVED_PREFIX_XML = (long)1 << XMLError.RESERVED_PREFIX_XML,
    RESERVED_PREFIX_XMLNS = (long)1 << XMLError.RESERVED_PREFIX_XMLNS,
    RESERVED_NAMESPACE_URI = (long)1 << XMLError.RESERVED_NAMESPACE_URI,
  #endif
    // end of "set of XMLError" flags
    OPERATIONAL = FEATURE_REQUIRES_XML_DTD | CANT_CHANGE_FEATURE_ONCE_PARSING
  #if EXPAT_1_95_8_UP
                  | SUSPENDED | NOT_SUSPENDED | ABORTED | FINISHED
  #endif
  }

  /**<summary>Represents XML_Content_Type enum in Expat library.</summary> */
  public enum XMLContentType: int {
    ILLEGAL,  // dummy, to make XML_CTYPE_EMPTY = 1
    EMPTY,
    ANY,
    MIXED,
    NAME,
    CHOICE,
    SEQ
  };

  /**<summary>Represents XML_Content_Quant enum in Expat library.</summary> */
  public enum XMLContentQuant: int {
    NONE,
    OPT,
    REP,
    PLUS
  };

  /**<summary>Represents XML_Content struct in Expat library.</summary> */
  [StructLayout(LayoutKind.Sequential, Pack=Compile.PackSize)]
  public struct XMLContent {
    public XMLContentType Type;
    public XMLContentQuant Quant;
    public unsafe char* Name;
    public uint NumChildren;
    public unsafe XMLContent* Children;
  }

  /**<summary>Represents XML_ElementDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLElementDeclHandler(IntPtr userData,
                        char* name,
                        XMLContent* model);

  /**<summary>Represents XML_AttlistDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLAttlistDeclHandler(IntPtr userData,
                        char* elName,
                        char* attName,
                        char* attType,
                        char* dflt,
                        int isRequired);

  /**<summary>Represents XML_XmlDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLXmlDeclHandler(IntPtr userData,
                    char* version,
                    char* encoding,
                    int standalone);

  /**<summary>Allocate memory call-back. Represents function pointer type of
   * XML_Memory_Handling_Suite.malloc_fcn member in Expat library.</summary>
   * <remarks>Delegate instance must not be garbage collected while in use.</remarks>
   */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void* MallocFcn(int size);

  /**<summary>Re-allocate memory call-back. Represents function pointer type
   * of XML_Memory_Handling_Suite.realloc_fcn member in Expat library.</summary>
   * <remarks>Delegate instance must not be garbage collected while in use.</remarks>
   */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void* ReallocFcn(void* ptr, int size);

  /**<summary>Free memory call-back. Represents function pointer type of
   * XML_Memory_Handling_Suite.free_fcn member in Expat library.</summary>
   * <remarks>Delegate instance must not be garbage collected while in use.</remarks>
   */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void FreeFcn(void* ptr);

  /**<summary>Represents the XML_Memory_Handling_Suite struct, which is used
   * to pass memory handling function pointers to the Expat library.</summary>
   */
  [StructLayout(LayoutKind.Sequential, Pack=Compile.PackSize)]
  public class XMLMemoryHandlingSuite {
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public MallocFcn Malloc;
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public ReallocFcn Realloc;
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public FreeFcn Free;
  }

  /**<summary>Represents XML_StartElementHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLStartElementHandler(IntPtr userData,
                         char* name,
                         char** atts);

  /**<summary>Represents XML_EndElementHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLEndElementHandler(IntPtr userData,
                       char* name);

  /**<summary>Represents XML_CharacterDataHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLCharacterDataHandler(IntPtr userData,
                          char* s,
                          int len);

  /**<summary>Represents XML_ProcessingInstructionHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLProcessingInstructionHandler(IntPtr userData,
                                  char* target,
                                  char* data);

  /**<summary>Represents XML_CommentHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLCommentHandler(IntPtr userData,
                    char* data);

  /**<summary>Represents XML_StartCdataSectionHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLStartCdataSectionHandler(IntPtr userData);

  /**<summary>Represents XML_EndCdataSectionHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLEndCdataSectionHandler(IntPtr userData);

  /**<summary>Represents XML_DefaultHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLDefaultHandler(IntPtr userData,
                    char* s,
                    int len);

  /**<summary>Represents XML_StartDoctypeDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLStartDoctypeDeclHandler(IntPtr userData,
                             char* doctypeName,
                             char* systemId,
                             char* publicId,
                             int hasInternalSubset);

  /**<summary>Represents XML_EndDoctypeDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLEndDoctypeDeclHandler(IntPtr userData);

  /**<summary>Represents XML_EntityDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLEntityDeclHandler(IntPtr userData,
                       char* entityName,
                       int isParameterEntity,
                       char* value,
                       int valueLen,
                       char* baseUri,
                       char* systemId,
                       char* publicId,
                       char* notationName);

  /**<summary>Represents XML_UnparsedEntityDeclHandler call-back type in Expat library.</summary> 
   * <remarks>Obsolete - superceded by XML_EntityDeclHandler.</remarks>
   */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLUnparsedEntityDeclHandler(IntPtr userData,
                               char* entityName,
                               char* baseUri,
                               char* systemId,
                               char* publicId,
                               char* notationName);

  /**<summary>Represents XML_NotationDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLNotationDeclHandler(IntPtr userData,
                         char* notationName,
                         char* baseUri,
                         char* systemId,
                         char* publicId);

  /**<summary>Represents XML_StartNamespaceDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLStartNamespaceDeclHandler(IntPtr userData,
                               char* prefix,
                               char* uri);

  /**<summary>Represents XML_EndNamespaceDeclHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLEndNamespaceDeclHandler(IntPtr userData,
                             char* prefix);

  /**<summary>Represents XML_NotStandaloneHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate int
  XMLNotStandaloneHandler(IntPtr userData);

  /**<summary>Represents XML_ExternalEntityRefHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate int
  XMLExternalEntityRefHandler(XMLParser* parser,
                              char* context,
                              char* baseUri,
                              char* systemId,
                              char* publicId);

  /**<summary>Represents XML_SkippedEntityHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLSkippedEntityHandler(IntPtr userData,
                          char* entityName,
                          int isParameterEntity);

  /**<summary>Represents call-back type of XML_Encoding.convert member in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate int
  XMLEncConvert(IntPtr data,
                byte* s);

  /**<summary>Represents call-back type of XML_Encoding.release member in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate void
  XMLEncRelease(IntPtr data);


  /**<summary>Represents XML_Encoding struct in Expat library.</summary>
   * <remarks>Needs to be filled in by managed application only when
   * it wants to process documents in non-standard encodings.</remarks>
   */
  [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
  public unsafe struct XMLEncoding
  {
    fixed int Map[256];
    public IntPtr Data;
    // must be based on a delegate of type XMLEncConvert; use Marshal.DelegateToFuncionPointer()
    public IntPtr Convert;
    // must be based on a delegate of type XMLEncRelease; use Marshal.DelegateToFuncionPointer()
    public IntPtr Release;
  };

  /**<summary>Represents XML_UnknownEncodingHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate int
  XMLUnknownEncodingHandler(IntPtr encodingHandlerData,
                            char* name,
                            ref XMLEncoding info);

#if false // Did not choose this option for unknown encoding call-back
  /**<summary>Represents XML_Encoding struct in Expat library.</summary>
   * <remarks>Needs to be filled in by managed application only when
   * it wants to process documents in non-standard encodings.</remarks>
   */
  [StructLayout(LayoutKind.Sequential, Pack=Compile.PackSize)]
  public class XMLEncoding {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst=256)]
    public int[] Map;
    public IntPtr Data;
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public XMLEncConvert Convert;
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public XMLEncRelease Release;
  };

  /**<summary>Represents XML_UnknownEncodingHandler call-back type in Expat library.</summary> */
  [UnmanagedFunctionPointer(Compile.CallConv)]
  public unsafe delegate int
  XMLUnknownEncodingHandler(IntPtr encodingHandlerData,
                            char* name,
                            [Out, MarshalAs(UnmanagedType.LPStruct)] XMLEncoding info);
#endif


  /**<summary>This class wraps the the Expat library for interop purposes.</summary>
   * <remarks><list type="bullet">
   * <item>The name of the Expat library file is assumed to be libexpatw.dll.</item>
   * <item>Each exported Dll function corresponds to a public method.</item>
   * <item>The calling convention for interacting with the Dll can be controlled
   * at <see cref="Compile">compile time</see> by defining compile symbols.</item>
   * </list></remarks>
   */
  [SuppressUnmanagedCodeSecurity]
  public unsafe class LibExpat {

    public const string expatLib = "libexpatw.dll";

    public static bool ErrorInSet(XMLError error, XMLErrorSet errorSet)
    {
      XMLErrorSet errorFlag = (XMLErrorSet)((long)1 << (int)error);
      return (errorFlag & errorSet) != 0;
    }

    [DllImport(expatLib,
               EntryPoint="XML_SetElementDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetElementDeclHandler(XMLParser* parser,
                             XMLElementDeclHandler eldecl);

    [DllImport(expatLib,
               EntryPoint="XML_SetAttlistDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetAttlistDeclHandler(XMLParser* parser,
                             XMLAttlistDeclHandler attDecl);

    [DllImport(expatLib,
               EntryPoint="XML_SetXmlDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetXmlDeclHandler(XMLParser* parser,
                         XMLXmlDeclHandler xmlDecl);

    [DllImport(expatLib,
               EntryPoint="XML_ParserCreate",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLParser*
    XMLParserCreate(string encoding);

    [DllImport(expatLib,
               EntryPoint="XML_ParserCreateNS",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLParser*
    XMLParserCreateNS(string encoding,
                      char namespaceSeparator);

    /* make sure memsuite is not garbage collected */
    [DllImport(expatLib,
               EntryPoint="XML_ParserCreate_MM",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLParser*
    XMLParserCreateMM(string encoding,
                      [In] XMLMemoryHandlingSuite memsuite,
                      string namespaceSeparator);

    [DllImport(expatLib,
               EntryPoint="XML_ParserReset",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLBool
    XMLParserReset(XMLParser* parser,
                   string encoding);

    [DllImport(expatLib,
               EntryPoint="XML_SetEntityDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetEntityDeclHandler(XMLParser* parser,
                            XMLEntityDeclHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetElementHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetElementHandler(XMLParser* parser,
                         XMLStartElementHandler start,
                         XMLEndElementHandler end);

    [DllImport(expatLib,
               EntryPoint="XML_SetStartElementHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetStartElementHandler(XMLParser* parser,
                              XMLStartElementHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetEndElementHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetEndElementHandler(XMLParser* parser,
                            XMLEndElementHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetCharacterDataHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetCharacterDataHandler(XMLParser* parser,
                               XMLCharacterDataHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetProcessingInstructionHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetProcessingInstructionHandler(XMLParser* parser,
                                       XMLProcessingInstructionHandler handler);
    [DllImport(expatLib,
               EntryPoint="XML_SetCommentHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetCommentHandler(XMLParser* parser,
                         XMLCommentHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetCdataSectionHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetCdataSectionHandler(XMLParser* parser,
                              XMLStartCdataSectionHandler start,
                              XMLEndCdataSectionHandler end);

    [DllImport(expatLib,
               EntryPoint="XML_SetStartCdataSectionHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetStartCdataSectionHandler(XMLParser* parser,
                                   XMLStartCdataSectionHandler start);

    [DllImport(expatLib,
               EntryPoint="XML_SetEndCdataSectionHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetEndCdataSectionHandler(XMLParser* parser,
                                 XMLEndCdataSectionHandler end);

    [DllImport(expatLib,
               EntryPoint="XML_SetDefaultHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetDefaultHandler(XMLParser* parser,
                         XMLDefaultHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetDefaultHandlerExpand",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetDefaultHandlerExpand(XMLParser* parser,
                               XMLDefaultHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetDoctypeDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetDoctypeDeclHandler(XMLParser* parser,
                             XMLStartDoctypeDeclHandler start,
                             XMLEndDoctypeDeclHandler end);

    [DllImport(expatLib,
               EntryPoint="XML_SetStartDoctypeDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetStartDoctypeDeclHandler(XMLParser* parser,
                                  XMLStartDoctypeDeclHandler start);

    [DllImport(expatLib,
               EntryPoint="XML_SetEndDoctypeDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetEndDoctypeDeclHandler(XMLParser* parser,
                                XMLEndDoctypeDeclHandler end);

    /* obsolete */
    [DllImport(expatLib,
               EntryPoint="XML_SetUnparsedEntityDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetUnparsedEntityDeclHandler(XMLParser* parser,
                                    XMLUnparsedEntityDeclHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetNotationDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetNotationDeclHandler(XMLParser* parser,
                              XMLNotationDeclHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetNamespaceDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetNamespaceDeclHandler(XMLParser* parser,
                               XMLStartNamespaceDeclHandler start,
                               XMLEndNamespaceDeclHandler end);

    [DllImport(expatLib,
               EntryPoint="XML_SetStartNamespaceDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetStartNamespaceDeclHandler(XMLParser* parser,
                                    XMLStartNamespaceDeclHandler start);

    [DllImport(expatLib,
               EntryPoint="XML_SetEndNamespaceDeclHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetEndNamespaceDeclHandler(XMLParser* parser,
                                  XMLEndNamespaceDeclHandler end);

    [DllImport(expatLib,
               EntryPoint="XML_SetNotStandaloneHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetNotStandaloneHandler(XMLParser* parser,
                               XMLNotStandaloneHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetExternalEntityRefHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetExternalEntityRefHandler(XMLParser* parser,
                                   XMLExternalEntityRefHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetExternalEntityRefHandlerArg",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetExternalEntityRefHandlerArg(XMLParser* parser,
                                      void* arg);

    [DllImport(expatLib,
               EntryPoint="XML_SetSkippedEntityHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetSkippedEntityHandler(XMLParser* parser,
                               XMLSkippedEntityHandler handler);

    [DllImport(expatLib,
               EntryPoint="XML_SetUnknownEncodingHandler",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetUnknownEncodingHandler(XMLParser* parser,
                                 XMLUnknownEncodingHandler handler,
                                 IntPtr encodingHandlerData);

    [DllImport(expatLib,
               EntryPoint="XML_DefaultCurrent",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLDefaultCurrent(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_SetReturnNSTriplet",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetReturnNSTriplet(XMLParser* parser,
                          int do_nst);

    [DllImport(expatLib,
               EntryPoint="XML_SetUserData",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLSetUserData(XMLParser* parser,
                   IntPtr userData);

    public static IntPtr
    XMLGetUserData(XMLParser* parser) {
      return (*parser).UserData;
    }

    [DllImport(expatLib,
               EntryPoint="XML_SetEncoding",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLStatus
    XMLSetEncoding(XMLParser* parser,
                   string encoding);

    [DllImport(expatLib,
               EntryPoint="XML_UseParserAsHandlerArg",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLUseParserAsHandlerArg(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_UseForeignDTD",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLError
    XMLUseForeignDTD(XMLParser* parser,
                     XMLBool useDTD);


    [DllImport(expatLib,
               EntryPoint="XML_SetBase",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLStatus
    XMLSetBase(XMLParser* parser,
               string baseUri);

    [DllImport(expatLib,
               EntryPoint="XML_GetBase",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern IntPtr
    _XMLGetBase(XMLParser* parser);

    public static string XMLGetBase(XMLParser* parser)
    {
      IntPtr basePtr = _XMLGetBase(parser);
      return Marshal.PtrToStringUni(basePtr);
    }

    [DllImport(expatLib,
               EntryPoint="XML_GetSpecifiedAttributeCount",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern int
    XMLGetSpecifiedAttributeCount(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_GetIdAttributeIndex",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern int
    XMLGetIdAttributeIndex(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_Parse",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLStatus
    XMLParse(XMLParser* parser,
             byte* s,
             int len,
             int isFinal);

    [DllImport(expatLib,
               EntryPoint="XML_Parse",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLStatus
    XMLParse(XMLParser* parser,
             [In, MarshalAs(UnmanagedType.LPArray)]
             byte[] s,
             int len,
             int isFinal);

    [DllImport(expatLib,
               EntryPoint="XML_GetBuffer",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern byte*
    XMLGetBuffer(XMLParser* parser,
                 int len);

/*  this will not work, as the returned array is copied
    [DllImport(expatLib,port(expatLib,
               EntryPoint="XML_GetBuffer",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    [return: MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)]
    public static extern byte[]
    XMLGetBufferArray(XMLParser* parser,
                      int len);
*/

    [DllImport(expatLib,
               EntryPoint="XML_ParseBuffer",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLStatus
    XMLParseBuffer(XMLParser* parser,
                   int len,
                   int isFinal);

    [DllImport(expatLib,
               EntryPoint="XML_StopParser",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLStatus
    XMLStopParser(XMLParser* parser,
                  XMLBool resumable);

    [DllImport(expatLib,
               EntryPoint="XML_ResumeParser",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLStatus
    XMLResumeParser(XMLParser* parser);


#if EXPAT_1_95_8_UP
    [DllImport(expatLib,
               EntryPoint="XML_GetParsingStatus",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLGetParsingStatus(XMLParser* parser, out XMLParsingStatus status);
#endif

    [DllImport(expatLib,
               EntryPoint="XML_ExternalEntityParserCreate",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLParser*
    XMLExternalEntityParserCreate(XMLParser* parser,
                                  char* context,
                                  string encoding);

    [DllImport(expatLib,
               EntryPoint="XML_SetParamEntityParsing",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern int
    XMLSetParamEntityParsing(XMLParser* parser,
                             XMLParamEntityParsing parsing);

    [DllImport(expatLib,
               EntryPoint="XML_GetErrorCode",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLError
    XMLGetErrorCode(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_GetCurrentLineNumber",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
#if EXPAT_2_0_UP
    public static extern ulong
#else
    public static extern uint
#endif
    XMLGetCurrentLineNumber(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_GetCurrentColumnNumber",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
#if EXPAT_2_0_UP
    public static extern ulong
#else
    public static extern uint
#endif
    XMLGetCurrentColumnNumber(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_GetCurrentByteIndex",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
#if EXPAT_2_0_UP
    public static extern long
#else
    public static extern int
#endif
    XMLGetCurrentByteIndex(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_GetCurrentByteCount",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern int
    XMLGetCurrentByteCount(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_GetInputContext",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern byte*
    XMLGetInputContext(XMLParser* parser,
                       ref int offset,
                       ref int size);

    [DllImport(expatLib,
               EntryPoint="XML_FreeContentModel",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLFreeContentModel(XMLParser* parser,
                        XMLContent* model);

    [DllImport(expatLib,
               EntryPoint="XML_MemMalloc",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void*
    XMLMemMalloc(XMLParser* parser,
                 uint size);  //kw size_t in C is platform dependent

    [DllImport(expatLib,
               EntryPoint="XML_MemRealloc",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void*
    XMLMemRealloc(XMLParser* parser,
                  void* ptr,
                  uint size);  // size_t in C is platform dependent

    [DllImport(expatLib,
               EntryPoint="XML_MemFree",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLMemFree(XMLParser* parser,
               void* ptr);

    [DllImport(expatLib,
               EntryPoint="XML_ParserFree",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern void
    XMLParserFree(XMLParser* parser);

    [DllImport(expatLib,
               EntryPoint="XML_ErrorString",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern IntPtr  //kw or char* and table lookup?
    _XMLErrorString(XMLError code);

    public static string XMLErrorString(XMLError code)
    {
      IntPtr errPtr = _XMLErrorString(code);
      return Marshal.PtrToStringUni(errPtr);
    }

    [DllImport(expatLib,
               EntryPoint="XML_ExpatVersion",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern IntPtr
    _XMLExpatVersion();

    public static string XMLExpatVersion()
    {
      IntPtr verPtr = _XMLExpatVersion();
      return Marshal.PtrToStringUni(verPtr);
    }

    [DllImport(expatLib,
               EntryPoint="XML_ExpatVersionInfo",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLExpatVer
    XMLExpatVersionInfo();

    [DllImport(expatLib,
               EntryPoint="XML_GetFeatureList",
               CharSet=CharSet.Unicode,
               CallingConvention=Compile.CallConv)]
    public static extern XMLFeature*
    XMLGetFeatureList();
  }
}
