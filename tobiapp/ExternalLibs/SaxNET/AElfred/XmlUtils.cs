using System;

/*** License **********************************************************
*
* Copyright (c) 2005, Jeff Rafter
*
* This file is part of the AElfred C# library. 
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

namespace AElfred 
{

  public class XmlUtils {

    public static bool isXmlNameChar(char c) {
      return (c == '.') || 
        (c == '-') ||
        (c == '_') ||
        (c == ':') || 
        ((isXmlLetter(c)) ||
        (isXmlDigit(c)) ||
        (isXmlCombiningChar(c)) ||
        (isXmlExtender(c)));           
    }

    public static bool isXmlExtender(char c) {
      return (c == '\x00B7') || (c == '\x02D0') || (c == '\x02D1') || (c == '\x0387') || (c == '\x0640') || (c == '\x0E46') || (c == '\x0EC6') || (c == '\x3005') ||
        (c >= '\x3031' && c <= '\x3035') || (c >= '\x309D' && c <= '\x309E') || (c >= '\x30FC' && c <= '\x30FE'); 
    }

    public static bool isXmlDigit(char c) {
      return (c >= '\x0030' && c <= '\x0039') || (c >= '\x0660' && c <= '\x0669') || (c >= '\x06F0' && c <= '\x06F9') || (c >= '\x0966' && c <= '\x096F') ||
        (c >= '\x09E6' && c <= '\x09EF') || (c >= '\x0A66' && c <= '\x0A6F') || (c >= '\x0AE6' && c <= '\x0AEF') || (c >= '\x0B66' && c <= '\x0B6F') ||
        (c >= '\x0BE7' && c <= '\x0BEF') || (c >= '\x0C66' && c <= '\x0C6F') || (c >= '\x0CE6' && c <= '\x0CEF') || (c >= '\x0D66' && c <= '\x0D6F') ||
        (c >= '\x0E50' && c <= '\x0E59') || (c >= '\x0ED0' && c <= '\x0ED9') || (c >= '\x0F20' && c <= '\x0F29');
    }

    public static bool isXmlCombiningChar(char c)  {
      return (c >= '\x0300' && c <= '\x0345') || (c >= '\x0360' && c <= '\x0361') || (c >= '\x0483' && c <= '\x0486') || (c >= '\x0591' && c <= '\x05A1') ||
        (c >= '\x05A3' && c <= '\x05B9') || (c >= '\x05BB' && c <= '\x05BD') || (c == '\x05BF') || (c >= '\x05C1' && c <= '\x05C2') || (c == '\x05C4') ||
        (c >= '\x064B' && c <= '\x0652') || (c == '\x0670') || (c >= '\x06D6' && c <= '\x06DC') || (c >= '\x06DD' && c <= '\x06DF') ||
        (c >= '\x06E0' && c <= '\x06E4') || (c >= '\x06E7' && c <= '\x06E8') || (c >= '\x06EA' && c <= '\x06ED') || (c >= '\x0901' && c <= '\x0903') ||
        (c == '\x093C') || (c >= '\x093E' && c <= '\x094C') || (c == '\x094D') || (c >= '\x0951' && c <= '\x0954') || (c >= '\x0962' && c <= '\x0963') ||
        (c >= '\x0981' && c <= '\x0983') || (c == '\x09BC') || (c == '\x09BE') || (c == '\x09BF') || (c >= '\x09C0' && c <= '\x09C4') ||
        (c >= '\x09C7' && c <= '\x09C8') || (c >= '\x09CB' && c <= '\x09CD') || (c == '\x09D7') || (c >= '\x09E2' && c <= '\x09E3') || (c == '\x0A02') ||
        (c == '\x0A3C') || (c == '\x0A3E') || (c == '\x0A3F') || (c >= '\x0A40' && c <= '\x0A42') || (c >= '\x0A47' && c <= '\x0A48') ||
        (c >= '\x0A4B' && c <= '\x0A4D') || (c >= '\x0A70' && c <= '\x0A71') || (c >= '\x0A81' && c <= '\x0A83') || (c == '\x0ABC') ||
        (c >= '\x0ABE' && c <= '\x0AC5') || (c >= '\x0AC7' && c <= '\x0AC9') || (c >= '\x0ACB' && c <= '\x0ACD') || (c >= '\x0B01' && c <= '\x0B03') ||
        (c == '\x0B3C') || (c >= '\x0B3E' && c <= '\x0B43') || (c >= '\x0B47' && c <= '\x0B48') || (c >= '\x0B4B' && c <= '\x0B4D') ||
        (c >= '\x0B56' && c <= '\x0B57') || (c >= '\x0B82' && c <= '\x0B83') || (c >= '\x0BBE' && c <= '\x0BC2') || (c >= '\x0BC6' && c <= '\x0BC8') ||
        (c >= '\x0BCA' && c <= '\x0BCD') || (c == '\x0BD7') || (c >= '\x0C01' && c <= '\x0C03') || (c >= '\x0C3E' && c <= '\x0C44') ||
        (c >= '\x0C46' && c <= '\x0C48') || (c >= '\x0C4A' && c <= '\x0C4D') || (c >= '\x0C55' && c <= '\x0C56') || (c >= '\x0C82' && c <= '\x0C83') ||
        (c >= '\x0CBE' && c <= '\x0CC4') || (c >= '\x0CC6' && c <= '\x0CC8') || (c >= '\x0CCA' && c <= '\x0CCD') || (c >= '\x0CD5' && c <= '\x0CD6') ||
        (c >= '\x0D02' && c <= '\x0D03') || (c >= '\x0D3E' && c <= '\x0D43') || (c >= '\x0D46' && c <= '\x0D48') || (c >= '\x0D4A' && c <= '\x0D4D') ||
        (c == '\x0D57') || (c == '\x0E31') || (c >= '\x0E34' && c <= '\x0E3A') || (c >= '\x0E47' && c <= '\x0E4E') || (c == '\x0EB1') ||
        (c >= '\x0EB4' && c <= '\x0EB9') || (c >= '\x0EBB' && c <= '\x0EBC') || (c >= '\x0EC8' && c <= '\x0ECD') || (c >= '\x0F18' && c <= '\x0F19') ||
        (c == '\x0F35') || (c == '\x0F37') || (c == '\x0F39') || (c == '\x0F3E') || (c == '\x0F3F') || (c >= '\x0F71' && c <= '\x0F84') ||
        (c >= '\x0F86' && c <= '\x0F8B') || (c >= '\x0F90' && c <= '\x0F95') || (c == '\x0F97') || (c >= '\x0F99' && c <= '\x0FAD') ||
        (c >= '\x0FB1' && c <= '\x0FB7') || (c == '\x0FB9') || (c >= '\x20D0' && c <= '\x20DC') || (c == '\x20E1') || (c >= '\x302A' && c <= '\x302F') ||
        (c == '\x3099') || (c == '\x309A');
    }

    public static bool isXmlIdeographic(char c) {
      return (c >= '\x4E00' && c <= '\x9FA5') || (c == '\x3007') || (c >= '\x3021' && c <= '\x3029');
    }

    public static bool isXmlBaseChar(char c) {  
      return (c >= '\x0041' && c <= '\x005A') || (c >= '\x0061' && c <= '\x007A') || (c >= '\x00C0' && c <= '\x00D6') || (c >= '\x00D8' && c <= '\x00F6') ||
        (c >= '\x00F8' && c <= '\x00FF') || (c >= '\x0100' && c <= '\x0131') || (c >= '\x0134' && c <= '\x013E') || (c >= '\x0141' && c <= '\x0148') ||
        (c >= '\x014A' && c <= '\x017E') || (c >= '\x0180' && c <= '\x01C3') || (c >= '\x01CD' && c <= '\x01F0') || (c >= '\x01F4' && c <= '\x01F5') ||
        (c >= '\x01FA' && c <= '\x0217') || (c >= '\x0250' && c <= '\x02A8') || (c >= '\x02BB' && c <= '\x02C1') || (c == '\x0386') ||
        (c >= '\x0388' && c <= '\x038A') || (c == '\x038C') || (c >= '\x038E' && c <= '\x03A1') || (c >= '\x03A3' && c <= '\x03CE') ||
        (c >= '\x03D0' && c <= '\x03D6') || (c == '\x03DA') || (c == '\x03DC') || (c == '\x03DE') || (c == '\x03E0') || (c >= '\x03E2' && c <= '\x03F3') ||
        (c >= '\x0401' && c <= '\x040C') || (c >= '\x040E' && c <= '\x044F') || (c >= '\x0451' && c <= '\x045C') || (c >= '\x045E' && c <= '\x0481') ||
        (c >= '\x0490' && c <= '\x04C4') || (c >= '\x04C7' && c <= '\x04C8') || (c >= '\x04CB' && c <= '\x04CC') || (c >= '\x04D0' && c <= '\x04EB') ||
        (c >= '\x04EE' && c <= '\x04F5') || (c >= '\x04F8' && c <= '\x04F9') || (c >= '\x0531' && c <= '\x0556') || (c == '\x0559') ||
        (c >= '\x0561' && c <= '\x0586') || (c >= '\x05D0' && c <= '\x05EA') || (c >= '\x05F0' && c <= '\x05F2') || (c >= '\x0621' && c <= '\x063A') ||
        (c >= '\x0641' && c <= '\x064A') || (c >= '\x0671' && c <= '\x06B7') || (c >= '\x06BA' && c <= '\x06BE') || (c >= '\x06C0' && c <= '\x06CE') ||
        (c >= '\x06D0' && c <= '\x06D3') || (c == '\x06D5') || (c >= '\x06E5' && c <= '\x06E6') || (c >= '\x0905' && c <= '\x0939') || (c == '\x093D') ||
        (c >= '\x0958' && c <= '\x0961') || (c >= '\x0985' && c <= '\x098C') || (c >= '\x098F' && c <= '\x0990') || (c >= '\x0993' && c <= '\x09A8') ||
        (c >= '\x09AA' && c <= '\x09B0') || (c == '\x09B2') || (c >= '\x09B6' && c <= '\x09B9') || (c >= '\x09DC' && c <= '\x09DD') ||
        (c >= '\x09DF' && c <= '\x09E1') || (c >= '\x09F0' && c <= '\x09F1') || (c >= '\x0A05' && c <= '\x0A0A') || (c >= '\x0A0F' && c <= '\x0A10') ||
        (c >= '\x0A13' && c <= '\x0A28') || (c >= '\x0A2A' && c <= '\x0A30') || (c >= '\x0A32' && c <= '\x0A33') || (c >= '\x0A35' && c <= '\x0A36') ||
        (c >= '\x0A38' && c <= '\x0A39') || (c >= '\x0A59' && c <= '\x0A5C') || (c == '\x0A5E') || (c >= '\x0A72' && c <= '\x0A74') ||
        (c >= '\x0A85' && c <= '\x0A8B') || (c == '\x0A8D') || (c >= '\x0A8F' && c <= '\x0A91') || (c >= '\x0A93' && c <= '\x0AA8') ||
        (c >= '\x0AAA' && c <= '\x0AB0') || (c >= '\x0AB2' && c <= '\x0AB3') || (c >= '\x0AB5' && c <= '\x0AB9') || (c == '\x0ABD') || (c == '\x0AE0') ||
        (c >= '\x0B05' && c <= '\x0B0C') || (c >= '\x0B0F' && c <= '\x0B10') || (c >= '\x0B13' && c <= '\x0B28') || (c >= '\x0B2A' && c <= '\x0B30') ||
        (c >= '\x0B32' && c <= '\x0B33') || (c >= '\x0B36' && c <= '\x0B39') || (c == '\x0B3D') || (c >= '\x0B5C' && c <= '\x0B5D') ||
        (c >= '\x0B5F' && c <= '\x0B61') || (c >= '\x0B85' && c <= '\x0B8A') || (c >= '\x0B8E' && c <= '\x0B90') || (c >= '\x0B92' && c <= '\x0B95') ||
        (c >= '\x0B99' && c <= '\x0B9A') || (c == '\x0B9C') || (c >= '\x0B9E' && c <= '\x0B9F') || (c >= '\x0BA3' && c <= '\x0BA4') ||
        (c >= '\x0BA8' && c <= '\x0BAA') || (c >= '\x0BAE' && c <= '\x0BB5') || (c >= '\x0BB7' && c <= '\x0BB9') || (c >= '\x0C05' && c <= '\x0C0C') ||
        (c >= '\x0C0E' && c <= '\x0C10') || (c >= '\x0C12' && c <= '\x0C28') || (c >= '\x0C2A' && c <= '\x0C33') || (c >= '\x0C35' && c <= '\x0C39') ||
        (c >= '\x0C60' && c <= '\x0C61') || (c >= '\x0C85' && c <= '\x0C8C') || (c >= '\x0C8E' && c <= '\x0C90') || (c >= '\x0C92' && c <= '\x0CA8') ||
        (c >= '\x0CAA' && c <= '\x0CB3') || (c >= '\x0CB5' && c <= '\x0CB9') || (c == '\x0CDE') || (c >= '\x0CE0' && c <= '\x0CE1') ||
        (c >= '\x0D05' && c <= '\x0D0C') || (c >= '\x0D0E' && c <= '\x0D10') || (c >= '\x0D12' && c <= '\x0D28') || (c >= '\x0D2A' && c <= '\x0D39') ||
        (c >= '\x0D60' && c <= '\x0D61') || (c >= '\x0E01' && c <= '\x0E2E') || (c == '\x0E30') || (c >= '\x0E32' && c <= '\x0E33') ||
        (c >= '\x0E40' && c <= '\x0E45') || (c >= '\x0E81' && c <= '\x0E82') || (c == '\x0E84') || (c >= '\x0E87' && c <= '\x0E88') || (c == '\x0E8A') ||
        (c == '\x0E8D') || (c >= '\x0E94' && c <= '\x0E97') || (c >= '\x0E99' && c <= '\x0E9F') || (c >= '\x0EA1' && c <= '\x0EA3') || (c == '\x0EA5') ||
        (c == '\x0EA7') || (c >= '\x0EAA' && c <= '\x0EAB') || (c >= '\x0EAD' && c <= '\x0EAE') || (c == '\x0EB0') || (c >= '\x0EB2' && c <= '\x0EB3') ||
        (c == '\x0EBD') || (c >= '\x0EC0' && c <= '\x0EC4') || (c >= '\x0F40' && c <= '\x0F47') || (c >= '\x0F49' && c <= '\x0F69') ||
        (c >= '\x10A0' && c <= '\x10C5') || (c >= '\x10D0' && c <= '\x10F6') || (c == '\x1100') || (c >= '\x1102' && c <= '\x1103') ||
        (c >= '\x1105' && c <= '\x1107') || (c == '\x1109') || (c >= '\x110B' && c <= '\x110C') || (c >= '\x110E' && c <= '\x1112') || (c == '\x113C') ||
        (c == '\x113E') || (c == '\x1140') || (c == '\x114C') || (c == '\x114E') || (c == '\x1150') || (c >= '\x1154' && c <= '\x1155') || (c == '\x1159') ||
        (c >= '\x115F' && c <= '\x1161') || (c == '\x1163') || (c == '\x1165') || (c == '\x1167') || (c == '\x1169') || (c >= '\x116D' && c <= '\x116E') ||
        (c >= '\x1172' && c <= '\x1173') || (c == '\x1175') || (c == '\x119E') || (c == '\x11A8') || (c == '\x11AB') || (c >= '\x11AE' && c <= '\x11AF') ||
        (c >= '\x11B7' && c <= '\x11B8') || (c == '\x11BA') || (c >= '\x11BC' && c <= '\x11C2') || (c == '\x11EB') || (c == '\x11F0') || (c == '\x11F9') ||
        (c >= '\x1E00' && c <= '\x1E9B') || (c >= '\x1EA0' && c <= '\x1EF9') || (c >= '\x1F00' && c <= '\x1F15') || (c >= '\x1F18' && c <= '\x1F1D') ||
        (c >= '\x1F20' && c <= '\x1F45') || (c >= '\x1F48' && c <= '\x1F4D') || (c >= '\x1F50' && c <= '\x1F57') || (c == '\x1F59') || (c == '\x1F5B') ||
        (c == '\x1F5D') || (c >= '\x1F5F' && c <= '\x1F7D') || (c >= '\x1F80' && c <= '\x1FB4') || (c >= '\x1FB6' && c <= '\x1FBC') || (c == '\x1FBE') ||
        (c >= '\x1FC2' && c <= '\x1FC4') || (c >= '\x1FC6' && c <= '\x1FCC') || (c >= '\x1FD0' && c <= '\x1FD3') || (c >= '\x1FD6' && c <= '\x1FDB') ||
        (c >= '\x1FE0' && c <= '\x1FEC') || (c >= '\x1FF2' && c <= '\x1FF4') || (c >= '\x1FF6' && c <= '\x1FFC') || (c == '\x2126') ||
        (c >= '\x212A' && c <= '\x212B') || (c == '\x212E') || (c >= '\x2180' && c <= '\x2182') || (c >= '\x3041' && c <= '\x3094') ||
        (c >= '\x30A1' && c <= '\x30FA') || (c >= '\x3105' && c <= '\x312C') || (c >= '\xAC00' && c <= '\xD7A3');         
    }

    public static bool isXmlLetter(char c) {
      return ((isXmlBaseChar(c)) || (isXmlIdeographic(c)));
    }

    public static bool isXmlPubidChar(char c) {
      return  (c == '\x20') || (c == '\xD') || (c == '\xA') || (c >= 'a' && c <= 'z') ||
        (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c == '-') || (c == '\'') ||
        (c == '(') || (c == ')') || (c == '+') || (c == ',') || (c == '.') || (c == ':') ||
        (c == '=') || (c == '?') || (c == ';') || (c == '!') || (c == '*') || (c == '#') ||
        (c == '@') || (c == '$') || (c == '_') || (c == '%'); 
    }

    public static bool isXmlWhitespace(char c) {
      return  (c == '\x20') || (c == '\x9') || (c == '\xD') || (c == '\xA');
    }

    public static bool isXmlChar(char c) {
      return  (c == '\x9') || (c == '\xD') || (c == '\xA') || (c >= '\x20' && c <= '\xD7FF') ||
        (c >= '\xE000' && c >= '\xFFFD');
      // Beyond C# character range : (c >= '\x10000' && c <= '\x10FFFF');
    }

  }
}