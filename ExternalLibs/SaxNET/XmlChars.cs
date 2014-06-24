// NO WARRANTY!  This code is in the Public Domain.
// Written by Karl Waclawek (karl@waclawek.net).

using System;

namespace Org.System.Xml
{
  /// <summary>Useful for character and name checking routines.</summary>
  public enum CharType: byte 
  {
    NONXML, MALFORM, LT, AMP, RSQB, LEAD2, LEAD3,
    LEAD4, TRAIL, CR, LF, GT, QUOT, APOS,
    EQUALS, QUEST, EXCL, SOL, SEMI, NUM, LSQB,
    S, NMSTRT, COLON, HEX, DIGIT, NAME, MINUS,
    /// <summary>Known not to be a name or name start character.</summary>
    OTHER,
    /// <summary>Might be a name or name start character.</summary>
    NONASCII,
    PERCNT, LPAR, RPAR, AST, PLUS, COMMA, VERBAR
  };

  /// <summary>For convenient access to the high and low byte of a character.</summary>
  public struct CharStruct
  {
    public const ushort HiMask = 0xFF00;
    public const ushort LoMask = 0x00FF;

    public readonly byte Lo;
    public readonly byte Hi;

    public CharStruct(char ch) 
    {
      unchecked {
        Hi = (byte)((ushort)(ch & HiMask) >> 8);
        Lo = (byte)(ch & LoMask);
      }
    }
  }

  /// <summary>Provides XML character and name checking.</summary>summary>
  /// <remarks> The byte and bitmap tables were translated from James Clark's 
  /// <see href="http://www.libexpat.org">Expat parser</see>.</remarks>
  public static class XmlChars
  {
    /// <summary>Splits <see href="http://w3.org/TR/REC-xml-names/#ns-qualnames">
    /// namespace qualified name</see> into namespace prefix and local part.</summary>
    /// <remarks>To check if <c>qName</c> is well-formed, call <see cref="IsNcName(string)"/>
    /// for both, the <c>prefix</c> and <c>localName</c> return values.</remarks>
    /// <param name="qName">Qualified name.</param>
    /// <param name="prefix">Prefix of qualified name.</param>
    /// <param name="localName">Local part of qualified name.</param>
    public static void SplitQName(string qName, out string prefix, out string localName)
    {
      int colIndex = qName.IndexOf(':');
      if (colIndex == -1) {
        prefix = String.Empty;
        localName = qName;
      }
      else {
        prefix = qName.Substring(0, colIndex);
        localName = qName.Substring(colIndex + 1);
      }
    }

    /// <summary>Returns character type for XML character processing.</summary>
    /// <remarks>Useful for character and name checking routines.</remarks>
    public static CharType GetCharType(CharStruct cs) 
    {
      if (cs.Hi == 0)
        return Latin1ByteTypes[cs.Lo];
      else {
        switch (cs.Hi) {
          case 0xD8: case 0xD9: case 0xDA: case 0xDB:
            return CharType.LEAD4;
          case 0xDC: case 0xDD: case 0xDE: case 0xDF:
            return CharType.TRAIL;
          case 0xFF:
            if (cs.Lo == 0xFF || cs.Lo == 0xFE)
              return CharType.NONXML;
            else 
              return CharType.NONASCII;
          default:
            return CharType.NONASCII;
        }
      }
    }

    /// <summary>Returns if character is acceptable for XML name tokens.</summary>
    /// <seealso href="http://www.w3.org/TR/REC-xml/#NT-Nmtoken">NmToken Production on w3.org.</seealso>
    public static bool IsNmTokenType(CharStruct cs)
    {
      switch (GetCharType(cs)) {
        case CharType.NONASCII:
          if ((NamingBitmap[(NamePages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
            return false;
          break;
        case CharType.NMSTRT: case CharType.HEX: case CharType.DIGIT:
        case CharType.NAME: case CharType.MINUS: case CharType.COLON:
          break;  // OK
        default:
          return false;
      }
      return true;
    }

    /// <summary>Returns if a character is acceptable for XML name tokens
    /// conforming to the "Namespaces Constraints".</summary>.
    /// <remarks>Identical to <see cref="IsNmTokenType"/> except
    /// that colons are not allowed.</remarks>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#NT-NCName">NCNameChar Production on w3.org.</seealso>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#Conformance">Namespaces Constraints on w3.org.</seealso>
    public static bool IsNcNmTokenType(CharStruct cs)
    {
      switch (GetCharType(cs)) {
        case CharType.NONASCII:
          if ((NamingBitmap[(NamePages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
            return false;
          break;
        case CharType.NMSTRT: case CharType.HEX: case CharType.DIGIT:
        case CharType.NAME: case CharType.MINUS: 
          break;  // OK
        case CharType.COLON: 
          return false;
        default:
          return false;
      }
      return true;
    }

    /// <summary>Returns if a character is acceptable as an XML name start character.</summary>.
    /// <seealso href="http://www.w3.org/TR/REC-xml/#NT-Name">Name production on w3.org.</seealso>
    public static bool IsNameStartType(CharStruct cs)
    {
      switch (GetCharType(cs)) {
        case CharType.NONASCII:
          if ((NamingBitmap[(NmStartPages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
            return false;
          break;
        case CharType.NMSTRT: case CharType.HEX: case CharType.COLON: 
          break;  // OK
        default:
          return false;
      }
      return true;
    }

    /// <summary>Returns if a character is acceptable as an XML name start
    /// character conforming to the "Namespaces Constraints".</summary>.
    /// <remarks>Identical to <see cref="IsNameStartType"/> except
    /// that colons are not allowed.</remarks>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#NT-NCName">NCName production on w3.org.</seealso>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#Conformance">Namespaces Constraints on w3.org.</seealso>
    public static bool IsNcNameStartType(CharStruct cs)
    {
      switch (GetCharType(cs)) {
        case CharType.NONASCII:
          if ((NamingBitmap[(NmStartPages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
            return false;
          break;
        case CharType.NMSTRT: case CharType.HEX: 
          break;  // OK
        case CharType.COLON: 
          return false;
        default:
          return false;
      }
      return true;
    }

    /// <overloads>
    /// <summary>Checks if a string of characters is a well-formed XML name token.</summary>
    /// <returns><c>true</c> if string is a valid XML name token, <c>false</c> otherwise.</returns>
    /// <seealso href="http://www.w3.org/TR/REC-xml/#NT-Nmtoken">NmToken production on w3.org.</seealso>
    /// </overloads>
    /// <remarks>Contains <c>unsafe</c> code tuned for performance.</remarks>
    /// <param name="nmTokPtr">Pointer to first character in string to be checked.</param>
    /// <param name="len">Length of string.</param>
    public static unsafe bool IsNmToken(char* nmTokPtr, int len)
    {
      if (len <= 0)
        return false;
      do {
        CharStruct cs = new CharStruct(*nmTokPtr);
        // inlined call to GetCharType(cs)
        CharType ct;
        if (cs.Hi == 0)
          ct = Latin1ByteTypes[cs.Lo];
        else {
          switch (cs.Hi) {
            case 0xD8: case 0xD9: case 0xDA: case 0xDB:
              ct = CharType.LEAD4;
              break;
            case 0xDC: case 0xDD: case 0xDE: case 0xDF:
              ct = CharType.TRAIL;
              break;
            case 0xFF:
              if (cs.Lo == 0xFF || cs.Lo == 0xFE)
                ct = CharType.NONXML;
              else 
                ct = CharType.NONASCII;
              break;
            default:
              ct = CharType.NONASCII;
              break;
          }
        }
        // inlined call to IsNmTokenType(cs)
        switch (ct) {
          case CharType.NONASCII:
            if ((NamingBitmap[(NamePages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
              return false;
            break;
          case CharType.NMSTRT: case CharType.HEX: case CharType.DIGIT:
          case CharType.NAME: case CharType.MINUS: case CharType.COLON:
            break;  // OK
          default:
            return false;
        }
        nmTokPtr++;
        len--;
      } while (len != 0);
      return true;
    }

    /// <param name="chars">Character array containing string to be checked.</param>
    /// <param name="start">Start index of string.</param>
    /// <param name="len">Length of string.</param>
    public static bool IsNmToken(char[] chars, int start, int len)
    {
      if (len <= 0)
        return false;
      int endIndx = start + len;
      for (int indx = start; indx < endIndx; indx++) {
        CharStruct cs = new CharStruct(chars[indx]);
        if (!IsNmTokenType(cs))
          return false;
      }
      return true;
    }

    /// <param name="str">String to be checked.</param>
    public static bool IsNmToken(string str)
    {
      if (str == String.Empty)
        return false;
      for (int indx = 0; indx < str.Length; indx++) {
        CharStruct cs = new CharStruct(str[indx]);
        if (!IsNmTokenType(cs))
          return false;
      }
      return true;
    }

    /// <param name="str">String containing sub-string to be checked.</param>
    /// <param name="start">Start index of sub-string.</param>
    /// <param name="len">Length of sub-string.</param>
    public static bool IsNmToken(string str, int start, int len)
    {
      if (len <= 0)
        return false;
      int endIndx = start + len;
      for (int indx = start; indx < str.Length; indx++) {
        if (indx == endIndx)
          break;
        CharStruct cs = new CharStruct(str[indx]);
        if (!IsNmTokenType(cs))
          return false;
      }
      return true;
    }

    /// <overloads>
    /// <summary>Checks if a string of characters is a well-formed XML name token
    /// conforming to the "Namespaces Constraints".</summary>
    /// <returns><c>true</c> if string is a valid XML name token, <c>false</c> otherwise.</returns>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#NT-NCName">NCNameChar Production on w3.org.</seealso>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#Conformance">Namespaces Constraints on w3.org.</seealso>
    /// </overloads>
    /// <remarks>Contains <c>unsafe</c> code tuned for performance.</remarks>
    /// <param name="nmTokPtr">Pointer to first character in string to be checked.</param>
    /// <param name="len">Length of string.</param>
    public static unsafe bool IsNcNmToken(char* nmTokPtr, int len)
    {
      if (len <= 0)
        return false;
      do {
        CharStruct cs = new CharStruct(*nmTokPtr);
        // inlined call to GetCharType(cs)
        CharType ct;
        if (cs.Hi == 0)
          ct = Latin1ByteTypes[cs.Lo];
        else {
          switch (cs.Hi) {
            case 0xD8: case 0xD9: case 0xDA: case 0xDB:
              ct = CharType.LEAD4;
              break;
            case 0xDC: case 0xDD: case 0xDE: case 0xDF:
              ct = CharType.TRAIL;
              break;
            case 0xFF:
              if (cs.Lo == 0xFF || cs.Lo == 0xFE)
                ct = CharType.NONXML;
              else 
                ct = CharType.NONASCII;
              break;
            default:
              ct = CharType.NONASCII;
              break;
          }
        }
        // inlined call to IsNcNmTokenType(cs)
        switch (ct) {
          case CharType.NONASCII:
            if ((NamingBitmap[(NamePages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
              return false;
            break;
          case CharType.NMSTRT: case CharType.HEX: case CharType.DIGIT:
          case CharType.NAME: case CharType.MINUS: 
            break;  // OK
          case CharType.COLON: 
            return false;
          default:
            return false;
        }
        nmTokPtr++;
        len--;
      } while (len != 0);
      return true;
    }

    /// <param name="chars">Character array containing string to be checked.</param>
    /// <param name="start">Start index of string.</param>
    /// <param name="len">Length of string.</param>
    public static bool IsNcNmToken(char[] chars, int start, int len)
    {
      if (len <= 0)
        return false;
      int endIndx = start + len;
      for (int indx = start; indx < endIndx; indx++) {
        CharStruct cs = new CharStruct(chars[indx]);
        if (!IsNcNmTokenType(cs))
          return false;
      }
      return true;
    }

    /// <param name="str">String to be checked.</param>
    public static bool IsNcNmToken(string str)
    {
      if (str == String.Empty)
        return false;
      for (int indx = 0; indx < str.Length; indx++) {
        CharStruct cs = new CharStruct(str[indx]);
        if (!IsNcNmTokenType(cs))
          return false;
      }
      return true;
    }

    /// <param name="str">String containing sub-string to be checked.</param>
    /// <param name="start">Start index of sub-string.</param>
    /// <param name="len">Length of sub-string.</param>
    public static bool IsNcNmToken(string str, int start, int len)
    {
      if (len <= 0)
        return false;
      int endIndx = start + len;
      for (int indx = start; indx < str.Length; indx++) {
        if (indx == endIndx)
          break;
        CharStruct cs = new CharStruct(str[indx]);
        if (!IsNcNmTokenType(cs))
          return false;
      }
      return true;
    }

    /// <overloads>
    /// <summary>Checks if string of characters is a valid XML name.</summary>
    /// <returns><c>true</c> if string is a valid XML name, <c>false</c> otherwise.</returns>
    /// <seealso href="http://www.w3.org/TR/REC-xml/#NT-Name">Name production on w3.org.</seealso>
    /// </overloads>
    /// <remarks>Contains <c>unsafe</c> code tuned for performance.</remarks>
    /// <param name="namePtr">Pointer to first character in string to be checked.</param>
    /// <param name="len">Length of string.</param>
    public static unsafe bool IsName(char* namePtr, int len)
    {
      if (len <= 0)
        return false;
      CharStruct cs = new CharStruct(*namePtr);
      CharType ct;
      // inlined call to GetCharType(cs)
      if (cs.Hi == 0)
        ct = Latin1ByteTypes[cs.Lo];
      else {
        switch (cs.Hi) {
          case 0xD8: case 0xD9: case 0xDA: case 0xDB:
            ct = CharType.LEAD4;
            break;
          case 0xDC: case 0xDD: case 0xDE: case 0xDF:
            ct = CharType.TRAIL;
            break;
          case 0xFF:
            if (cs.Lo == 0xFF || cs.Lo == 0xFE)
              ct = CharType.NONXML;
            else 
              ct = CharType.NONASCII;
            break;
          default:
            ct = CharType.NONASCII;
            break;
        }
      }
      // inlined call to IsNameStartType(cs)
      switch (ct) {
        case CharType.NONASCII:
          if ((NamingBitmap[(NmStartPages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
            return false;
          break;
        case CharType.NMSTRT: case CharType.HEX: case CharType.COLON:
          break;  // OK
        default:
          return false;
      }
      len--;
      if (len == 0)
        return true;
      else {
        namePtr++;
        return IsNmToken(namePtr, len);
      }
    }

    /// <param name="chars">Character array containing string to be checked.</param>
    /// <param name="start">Start index of string.</param>
    /// <param name="len">Length of string.</param>
    public static bool IsName(char[] chars, int start, int len)
    {
      if (len <= 0)
        return false;
      CharStruct cs = new CharStruct(chars[start]);
      if (!IsNameStartType(cs))
        return false;
      len--;
      if (len == 0)
        return true;
      else {
        start++;
        return IsNmToken(chars, start, len);
      }
    }

    /// <param name="str">String to be checked.</param>
    public static bool IsName(string str)
    {
      if (str == String.Empty)
        return false;
      CharStruct cs = new CharStruct(str[0]);
      if (!IsNameStartType(cs))
        return false;
      if (str.Length == 1)
        return true;
      else
        return IsNmToken(str, 1, str.Length - 1);
    }

    /// <summary>Checks if <c>name</c> argument is a well-formed XML name.</summary>
    /// <param name="name">XML name to be checked.</param>
    /// <exception cref="ArgumentException">Thrown when <c>name</c> argument is not well-formed.</exception>
    public static void CheckName(string name)
    {
      if (!IsName(name)) {
        string msg = Resources.GetString(RsId.InvalidXmlName);
        throw new ArgumentException(String.Format(msg, name));
      }
    }

    /// <overloads>
    /// <summary>Checks if a string of characters is a valid XML name
    /// conforming to the "Namespaces Constraints".</summary>
    /// <remarks>This applies to <see href="http://w3.org/TR/REC-xml-names/#NT-NCName">NCNames</see>
    /// but not to <see href="http://w3.org/TR/REC-xml-names/#ns-qualnames">Qualified Names</see>,
    /// so prefixes separated by a colon are not allowed.</remarks>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#NT-NCName">NCName production on w3.org.</seealso>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#Conformance">Namespaces Constraints on w3.org.</seealso>
    /// <returns><c>true</c> if string is a valid XML name, <c>false</c> otherwise.</returns>
    /// </overloads>
    /// <remarks>Contains <c>unsafe</c> code tuned for performance.</remarks>
    /// <param name="namePtr">Pointer to first character in string to be checked.</param>
    /// <param name="len">Length of string.</param>
    public static unsafe bool IsNcName(char* namePtr, int len)
    {
      if (len <= 0)
        return false;
      CharStruct cs = new CharStruct(*namePtr);
      CharType ct;
      // inlined call to GetCharType(cs)
      if (cs.Hi == 0)
        ct = Latin1ByteTypes[cs.Lo];
      else {
        switch (cs.Hi) {
          case 0xD8: case 0xD9: case 0xDA: case 0xDB:
            ct = CharType.LEAD4;
            break;
          case 0xDC: case 0xDD: case 0xDE: case 0xDF:
            ct = CharType.TRAIL;
            break;
          case 0xFF:
            if (cs.Lo == 0xFF || cs.Lo == 0xFE)
              ct = CharType.NONXML;
            else 
              ct = CharType.NONASCII;
            break;
          default:
            ct = CharType.NONASCII;
            break;
        }
      }
      // inlined call to IsNcNameStartType(cs)
      switch (ct) {
        case CharType.NONASCII:
          if ((NamingBitmap[(NmStartPages[cs.Hi] << 3) + (cs.Lo >> 5)] & (1 << (cs.Lo & 0x1F))) == 0) 
            return false;
          break;
        case CharType.NMSTRT: case CharType.HEX: 
          break;  // OK
        case CharType.COLON:
          return false;
        default:
          return false;
      }
      len--;
      if (len == 0)
        return true;
      else {
        namePtr++;
        return IsNcNmToken(namePtr, len);
      }
    }

    /// <param name="chars">Character array containing string to be checked.</param>
    /// <param name="start">Start index of string.</param>
    /// <param name="len">Length of string.</param>
    public static bool IsNcName(char[] chars, int start, int len)
    {
      if (len <= 0)
        return false;
      CharStruct cs = new CharStruct(chars[start]);
      if (!IsNcNameStartType(cs))
        return false;
      len--;
      if (len == 0)
        return true;
      else {
        start++;
        return IsNcNmToken(chars, start, len);
      }
    }

    /// <param name="str">String to be checked.</param>
    public static bool IsNcName(string str)
    {
      if (str == String.Empty)
        return false;
      CharStruct cs = new CharStruct(str[0]);
      if (!IsNcNameStartType(cs))
        return false;
      if (str.Length == 1)
        return true;
      else
        return IsNcNmToken(str, 1, str.Length - 1);
    }

    /// <summary>Checks if <c>name</c> argument is a well-formed XML name
    /// conforming to the "Namespaces Constraints".</summary>
    /// <param name="name">XML name to be checked.</param>
    /// <exception cref="ArgumentException">Thrown when <c>name</c> argument is not well-formed.</exception>
    /// <seealso href="http://w3.org/TR/REC-xml-names/#Conformance">Namespaces Constraints on w3.org.</seealso>
    public static void CheckNcName(string name)
    {
      if (!IsNcName(name)) {
        string msg = Resources.GetString(RsId.InvalidXmlName);
        throw new ArgumentException(String.Format(msg, name));
      }
    }

    /// <overloads>
    /// <summary>Checks if UTF-16 encoded string contains valid XML characters.</summary>
    /// <remarks>If the return value indicates an invalid character then this means
    /// that either a complete but invalid character was found, or that there are not
    /// enough bytes left to form a complete character. If the second part of a surrogate
    /// pair is invalid or missing then the return value points to the first part.</remarks>
    /// </overloads>
    /// <remarks>Contains <c>unsafe</c> code tuned for performance.</remarks>
    /// <param name="strPtr">Pointer to first character in string.</param>
    /// <param name="len">Length of string.</param>
    /// <returns>Pointer to first invalid character, or <c>null</c> if string valid.</returns>
    public static unsafe char* CheckStringValid(char* strPtr, int len)
    {
      if (len <= 0)
        return null;
      bool surrogate = false;
      char* endPtr = strPtr + len;
      while (strPtr < endPtr) {
        CharStruct cs = new CharStruct(*strPtr);
        // inlined call to GetCharType(cs)
        CharType ct;
        if (cs.Hi == 0)
          ct = Latin1ByteTypes[cs.Lo];
        else {
          switch (cs.Hi) {
            case 0xD8: case 0xD9: case 0xDA: case 0xDB:
              ct = CharType.LEAD4;
              break;
            case 0xDC: case 0xDD: case 0xDE: case 0xDF:
              ct = CharType.TRAIL;
              break;
            case 0xFF:
              if (cs.Lo == 0xFF || cs.Lo == 0xFE)
                ct = CharType.NONXML;
              else 
                ct = CharType.NONASCII;
              break;
            default:
              ct = CharType.NONASCII;
              break;
          }
        }
        if (surrogate) {
          if (ct == CharType.TRAIL)
            strPtr++;
          else  // return pointer to first part of surrogate pair
            return --strPtr;
          surrogate = false;
        }
        else {
          switch (ct) {
            case CharType.LEAD4:
              if ((endPtr - strPtr) < 2)
                return strPtr;
              surrogate = true;
              strPtr++;
              break;
            case CharType.NONXML: case CharType.MALFORM: case CharType.TRAIL:
              return strPtr;
            default:
              strPtr++;
              break;
          }
        }
      }
      return null;
    }

    /// <param name="chars">Array containing the string to be checked.</param>
    /// <param name="start">Start index of string.</param>
    /// <param name="len">Length of string.</param>
    /// <returns>Array index of first invalid character, or <c>-1</c> if string valid.</returns>
    public static int CheckStringValid(char[] chars, int start, int len)
    {
      if (len <= 0)
        return -1;
      bool surrogate = false;
      int endIndx = start + len;
      // use chars.Length so that compiler can eliminate bounds checking
      for (int indx = start; indx < chars.Length; indx++) {
        if (indx == endIndx)
          break;
        CharType ct = GetCharType(new CharStruct(chars[indx]));
        if (surrogate) {
          if (ct != CharType.TRAIL)  // return index of first part of surrogate pair
            return indx - 1;
          surrogate = false;
        }
        else {
          switch (ct) {
            case CharType.LEAD4:
              if (endIndx - indx < 2)
                return indx;
              surrogate = true;
              break;
            case CharType.NONXML: case CharType.MALFORM: case CharType.TRAIL:
              return indx;
            default:
              break;
          }
        }
      }
      return -1;
    }

    /// <param name="str">String to be checked.</param>
    /// <returns>String index of first invalid character, or <c>-1</c> if string valid.</returns>
    public static int CheckStringValid(string str)
    {
      if (str == String.Empty)
        return -1;
      bool surrogate = false;
      int len = str.Length;
      // use str.Length so that compiler can eliminate bounds checking
      for (int indx = 0; indx < str.Length; indx++) {
        CharType ct = GetCharType(new CharStruct(str[indx]));
        if (surrogate) {
          if (ct != CharType.TRAIL)  // return index of first part of surrogate pair
            return indx - 1;
          surrogate = false;
        }
        else {
          switch (ct) {
            case CharType.LEAD4:
              if (len - indx < 2)
                return indx;
              surrogate = true;
              break;
            case CharType.NONXML: case CharType.MALFORM: case CharType.TRAIL:
              return indx;
            default:
              break;
          }
        }
      }
      return -1;
    }

    /// <summary>Table for use with character and name checking routines.</summary>
    public static readonly CharType[] Latin1ByteTypes = new CharType[256]
    {
      /* 0x00 */ CharType.NONXML, CharType.NONXML, CharType.NONXML, CharType.NONXML,
      /* 0x04 */ CharType.NONXML, CharType.NONXML, CharType.NONXML, CharType.NONXML,
      /* 0x08 */ CharType.NONXML, CharType.S, CharType.LF, CharType.NONXML,
      /* 0x0C */ CharType.NONXML, CharType.CR, CharType.NONXML, CharType.NONXML,
      /* 0x10 */ CharType.NONXML, CharType.NONXML, CharType.NONXML, CharType.NONXML,
      /* 0x14 */ CharType.NONXML, CharType.NONXML, CharType.NONXML, CharType.NONXML,
      /* 0x18 */ CharType.NONXML, CharType.NONXML, CharType.NONXML, CharType.NONXML,
      /* 0x1C */ CharType.NONXML, CharType.NONXML, CharType.NONXML, CharType.NONXML,
      /* 0x20 */ CharType.S, CharType.EXCL, CharType.QUOT, CharType.NUM,
      /* 0x24 */ CharType.OTHER, CharType.PERCNT, CharType.AMP, CharType.APOS,
      /* 0x28 */ CharType.LPAR, CharType.RPAR, CharType.AST, CharType.PLUS,
      /* 0x2C */ CharType.COMMA, CharType.MINUS, CharType.NAME, CharType.SOL,
      /* 0x30 */ CharType.DIGIT, CharType.DIGIT, CharType.DIGIT, CharType.DIGIT,
      /* 0x34 */ CharType.DIGIT, CharType.DIGIT, CharType.DIGIT, CharType.DIGIT,
      /* 0x38 */ CharType.DIGIT, CharType.DIGIT, CharType.COLON, CharType.SEMI,
      /* 0x3C */ CharType.LT, CharType.EQUALS, CharType.GT, CharType.QUEST,
      /* 0x40 */ CharType.OTHER, CharType.HEX, CharType.HEX, CharType.HEX,
      /* 0x44 */ CharType.HEX, CharType.HEX, CharType.HEX, CharType.NMSTRT,
      /* 0x48 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x4C */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x50 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x54 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x58 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.LSQB,
      /* 0x5C */ CharType.OTHER, CharType.RSQB, CharType.OTHER, CharType.NMSTRT,
      /* 0x60 */ CharType.OTHER, CharType.HEX, CharType.HEX, CharType.HEX,
      /* 0x64 */ CharType.HEX, CharType.HEX, CharType.HEX, CharType.NMSTRT,
      /* 0x68 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x6C */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x70 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x74 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0x78 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.OTHER,
      /* 0x7C */ CharType.VERBAR, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x80 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x84 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x88 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x8C */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x90 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x94 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x98 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0x9C */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0xA0 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0xA4 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0xA8 */ CharType.OTHER, CharType.OTHER, CharType.NMSTRT, CharType.OTHER,
      /* 0xAC */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0xB0 */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0xB4 */ CharType.OTHER, CharType.NMSTRT, CharType.OTHER, CharType.NAME,
      /* 0xB8 */ CharType.OTHER, CharType.OTHER, CharType.NMSTRT, CharType.OTHER,
      /* 0xBC */ CharType.OTHER, CharType.OTHER, CharType.OTHER, CharType.OTHER,
      /* 0xC0 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xC4 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xC8 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xCC */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xD0 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xD4 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.OTHER,
      /* 0xD8 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xDC */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xE0 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xE4 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xE8 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xEC */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xF0 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xF4 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.OTHER,
      /* 0xF8 */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT,
      /* 0xFC */ CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT, CharType.NMSTRT
    };

    /// <summary>Table for use with character and name checking routines.</summary>
    public static readonly long[] NamingBitmap = new long[320]
    {
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
      0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
      0x00000000, 0x04000000, 0x87FFFFFE, 0x07FFFFFE,
      0x00000000, 0x00000000, 0xFF7FFFFF, 0xFF7FFFFF,
      0xFFFFFFFF, 0x7FF3FFFF, 0xFFFFFDFE, 0x7FFFFFFF,
      0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFE00F, 0xFC31FFFF,
      0x00FFFFFF, 0x00000000, 0xFFFF0000, 0xFFFFFFFF,
      0xFFFFFFFF, 0xF80001FF, 0x00000003, 0x00000000,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0xFFFFD740, 0xFFFFFFFB, 0x547F7FFF, 0x000FFFFD,
      0xFFFFDFFE, 0xFFFFFFFF, 0xDFFEFFFF, 0xFFFFFFFF,
      0xFFFF0003, 0xFFFFFFFF, 0xFFFF199F, 0x033FCFFF,
      0x00000000, 0xFFFE0000, 0x027FFFFF, 0xFFFFFFFE,
      0x0000007F, 0x00000000, 0xFFFF0000, 0x000707FF,
      0x00000000, 0x07FFFFFE, 0x000007FE, 0xFFFE0000,
      0xFFFFFFFF, 0x7CFFFFFF, 0x002F7FFF, 0x00000060,
      0xFFFFFFE0, 0x23FFFFFF, 0xFF000000, 0x00000003,
      0xFFF99FE0, 0x03C5FDFF, 0xB0000000, 0x00030003,
      0xFFF987E0, 0x036DFDFF, 0x5E000000, 0x001C0000,
      0xFFFBAFE0, 0x23EDFDFF, 0x00000000, 0x00000001,
      0xFFF99FE0, 0x23CDFDFF, 0xB0000000, 0x00000003,
      0xD63DC7E0, 0x03BFC718, 0x00000000, 0x00000000,
      0xFFFDDFE0, 0x03EFFDFF, 0x00000000, 0x00000003,
      0xFFFDDFE0, 0x03EFFDFF, 0x40000000, 0x00000003,
      0xFFFDDFE0, 0x03FFFDFF, 0x00000000, 0x00000003,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0xFFFFFFFE, 0x000D7FFF, 0x0000003F, 0x00000000,
      0xFEF02596, 0x200D6CAE, 0x0000001F, 0x00000000,
      0x00000000, 0x00000000, 0xFFFFFEFF, 0x000003FF,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0x00000000, 0xFFFFFFFF, 0xFFFF003F, 0x007FFFFF,
      0x0007DAED, 0x50000000, 0x82315001, 0x002C62AB,
      0x40000000, 0xF580C900, 0x00000007, 0x02010800,
      0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
      0x0FFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0x03FFFFFF,
      0x3F3FFFFF, 0xFFFFFFFF, 0xAAFF3F3F, 0x3FFFFFFF,
      0xFFFFFFFF, 0x5FDFFFFF, 0x0FCF1FDC, 0x1FDC1FFF,
      0x00000000, 0x00004C40, 0x00000000, 0x00000000,
      0x00000007, 0x00000000, 0x00000000, 0x00000000,
      0x00000080, 0x000003FE, 0xFFFFFFFE, 0xFFFFFFFF,
      0x001FFFFF, 0xFFFFFFFE, 0xFFFFFFFF, 0x07FFFFFF,
      0xFFFFFFE0, 0x00001FFF, 0x00000000, 0x00000000,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
      0xFFFFFFFF, 0x0000003F, 0x00000000, 0x00000000,
      0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
      0xFFFFFFFF, 0x0000000F, 0x00000000, 0x00000000,
      0x00000000, 0x07FF6000, 0x87FFFFFE, 0x07FFFFFE,
      0x00000000, 0x00800000, 0xFF7FFFFF, 0xFF7FFFFF,
      0x00FFFFFF, 0x00000000, 0xFFFF0000, 0xFFFFFFFF,
      0xFFFFFFFF, 0xF80001FF, 0x00030003, 0x00000000,
      0xFFFFFFFF, 0xFFFFFFFF, 0x0000003F, 0x00000003,
      0xFFFFD7C0, 0xFFFFFFFB, 0x547F7FFF, 0x000FFFFD,
      0xFFFFDFFE, 0xFFFFFFFF, 0xDFFEFFFF, 0xFFFFFFFF,
      0xFFFF007B, 0xFFFFFFFF, 0xFFFF199F, 0x033FCFFF,
      0x00000000, 0xFFFE0000, 0x027FFFFF, 0xFFFFFFFE,
      0xFFFE007F, 0xBBFFFFFB, 0xFFFF0016, 0x000707FF,
      0x00000000, 0x07FFFFFE, 0x0007FFFF, 0xFFFF03FF,
      0xFFFFFFFF, 0x7CFFFFFF, 0xFFEF7FFF, 0x03FF3DFF,
      0xFFFFFFEE, 0xF3FFFFFF, 0xFF1E3FFF, 0x0000FFCF,
      0xFFF99FEE, 0xD3C5FDFF, 0xB080399F, 0x0003FFCF,
      0xFFF987E4, 0xD36DFDFF, 0x5E003987, 0x001FFFC0,
      0xFFFBAFEE, 0xF3EDFDFF, 0x00003BBF, 0x0000FFC1,
      0xFFF99FEE, 0xF3CDFDFF, 0xB0C0398F, 0x0000FFC3,
      0xD63DC7EC, 0xC3BFC718, 0x00803DC7, 0x0000FF80,
      0xFFFDDFEE, 0xC3EFFDFF, 0x00603DDF, 0x0000FFC3,
      0xFFFDDFEC, 0xC3EFFDFF, 0x40603DDF, 0x0000FFC3,
      0xFFFDDFEC, 0xC3FFFDFF, 0x00803DCF, 0x0000FFC3,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0xFFFFFFFE, 0x07FF7FFF, 0x03FF7FFF, 0x00000000,
      0xFEF02596, 0x3BFF6CAE, 0x03FF3F5F, 0x00000000,
      0x03000000, 0xC2A003FF, 0xFFFFFEFF, 0xFFFE03FF,
      0xFEBF0FDF, 0x02FE3FFF, 0x00000000, 0x00000000,
      0x00000000, 0x00000000, 0x00000000, 0x00000000,
      0x00000000, 0x00000000, 0x1FFF0000, 0x00000002,
      0x000000A0, 0x003EFFFE, 0xFFFFFFFE, 0xFFFFFFFF,
      0x661FFFFF, 0xFFFFFFFE, 0xFFFFFFFF, 0x77FFFFFF
    };

    /// <summary>Table for use with character and name checking routines.</summary>
    public static readonly byte[] NmStartPages = new byte[256]
    {
      0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x00,
      0x00, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
      0x10, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x13,
      0x00, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x15, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x17,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x18,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    /// <summary>Table for use with character and name checking routines.</summary>
    public static readonly byte[] NamePages = new byte[256]
    {
      0x19, 0x03, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x00,
      0x00, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25,
      0x10, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x12, 0x13,
      0x26, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x27, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x17,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
      0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x18,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };
  }
}