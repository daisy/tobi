using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class StreamInfo
    {
        public string Id { get; private set;}
        public StreamReader Reader { get; private set;}
        public int LineNumber { get; set;}
        public int Column {get; set;}

        public StreamInfo(string id, StreamReader reader)
        {
            LineNumber = 1;
            Column = 1;
            Id = id;
            Reader = reader;
        }
    }

    public class Scanner
    {
	    public static TokenType LTQUES = new TokenType(0, "LTQUES");
	    public static TokenType IDENTIFIER = new TokenType(1, "IDENTIFIER");
	    public static TokenType EQUAL = new TokenType(2, "EQUAL");
	    public static TokenType LPAREN = new TokenType(3, "LPAREN");
	    public static TokenType RPAREN = new TokenType(4, "RPAREN");
	    public static TokenType COMMA = new TokenType(5, "COMMA");
	    public static TokenType STRING = new TokenType(6, "STRING");
	    public static TokenType QUESGT = new TokenType(7, "QUESGT");
	    public static TokenType LTBANG = new TokenType(8, "LTBANG");
	    public static TokenType GT = new TokenType(9, "GT");
	    public static TokenType PIPE = new TokenType(10, "PIPE");
	    public static TokenType QUES = new TokenType(11, "QUES");
	    public static TokenType PLUS = new TokenType(12, "PLUS");
	    public static TokenType ASTERISK = new TokenType(13, "ASTERISK");
	    public static TokenType LT = new TokenType(14, "LT");
	    public static TokenType EOF = new TokenType(15, "EOF");
	    public static TokenType COMMENT = new TokenType(16, "COMMENT");
	    public static TokenType PERCENT = new TokenType(17, "PERCENT");
	    public static TokenType CONDITIONAL = new TokenType(18, "CONDITIONAL");
	    public static TokenType ENDCONDITIONAL = new TokenType(19, "ENDCONDITIONAL");
        public static TokenType NMTOKEN = new TokenType(20, "NMTOKEN");

        protected StreamInfo StreamInfo;
        protected Stack InputStreams;
	    protected Token NextToken;
	    protected int NextChar;
        protected bool AtEof;
        protected bool Trace;
        protected char[] ExpandBuffer;
        protected int ExpandPos;
        protected Hashtable EntityExpansion;
        protected IEntityExpansion Expander;

	    public Scanner(StreamReader inReader, IEntityExpansion anExpander)
	    {
            StreamInfo = new StreamInfo("", inReader);
            AtEof = false;
            Trace = false;
            ExpandBuffer = null;
            EntityExpansion = new Hashtable();
            Expander = anExpander;
	    }

	    public Scanner(StreamReader inReader, bool doTrace, IEntityExpansion anExpander)
        {
            StreamInfo = new StreamInfo("", inReader);
            AtEof = false;
            Trace = doTrace;
            ExpandBuffer = null;
            EntityExpansion = new Hashtable();
            Expander = anExpander;
        }

	    public Token Peek()
	    {
		    if (NextToken == null)
		    {
			    NextToken = ReadNextToken();
		    }

		    return NextToken;
	    }

	    public Token Get()	
	    {
		    if (NextToken == null)
		    {
			    NextToken = ReadNextToken();
		    }

		    Token retval = NextToken;
		    NextToken = null;

		    return retval;
	    }

        protected int ReadNextChar()
        {
            int ch = StreamInfo.Reader.Read();

            if (ch < 0)
            {
                if ((InputStreams != null) && (InputStreams.Count > 0))
                {
                    StreamInfo.Reader.Close();
                    StreamInfo = (StreamInfo)InputStreams.Pop();
                    return ReadNextChar();
                }
            }
            return ch;
        }

	    protected int PeekChar()
	    {
            if (ExpandBuffer != null)
            {
                return (int) ExpandBuffer[ExpandPos];
            }

		    if (NextChar == 0)
		    {
			    NextChar = ReadNextChar();
                StreamInfo.Column++;
                if (NextChar == '\n' || NextChar == '\r')
                {
                    StreamInfo.LineNumber++;
                    StreamInfo.Column=1;
                }
		    }

		    return NextChar;
	    }

	    protected int Read()
	    {
            if (ExpandBuffer != null)
            {
                int expNextChar = ExpandBuffer[ExpandPos++];
                if (ExpandPos >= ExpandBuffer.Length)
                {
                    ExpandPos = -1;
                    ExpandBuffer = null;
                }
                if (Trace)
                {   
                    System.Diagnostics.Trace.Write((char) expNextChar);
                }
                return expNextChar;
            }
		    if (NextChar == 0)
		    {
			    PeekChar();
		    }

		    int retval = NextChar;
		    NextChar = 0;

            if (Trace)
            {
                System.Diagnostics.Trace.Write((char) retval);
            }
		    return retval;
	    }

        public string GetUntil(char stopChar)
        {
            string buff = "";

            int ch;

            while ((ch = Read()) >= 0)
            {
                if (ch == stopChar)
                {
                    return buff;
                }
                buff += (char) ch;
            }
            return buff;
        }

        public void SkipUntil(char stopChar)
        {
            int ch;

            while ((ch = Read()) >= 0)
            {
                if (ch == stopChar)
                {
                    return;
                }
            }
            return;
        }

	    protected Token ReadNextToken()
	    {
		    for (;;)
		    {
			    int ch = Read();
		        string chstr = "";
		        chstr += (char)ch;

			    if (ch == '<')
			    {
				    ch = PeekChar();
				    if (ch == '!')
				    {
					    Read();

                        if (PeekChar() == '[')
                        {
                            Read();

                            return new Token(CONDITIONAL);
                        }

					    if (PeekChar() != '-')
					    {
						    return new Token(LTBANG);
					    }
				        Read();
				        if (PeekChar() != '-')
				        {
				            throw new DTDParseException(GetUriId(),
				                                        "Invalid character sequence <!-"+Read(),
				                                        GetLineNumber(), GetColumn());
				        }
				        Read();

				        string buff = "";
				        for (;;)
				        {
				            if (PeekChar() < 0)
				            {
				                throw new DTDParseException(GetUriId(),
				                                            "Unterminated comment: <!--"+
				                                            buff,
				                                            GetLineNumber(), GetColumn());
				            }

				            if (PeekChar() != '-')
				            {
				                buff += (char) Read();
				            }
				            else
				            {
				                Read();
				                if (PeekChar() < 0)
				                {
				                    throw new DTDParseException(GetUriId(),
				                                                "Unterminated comment: <!--"+
				                                                buff,
				                                                GetLineNumber(), GetColumn());
				                }
				                if (PeekChar() == '-')
				                {
				                    Read();
				                    if (PeekChar() != '>')
				                    {
				                        throw new DTDParseException(GetUriId(),
				                                                    "Invalid character sequence --"+
				                                                    Read(), GetLineNumber(), GetColumn());
				                    }
				                    Read();
				                    return new Token(COMMENT, buff);
				                }
				                buff += '-';
				            }
				        }
				    }
			        if (ch == '?')
			        {
			            Read();
			            return new Token(LTQUES);
			        }
			        return new Token(LT);
			    }
		        if (ch == '?')
		        {
		            // Need to treat ?> as two separate tokens because
		            // <!ELEMENT blah (foo)?> needs the ? as a QUES, not QUESGT
		            /*				ch = peekChar();

				    if (ch == '>')
				    {
					    read();
					    return new Token(QUESGT);
				    }
				    else
				    {
					    return new Token(QUES);
				    }*/
		            return new Token(QUES);
		        }
		        if ((ch == '"') || (ch == '\''))
		        {
		            int quoteChar = ch;

		            string buff = "";
		            while (PeekChar() != quoteChar)
		            {
		                ch = Read();
		                if (ch == '\\')
		                {
		                    buff += (char) Read();
		                }
		                else if (ch < 0)
		                {
		                    break;  // IF EOF before getting end quote
		                }
		                else
		                {
		                    buff += (char) ch;
		                }
		            }
		            Read();
		            return new Token(STRING, buff);
		        }
		        if (ch == '(')
		        {
		            return new Token(LPAREN);
		        }
		        if (ch == ')')
		        {
		            return new Token(RPAREN);
		        }
		        if (ch == '|')
		        {
		            return new Token(PIPE);
		        }
		        if (ch == '>')
		        {
		            return new Token(GT);
		        }
		        if (ch == '=')
		        {
		            return new Token(EQUAL);
		        }
		        if (ch == '*')
		        {
		            return new Token(ASTERISK);
		        }
		        if (ch == ']')
		        {
		            if (Read() != ']')
		            {
		                throw new DTDParseException(GetUriId(),
		                                            "Illegal character in input stream: "+ch,
		                                            GetLineNumber(), GetColumn());
		            }
		            if (Read() != '>')
		            {
		                throw new DTDParseException(GetUriId(),
		                                            "Illegal character in input stream: "+ch,
		                                            GetLineNumber(), GetColumn());
		            }

		            return new Token(ENDCONDITIONAL);
		        }
		        if (ch == '#')
		        {
		            string buff = "";
		            buff += (char) ch;

		            if (IsIdentifierChar((char) PeekChar()))
		            {
		                buff += (char) Read();

		                while (IsNameChar((char) PeekChar()))
		                {
		                    buff += (char) Read();
		                }
		            }
		            return new Token(IDENTIFIER, buff);
		        }
		        if ((ch == '&') || (ch == '%'))
		        {
		            string peekstr = "";
		            peekstr += (char) PeekChar();
		            if ((ch == '%') && string.IsNullOrEmpty(peekstr.Trim()))
		            {
		                return new Token(PERCENT);
		            }

		            bool peRef = (ch == '%');

		            string buff = "";
		            buff += (char) ch;

		            if (IsIdentifierChar((char) PeekChar()))
		            {
		                buff += ((char) Read());
		                while (IsNameChar((char) PeekChar()))
		                {
		                    buff +=((char) Read());
		                }
		            }

		            if (Read() != ';')
		            {
		                throw new DTDParseException(GetUriId(),
		                                            "Expected ';' after reference "+
		                                            buff +", found '" + (char)ch + "'",
		                                            GetLineNumber(), GetColumn());
		            }
		            buff+= (';');

		            if (peRef)
		            {
		                if (ExpandEntity(buff))
		                {
		                    continue;
		                }
		                // MAW: Added version 1.17
		                // If the entity can't be expanded, don't return it, skip it
		                continue;
		            }
		            return new Token(IDENTIFIER, buff);
		        }
		        if (ch == '+')
		        {
		            return new Token(PLUS);
		        }
		        if (ch == ',')
		        {
		            return new Token(COMMA);
		        }
		        if (IsIdentifierChar((char) ch))
		        {
		            string buff = "";
		            buff += (char) ch;

		            while (IsNameChar((char) PeekChar()))
		            {
		                buff += ((char) Read());
		            }
		            return new Token(IDENTIFIER, buff);
		        }
		        if (IsNameChar((char) ch))
		        {
		            string buff = "";
		            buff += (char) ch;

		            while (IsNameChar((char) PeekChar()))
		            {
		                buff += ((char) Read());
		            }
		            return new Token(NMTOKEN, buff);
		        }
		        if (ch < 0)
		        {
		            if (AtEof)
		            {
		                throw new IOException("Read past EOF");
		            }
		            AtEof = true;
		            return new Token(EOF);
		        }
		        if (string.IsNullOrEmpty(chstr.Trim()))
		        {
		            continue;
		        }
		        throw new DTDParseException(GetUriId(),
		                                    "Illegal character in input stream: "+ch,
		                                    GetLineNumber(), GetColumn());
		    }
	    }

        public void SkipConditional()   
        {
            // 070401 MAW: Fix for nested conditionals provided by Noah Fike
            // BEGIN CHANGE
            int ch = 0;
            int nestingDepth = 0; // Add nestingDepth parameter

            //    Everything is ignored within an ignored section, except the
            //    sub-section delimiters '<![' and ']]>'. These must be balanced,
            //    but no section keyword is required:
            //    Conditional Section
            //[61] conditionalSect ::=  includeSect | ignoreSect
            //[62] includeSect ::=  '<![' S? 'INCLUDE' S? '[' extSubsetDecl ']]>'
            //[63] ignoreSect ::=  '<![' S? 'IGNORE' S? '[' ignoreSectContents* ']]>'
            //[64] ignoreSectContents ::=  Ignore ('<![' ignoreSectContents ']]>' Ignore)*
            //[65] Ignore ::=  Char* - (Char* ('<![' | ']]>') Char*)

            for (;;)
            {
                if ( ch != ']' )
                {
                    ch = Read();
                }
                if (ch == ']')
                {
                    ch = Read();
                    if (ch == ']')
                    {
                        ch = Read();
                        if (ch == '>')
                        {
                            if ( nestingDepth == 0)
                            {
                                // The end of the IGNORE conditional section
                                // has been found.  Break out of for loop.
                                break;
                            }
                            // We are within an ignoreSectContents section.  Decrement
                            // the nesting depth to represent that this section has
                            // been ended.
                            nestingDepth--;
                        }
                    }
                }
                // See if this is the first character of the beginning of a new section.
                if (ch == '<')
                {
                    ch = Read();
                    if ( ch == '!' )
                    {
                        ch = Read();
                        if ( ch == '[' )
                        {
                            // The beginning of a new ignoreSectContents section
                            // has been found.  Increment nesting depth.
                            nestingDepth++;
                        }
                    }
                }
            }
            // END CHANGE
        }

        public string GetUriId() { return(StreamInfo.Id); }
        public int GetLineNumber() { return StreamInfo.LineNumber; }
        public int GetColumn() { return StreamInfo.Column; }

	    public bool IsIdentifierChar(char ch)
	    {
		    if (IsLetter(ch) ||
			    (ch == '_') || (ch == ':'))
		    {
			    return true;
		    }
		    return false;
	    }

	    public bool IsNameChar(char ch)
	    {
		    if (IsLetter(ch) || IsDigit(ch) ||
			    (ch == '-') || (ch == '_') || (ch == '.') || (ch == ':')
			    || IsCombiningChar(ch) || IsExtender(ch))
		    {
			    return true;
		    }
		    return false;
	    }

        public bool IsLetter(char ch)
        {
            return IsBaseChar(ch) || IsIdeographic(ch);
        }

        public bool IsBaseChar(char ch)
        {
            for (int i=0; i < LetterRanges.GetLength(0); i++)
            {
                if (ch < (char)LetterRanges[i,0]) return false;
                if ((ch >= (char)LetterRanges[i,0]) &&
                    (ch <= LetterRanges[i,1])) return true;
            }
            return false;
        }

        public bool IsIdeographic(char ch)
        {
            if (ch < 0x4e00) return false;
            if ((ch >= 0x4e00) && (ch <= 0x9fa5)) return true;
            if (ch == 0x3007) return true;
            if ((ch >= 0x3021) && (ch <= 0x3029)) return true;
            return false;
        }

        public bool IsDigit(char ch)
        {
            if ((ch >= 0x0030) && (ch <= 0x0039)) return true;
            if (ch < 0x0660) return false;
            if ((ch >= 0x0660) && (ch <= 0x0669)) return true;
            if (ch < 0x06f0) return false;
            if ((ch >= 0x06f0) && (ch <= 0x06f9)) return true;
            if (ch < 0x0966) return false;
            if ((ch >= 0x0966) && (ch <= 0x096f)) return true;
            if (ch < 0x09e6) return false;
            if ((ch >= 0x09e6) && (ch <= 0x09ef)) return true;
            if (ch < 0x0a66) return false;
            if ((ch >= 0x0a66) && (ch <= 0x0a6f)) return true;
            if (ch < 0x0ae6) return false;
            if ((ch >= 0x0ae6) && (ch <= 0x0aef)) return true;
            if (ch < 0x0b66) return false;
            if ((ch >= 0x0b66) && (ch <= 0x0b6f)) return true;
            if (ch < 0x0be7) return false;
            if ((ch >= 0x0be7) && (ch <= 0x0bef)) return true;
            if (ch < 0x0c66) return false;
            if ((ch >= 0x0c66) && (ch <= 0x0c6f)) return true;
            if (ch < 0x0ce6) return false;
            if ((ch >= 0x0ce6) && (ch <= 0x0cef)) return true;
            if (ch < 0x0d66) return false;
            if ((ch >= 0x0d66) && (ch <= 0x0d6f)) return true;
            if (ch < 0x0e50) return false;
            if ((ch >= 0x0e50) && (ch <= 0x0e59)) return true;
            if (ch < 0x0ed0) return false;
            if ((ch >= 0x0ed0) && (ch <= 0x0ed9)) return true;
            if (ch < 0x0f20) return false;
            if ((ch >= 0x0f20) && (ch <= 0x0f29)) return true;
            return false;
        }

	    public bool IsCombiningChar(char ch)
	    {
		    if (ch < 0x0300) return false;
		    if ((ch >= 0x0300) && (ch <= 0x0345)) return true;
		    if ((ch >= 0x0360) && (ch <= 0x0361)) return true;
		    if ((ch >= 0x0483) && (ch <= 0x0486)) return true;
		    if ((ch >= 0x0591) && (ch <= 0x05a1)) return true;
		    if ((ch >= 0x05a3) && (ch <= 0x05b9)) return true;
		    if ((ch >= 0x05bb) && (ch <= 0x05bd)) return true;
		    if (ch == 0x05bf) return true;
		    if ((ch >= 0x05c1) && (ch <= 0x05c2)) return true;
		    if (ch == 0x05c4) return true;
		    if ((ch >= 0x064b) && (ch <= 0x0652)) return true;
		    if (ch == 0x0670) return true;
		    if ((ch >= 0x06d6) && (ch <= 0x06dc)) return true;
		    if ((ch >= 0x06dd) && (ch <= 0x06df)) return true;
		    if ((ch >= 0x06e0) && (ch <= 0x06e4)) return true;
		    if ((ch >= 0x06e7) && (ch <= 0x06e8)) return true;
		    if ((ch >= 0x06ea) && (ch <= 0x06ed)) return true;
		    if ((ch >= 0x0901) && (ch <= 0x0903)) return true;
		    if (ch == 0x093c) return true;
		    if ((ch >= 0x093e) && (ch <= 0x094c)) return true;
		    if (ch == 0x094d) return true;
		    if ((ch >= 0x0951) && (ch <= 0x0954)) return true;
		    if ((ch >= 0x0962) && (ch <= 0x0963)) return true;
		    if ((ch >= 0x0981) && (ch <= 0x0983)) return true;
		    if (ch == 0x09bc) return true;
		    if (ch == 0x09be) return true;
		    if (ch == 0x09bf) return true;
		    if ((ch >= 0x09c0) && (ch <= 0x09c4)) return true;
		    if ((ch >= 0x09c7) && (ch <= 0x09c8)) return true;
		    if ((ch >= 0x09cb) && (ch <= 0x09cd)) return true;
		    if (ch == 0x09d7) return true;
		    if ((ch >= 0x09e2) && (ch <= 0x09e3)) return true;
		    if (ch == 0x0a02) return true;
		    if (ch == 0x0a3c) return true;
		    if (ch == 0x0a3e) return true;
		    if (ch == 0x0a3f) return true;
		    if ((ch >= 0x0a40) && (ch <= 0x0a42)) return true;
		    if ((ch >= 0x0a47) && (ch <= 0x0a48)) return true;
		    if ((ch >= 0x0a4b) && (ch <= 0x0a4d)) return true;
		    if ((ch >= 0x0a70) && (ch <= 0x0a71)) return true;
		    if ((ch >= 0x0a81) && (ch <= 0x0a83)) return true;
		    if (ch == 0x0abc) return true;
		    if ((ch >= 0x0abe) && (ch <= 0x0ac5)) return true;
		    if ((ch >= 0x0ac7) && (ch <= 0x0ac9)) return true;
		    if ((ch >= 0x0acb) && (ch <= 0x0acd)) return true;
		    if ((ch >= 0x0b01) && (ch <= 0x0b03)) return true;
		    if (ch == 0x0b3c) return true;
		    if ((ch >= 0x0b3e) && (ch <= 0x0b43)) return true;
		    if ((ch >= 0x0b47) && (ch <= 0x0b48)) return true;
		    if ((ch >= 0x0b4b) && (ch <= 0x0b4d)) return true;
		    if ((ch >= 0x0b56) && (ch <= 0x0b57)) return true;
		    if ((ch >= 0x0b82) && (ch <= 0x0b83)) return true;
		    if ((ch >= 0x0bbe) && (ch <= 0x0bc2)) return true;
		    if ((ch >= 0x0bc6) && (ch <= 0x0bc8)) return true;
		    if ((ch >= 0x0bca) && (ch <= 0x0bcd)) return true;
		    if (ch == 0x0bd7) return true;
		    if ((ch >= 0x0c01) && (ch <= 0x0c03)) return true;
		    if ((ch >= 0x0c3e) && (ch <= 0x0c44)) return true;
		    if ((ch >= 0x0c46) && (ch <= 0x0c48)) return true;
		    if ((ch >= 0x0c4a) && (ch <= 0x0c4d)) return true;
		    if ((ch >= 0x0c55) && (ch <= 0x0c56)) return true;
		    if ((ch >= 0x0c82) && (ch <= 0x0c83)) return true;
		    if ((ch >= 0x0cbe) && (ch <= 0x0cc4)) return true;
		    if ((ch >= 0x0cc6) && (ch <= 0x0cc8)) return true;
		    if ((ch >= 0x0cca) && (ch <= 0x0ccd)) return true;
		    if ((ch >= 0x0cd5) && (ch <= 0x0cd6)) return true;
		    if ((ch >= 0x0d02) && (ch <= 0x0d03)) return true;
		    if ((ch >= 0x0d3e) && (ch <= 0x0d43)) return true;
		    if ((ch >= 0x0d46) && (ch <= 0x0d48)) return true;
		    if ((ch >= 0x0d4a) && (ch <= 0x0d4d)) return true;
		    if (ch == 0x0d57) return true;
		    if (ch == 0x0e31) return true;
		    if ((ch >= 0x0e34) && (ch <= 0x0e3a)) return true;
		    if ((ch >= 0x0e47) && (ch <= 0x0e4e)) return true;
		    if (ch == 0x0eb1) return true;
		    if ((ch >= 0x0eb4) && (ch <= 0x0eb9)) return true;
		    if ((ch >= 0x0ebb) && (ch <= 0x0ebc)) return true;
		    if ((ch >= 0x0ec8) && (ch <= 0x0ecd)) return true;
		    if ((ch >= 0x0f18) && (ch <= 0x0f19)) return true;
		    if (ch == 0x0f35) return true;
		    if (ch == 0x0f37) return true;
		    if (ch == 0x0f39) return true;
		    if (ch == 0x0f3e) return true;
		    if (ch == 0x0f3f) return true;
		    if ((ch >= 0x0f71) && (ch <= 0x0f84)) return true;
		    if ((ch >= 0x0f86) && (ch <= 0x0f8b)) return true;
		    if ((ch >= 0x0f90) && (ch <= 0x0f95)) return true;
		    if (ch == 0x0f97) return true;
		    if ((ch >= 0x0f99) && (ch <= 0x0fad)) return true;
		    if ((ch >= 0x0fb1) && (ch <= 0x0fb7)) return true;
		    if (ch == 0x0fb9) return true;
		    if ((ch >= 0x20d0) && (ch <= 0x20dc)) return true;
		    if (ch == 0x20e1) return true;
		    if ((ch >= 0x302a) && (ch <= 0x302f)) return true;
		    if (ch == 0x3099) return true;
		    if (ch == 0x309a) return true;

		    return false;
	    }

	    public bool IsExtender(char ch)
	    {
		    if (ch < 0x00b7) return false;

		    if ((ch == 0x00b7) || (ch == 0x02d0) || (ch == 0x02d1) ||
			    (ch == 0x0387) || (ch == 0x0640) || (ch == 0x0e46) ||
			    ((ch >= 0x3031) && (ch <= 0x3035)) ||
			    ((ch >= 0x309d) && (ch <= 0x309e)) ||
			    ((ch >= 0x30fc) && (ch <= 0x30fe))) return true;

		    return false;
	    }

        public bool ExpandEntity(string entityName)   
        {
            string entity = (string) EntityExpansion[entityName];
            if (entity != null)
            {
                Expand(entity.ToCharArray());
                return true;
            }

            entityName = entityName.Substring(1, entityName.Length-1);

            //System.writer.WriteLine("Trying to expand: "+entityName);
            DTDEntity realEntity = Expander.ExpandEntity(entityName);
            if (realEntity != null)
            {
                //System.writer.WriteLine("Expanded: "+entityName);
                StreamReader entityIn = realEntity.GetReader();
                if (entityIn != null)
                {
                    if (InputStreams == null)
                    {
                        InputStreams = new Stack();
                    }

                    InputStreams.Push(StreamInfo);
                    StreamInfo = new StreamInfo(realEntity.GetExternalId(), entityIn);

                    return true;
                }
            }

            return false;
        }

        public void Expand(char[] expandChars)
        {
            if (ExpandBuffer != null)
            {
                int oldCharsLeft = ExpandBuffer.Length - ExpandPos;

                char[] newExp = new char[oldCharsLeft + expandChars.Length];
                Array.Copy(expandChars, newExp, expandChars.Length);
                Array.Copy(ExpandBuffer, ExpandPos, newExp, expandChars.Length, oldCharsLeft);
                ExpandPos = 0;
                ExpandBuffer = newExp;
                if (ExpandBuffer.Length == 0)
                {
                    ExpandBuffer = null;
                    ExpandPos = -1;
                }
            }
            else
            {
                ExpandBuffer = expandChars;
                ExpandPos = 0;
                if (ExpandBuffer.Length == 0)
                {
                    ExpandBuffer = null;
                    ExpandPos = -1;
                }
            }
        }

        public void AddEntity(string entityName, string entityValue)
        {
            string name = string.Format("%{0};", entityName);
            EntityExpansion[name] = entityValue;
        }

        public static int[,] LetterRanges = {
                                                {0x0041, 0x005A}, {0x0061, 0x007A}, {0x00C0, 0x00D6},
                                                {0x00D8, 0x00F6}, {0x00F8, 0x00FF}, {0x0100, 0x0131},
                                                {0x0134, 0x013E}, {0x0141, 0x0148}, {0x014A, 0x017E},
                                                {0x0180, 0x01C3}, {0x01CD, 0x01F0}, {0x01F4, 0x01F5},
                                                {0x01FA, 0x0217}, {0x0250, 0x02A8}, {0x02BB, 0x02C1},
                                                {0x0386, 0x0386}, {0x0388, 0x038A}, {0x038C, 0x038C},
                                                {0x038E, 0x03A1}, {0x03A3, 0x03CE}, {0x03D0, 0x03D6},
                                                {0x03DA, 0x03DA}, {0x03DC, 0x03DC}, {0x03DE, 0x03DE},
                                                {0x03E0, 0x03E0}, {0x03E2, 0x03F3}, {0x0401, 0x040C},
                                                {0x040E, 0x044F}, {0x0451, 0x045C}, {0x045E, 0x0481},
                                                {0x0490, 0x04C4}, {0x04C7, 0x04C8}, {0x04CB, 0x04CC},
                                                {0x04D0, 0x04EB}, {0x04EE, 0x04F5}, {0x04F8, 0x04F9},
                                                {0x0531, 0x0556}, {0x0559, 0x0559}, {0x0561, 0x0586},
                                                {0x05D0, 0x05EA}, {0x05F0, 0x05F2}, {0x0621, 0x063A},
                                                {0x0641, 0x064A}, {0x0671, 0x06B7}, {0x06BA, 0x06BE},
                                                {0x06C0, 0x06CE}, {0x06D0, 0x06D3}, {0x06D5, 0x06D5},
                                                {0x06E5, 0x06E6}, {0x0905, 0x0939}, {0x093D, 0x093D},
                                                {0x0958, 0x0961}, {0x0985, 0x098C}, {0x098F, 0x0990},
                                                {0x0993, 0x09A8}, {0x09AA, 0x09B0}, {0x09B2, 0x09B2},
                                                {0x09B6, 0x09B9}, {0x09DC, 0x09DD}, {0x09DF, 0x09E1},
                                                {0x09F0, 0x09F1}, {0x0A05, 0x0A0A}, {0x0A0F, 0x0A10},
                                                {0x0A13, 0x0A28}, {0x0A2A, 0x0A30}, {0x0A32, 0x0A33},
                                                {0x0A35, 0x0A36}, {0x0A38, 0x0A39}, {0x0A59, 0x0A5C},
                                                {0x0A5E, 0x0A5E}, {0x0A72, 0x0A74}, {0x0A85, 0x0A8B},
                                                {0x0A8D, 0x0A8D}, {0x0A8F, 0x0A91}, {0x0A93, 0x0AA8},
                                                {0x0AAA, 0x0AB0}, {0x0AB2, 0x0AB3}, {0x0AB5, 0x0AB9},
                                                {0x0ABD, 0x0ABD}, {0x0AE0, 0x0AE0}, {0x0B05, 0x0B0C},
                                                {0x0B0F, 0x0B10}, {0x0B13, 0x0B28}, {0x0B2A, 0x0B30},
                                                {0x0B32, 0x0B33}, {0x0B36, 0x0B39}, {0x0B3D, 0x0B3D},
                                                {0x0B5C, 0x0B5D}, {0x0B5F, 0x0B61}, {0x0B85, 0x0B8A},
                                                {0x0B8E, 0x0B90}, {0x0B92, 0x0B95}, {0x0B99, 0x0B9A},
                                                {0x0B9C, 0x0B9C}, {0x0B9E, 0x0B9F}, {0x0BA3, 0x0BA4},
                                                {0x0BA8, 0x0BAA}, {0x0BAE, 0x0BB5}, {0x0BB7, 0x0BB9},
                                                {0x0C05, 0x0C0C}, {0x0C0E, 0x0C10}, {0x0C12, 0x0C28},
                                                {0x0C2A, 0x0C33}, {0x0C35, 0x0C39}, {0x0C60, 0x0C61},
                                                {0x0C85, 0x0C8C}, {0x0C8E, 0x0C90}, {0x0C92, 0x0CA8},
                                                {0x0CAA, 0x0CB3}, {0x0CB5, 0x0CB9}, {0x0CDE, 0x0CDE},
                                                {0x0CE0, 0x0CE1}, {0x0D05, 0x0D0C}, {0x0D0E, 0x0D10},
                                                {0x0D12, 0x0D28}, {0x0D2A, 0x0D39}, {0x0D60, 0x0D61},
                                                {0x0E01, 0x0E2E}, {0x0E30, 0x0E30}, {0x0E32, 0x0E33},
                                                {0x0E40, 0x0E45}, {0x0E81, 0x0E82}, {0x0E84, 0x0E84},
                                                {0x0E87, 0x0E88}, {0x0E8A, 0x0E8A}, {0x0E8D, 0x0E8D},
                                                {0x0E94, 0x0E97}, {0x0E99, 0x0E9F}, {0x0EA1, 0x0EA3},
                                                {0x0EA5, 0x0EA5}, {0x0EA7, 0x0EA7}, {0x0EAA, 0x0EAB},
                                                {0x0EAD, 0x0EAE}, {0x0EB0, 0x0EB0}, {0x0EB2, 0x0EB3},
                                                {0x0EBD, 0x0EBD}, {0x0EC0, 0x0EC4}, {0x0F40, 0x0F47},
                                                {0x0F49, 0x0F69}, {0x10A0, 0x10C5}, {0x10D0, 0x10F6},
                                                {0x1100, 0x1100}, {0x1102, 0x1103}, {0x1105, 0x1107},
                                                {0x1109, 0x1109}, {0x110B, 0x110C}, {0x110E, 0x1112},
                                                {0x113C, 0x113C}, {0x113E, 0x113E}, {0x1140, 0x1140},
                                                {0x114C, 0x114C}, {0x114E, 0x114E}, {0x1150, 0x1150},
                                                {0x1154, 0x1155}, {0x1159, 0x1159}, {0x115F, 0x1161},
                                                {0x1163, 0x1163}, {0x1165, 0x1165}, {0x1167, 0x1167},
                                                {0x1169, 0x1169}, {0x116D, 0x116E}, {0x1172, 0x1173},
                                                {0x1175, 0x1175}, {0x119E, 0x119E}, {0x11A8, 0x11A8},
                                                {0x11AB, 0x11AB}, {0x11AE, 0x11AF}, {0x11B7, 0x11B8},
                                                {0x11BA, 0x11BA}, {0x11BC, 0x11C2}, {0x11EB, 0x11EB},
                                                {0x11F0, 0x11F0}, {0x11F9, 0x11F9}, {0x1E00, 0x1E9B},
                                                {0x1EA0, 0x1EF9}, {0x1F00, 0x1F15}, {0x1F18, 0x1F1D},
                                                {0x1F20, 0x1F45}, {0x1F48, 0x1F4D}, {0x1F50, 0x1F57},
                                                {0x1F59, 0x1F59}, {0x1F5B, 0x1F5B}, {0x1F5D, 0x1F5D},
                                                {0x1F5F, 0x1F7D}, {0x1F80, 0x1FB4}, {0x1FB6, 0x1FBC},
                                                {0x1FBE, 0x1FBE}, {0x1FC2, 0x1FC4}, {0x1FC6, 0x1FCC},
                                                {0x1FD0, 0x1FD3}, {0x1FD6, 0x1FDB}, {0x1FE0, 0x1FEC},
                                                {0x1FF2, 0x1FF4}, {0x1FF6, 0x1FFC}, {0x2126, 0x2126},
                                                {0x212A, 0x212B}, {0x212E, 0x212E}, {0x2180, 0x2182},
                                                {0x3041, 0x3094}, {0x30A1, 0x30FA}, {0x3105, 0x312C},
                                                {0xAC00, 0xD7A3}
                                            };
    }
}