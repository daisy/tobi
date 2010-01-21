using System;
using System.Collections;
using System.IO;
using System.Net;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{ 
    public class DTDParser : IEntityExpansion
    {
        protected Scanner Scanner;
        protected DTD Dtd;
        protected object DefaultLocation;

    /** Creates a parser that will read from the specified Reader object */
        public DTDParser(StreamReader reader)
        {
            Scanner = new Scanner(reader, false, this);
            Dtd = new DTD();
        }

    /** Creates a parser that will read from the specified Reader object
     * @param in The input stream to read
     * @param trace True if the parser should print out tokens as it reads them
     *  (used for debugging the parser)
     */
        public DTDParser(StreamReader reader, bool trace)
        {
            Scanner = new Scanner(reader, trace, this);
            Dtd = new DTD();
            
        }
 
        /** Creates a parser that will read from the specified filename */
        public DTDParser(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new DTDParseException(string.Format("File not found: {0}", filename));
            }
            DefaultLocation = Directory.GetParent(filename);
            Scanner = new Scanner(new StreamReader(filename), false, this);
            Dtd = new DTD();
        }

    /** Creates a parser that will read from the specified filename
     * @param filename The file to read
     * @param trace True if the parser should print out tokens as it reads them
     *  (used for debugging the parser)
     */
        public DTDParser(string filename, bool trace)
        {
            if (!File.Exists(filename))
            {
                throw new DTDParseException(string.Format("File not found: {0}", filename));
            }
            DefaultLocation = Directory.GetParent(filename);
            Scanner = new Scanner(new StreamReader(filename), trace, this);
            Dtd = new DTD();
        }
    
    /** Creates a parser that will read from the specified URL object */
        public DTDParser(Uri uri)
        {
            //LAM: we need to set the defaultLocation to the directory where
            //the dtd is found so that we don't run into problems parsing any
            //relative external files referenced by the dtd.
            string file = uri.ToString();
            DefaultLocation = file.Substring(0, file.LastIndexOf('/') + 1);
            
            WebRequest req = WebRequest.Create(uri);
            WebResponse resp = req.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            
            Scanner = new Scanner(sr, false, this);
            Dtd = new DTD();
        }
    
    /** Creates a parser that will read from the specified URL object
     * @param uri The URL to read
     * @param trace True if the parser should print out tokens as it reads them
     *  (used for debugging the parser)
     */
        public DTDParser(Uri uri, bool trace)
        {
        //LAM: we need to set the defaultLocation to the directory where
        //the dtd is found so that we don't run into problems parsing any
        //relative external files referenced by the dtd.
            string file = uri.ToString();
            DefaultLocation = file.Substring(0, file.LastIndexOf('/') + 1);
            
            WebRequest req = WebRequest.Create(uri);
            WebResponse resp = req.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            
            Scanner = new Scanner(sr, trace, this);
            Dtd = new DTD();
        }
    
    /** Parses the DTD file and returns a DTD object describing the DTD.
        This invocation of parse does not try to guess the root element
        (for efficiency reasons) */
        public DTD Parse()     
        {
            return Parse(false);
        }

    /** Parses the DTD file and returns a DTD object describing the DTD.
     * @param guessRootElement If true, tells the parser to try to guess the
              root element of the document by process of elimination
     */
        public DTD Parse(bool guessRootElement)
        {
            Token token;

            for (;;)
            {
                token = Scanner.Peek();

                if (token.Type == Scanner.EOF) break;
                ParseTopLevelElement();
            }

            if (guessRootElement)
            {
                Hashtable roots = new Hashtable();
                
                foreach (DictionaryEntry de in Dtd.Elements)
                {
                    DTDElement element = (DTDElement) de.Value;
                    roots[element.Name] = element;
                }

                foreach (DictionaryEntry de in Dtd.Elements)
                {
                    DTDElement element = (DTDElement) de.Value;
                    if (!(element.Content is DTDContainer)) continue;
                    
                    //TODO: make sure this loop works.  it used to be based on a java enumeration.
                    foreach (DTDItem dtditem in ((DTDContainer)element.Content).Items)                    
                    {
                        RemoveElements(roots, Dtd, dtditem);
                    }
                }

                if (roots.Count == 1)
                {
                    IDictionaryEnumerator enumerator = roots.GetEnumerator();
                    enumerator.MoveNext();
                    Dtd.RootElement = (DTDElement)((DictionaryEntry)enumerator.Current).Value;
                }
                else
                {
                    Dtd.RootElement = null;
                }
            }
            else
            {
                Dtd.RootElement = null;
            }

            return Dtd;
        }

        protected void RemoveElements(Hashtable h, DTD dtd, DTDItem item)
        {
            if (item is DTDName)
            {
                h.Remove(((DTDName) item).Value);
            }
            else if (item is DTDContainer)
            {
                foreach(DTDItem dtditem in ((DTDContainer)item).Items)
                {
                    RemoveElements(h, dtd, dtditem);
                }
            }
        }

        protected void ParseTopLevelElement()    
        {
            Token token = Scanner.Get();

            // Is <? xxx ?> even valid in a DTD?  I'll ignore it just in case it's there
            if (token.Type == Scanner.LTQUES)
            {
                string textBuffer = "";

                for (;;)
                {
                    string text = Scanner.GetUntil('?');
                    textBuffer+=(text);

                    token = Scanner.Peek();
                    if (token.Type == Scanner.GT)
                    {
                        Scanner.Get();
                        break;
                    }
                    textBuffer+=('?');
                }
                DTDProcessingInstruction instruct =
                    new DTDProcessingInstruction(textBuffer);

                Dtd.Items.Add(instruct);

                return;
            }
            if (token.Type == Scanner.CONDITIONAL)
            {
                token = Expect(Scanner.IDENTIFIER);

                if (token.Value.Equals("IGNORE"))
                {
                    Scanner.SkipConditional();
                }
                else
                {
                    if (token.Value.Equals("INCLUDE"))
                    {
                        Scanner.SkipUntil('[');
                    }
                    else
                    {
                        throw new DTDParseException(Scanner.GetUriId(),
                                                    "Invalid token in conditional: "+token.Value,
                                                    Scanner.GetLineNumber(), Scanner.GetColumn());
                    }
                }
            }
            else if (token.Type == Scanner.ENDCONDITIONAL)
            {
                // Don't need to do anything for this token
            }
            else if (token.Type == Scanner.COMMENT)
            {
                Dtd.Items.Add(new DTDComment(token.Value));
            }
            else if (token.Type == Scanner.LTBANG)
            {

                token = Expect(Scanner.IDENTIFIER);

                if (token.Value.Equals("ELEMENT"))
                {
                    ParseElement();
                }
                else if (token.Value.Equals("ATTLIST"))
                {
                    ParseAttlist();
                }
                else if (token.Value.Equals("ENTITY"))
                {
                    ParseEntity();
                }
                else if (token.Value.Equals("NOTATION"))
                {
                    ParseNotation();
                }
                else
                {
                    SkipUntil(Scanner.GT);
                }
            }
            else
            {
                // MAW Version 1.17
                // Previously, the parser would skip over unexpected tokens at the
                // upper level. Some invalid DTDs would still show up as valid.
                throw new DTDParseException(Scanner.GetUriId(),
                                            "Unexpected token: "+ token.Type.Name+"("+token.Value+")",
                                            Scanner.GetLineNumber(), Scanner.GetColumn());
            }
        }

        protected void SkipUntil(TokenType stopToken)
        {
            Token token = Scanner.Get();

            while (token.Type != stopToken)
            {
                token = Scanner.Get();
            }
        }

        protected Token Expect(TokenType expected)
        {
            Token token = Scanner.Get();

            if (token.Type != expected)
            {
                if (token.Value == null)
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                "Expected "+expected.Name+" instead of "+token.Type.Name,
                                Scanner.GetLineNumber(), Scanner.GetColumn());
                }
                else
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                "Expected "+expected.Name+
                                    " instead of "+ token.Type.Name+"("+token.Value+")",
                                Scanner.GetLineNumber(), Scanner.GetColumn());
                }
            }

            return token;
        }

        protected void ParseElement()
        {
            Token name = Expect(Scanner.IDENTIFIER);

            DTDElement element = (DTDElement) Dtd.Elements[name.Value];

            if (element == null)
            {
                element = new DTDElement(name.Value);
                Dtd.Elements[element.Name] = element;
            }
            else if (element.Content != null)
            {
                // 070501 MAW: Since the ATTLIST tag can also cause an element to be created,
                // only throw this exception if the element has content defined, which
                // won't happen when you just create an ATTLIST. Thanks to
                // Jags Krishnamurthy of object Edge for pointing out this problem - 
                // originally the parser would let you define an element more than once.
                throw new DTDParseException(Scanner.GetUriId(),
                    "Found second definition of element: "+name.Value,
                            Scanner.GetLineNumber(), Scanner.GetColumn());
            }

            Dtd.Items.Add(element);
            ParseContentSpec(Scanner, element);

            Expect(Scanner.GT);
        }

        protected void ParseContentSpec(Scanner scanner, DTDElement element)
        {
            Token token = scanner.Get();

            if (token.Type == Scanner.IDENTIFIER)
            {
                if (token.Value.Equals("EMPTY"))
                {
                    element.Content = new DTDEmpty();
                }
                else if (token.Value.Equals("ANY"))
                {
                    element.Content = new DTDAny();
                }
                else
                {
                    throw new DTDParseException(scanner.GetUriId(),
                        "Invalid token in entity content spec "+
                            token.Value,
                            scanner.GetLineNumber(), scanner.GetColumn());
                }
            }
            else if (token.Type == Scanner.LPAREN)
            {
                token = scanner.Peek();

                if (token.Type == Scanner.IDENTIFIER)
                {
                    if (token.Value.Equals("#PCDATA"))
                    {
                        ParseMixed(element);
                    }
                    else
                    {
                        ParseChildren(element);
                    }
                }
                else if (token.Type == Scanner.LPAREN)
                {
                    ParseChildren(element);
                }
            }
        }

        protected void ParseMixed(DTDElement element)   
        {
            // MAW Version 1.19
            // Keep track of whether the mixed is #PCDATA only
            // Don't allow * after (#PCDATA), but allow after
            // (#PCDATA|foo|bar|baz)*
            bool isPcdataOnly = true;

            DTDMixed mixed = new DTDMixed();

            mixed.Items.Add(new DTDPCData());
            
            Scanner.Get();

            element.Content = mixed;

            for (;;)
            {
                Token token = Scanner.Get();

                if (token.Type == Scanner.RPAREN)
                {
                    token = Scanner.Peek();

                    if (token.Type == Scanner.ASTERISK)
                    {
                        Scanner.Get();
                        mixed.Cardinal = DTDCardinal.ZEROMANY;
                    }
                    else
                    {
                        if (!isPcdataOnly)
                        {
                            throw new DTDParseException(Scanner.GetUriId(),
                                            "Invalid token in Mixed content type, '*' required after (#PCDATA|xx ...): "+
                                            token.Type.Name, Scanner.GetLineNumber(), Scanner.GetColumn());
                        }

                        mixed.Cardinal = DTDCardinal.NONE;
                    }

                    return;
                }
                if (token.Type == Scanner.PIPE)
                {
                    token = Scanner.Get();

                    mixed.Items.Add(new DTDName(token.Value));

                    // MAW Ver. 1.19
                    isPcdataOnly = false;
                }
                else
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                                "Invalid token in Mixed content type: "+
                                                token.Type.Name, Scanner.GetLineNumber(), Scanner.GetColumn());
                }
            }
        }

        protected void ParseChildren(DTDElement element)  
        {
            DTDContainer choiceSeq = ParseChoiceSequence();

            Token token = Scanner.Peek();

            choiceSeq.Cardinal = ParseCardinality();

            if (token.Type == Scanner.QUES)
            {
                choiceSeq.Cardinal = DTDCardinal.OPTIONAL;
            }
            else if (token.Type == Scanner.ASTERISK)
            {
                choiceSeq.Cardinal = DTDCardinal.ZEROMANY;
            }
            else if (token.Type == Scanner.PLUS)
            {
                choiceSeq.Cardinal = DTDCardinal.ONEMANY;
            }
            else
            {
                choiceSeq.Cardinal = DTDCardinal.NONE;
            }

            element.Content = choiceSeq;
        }

        protected DTDContainer ParseChoiceSequence()
        {
            TokenType separator = null;

            DTDContainer cs = null;

            for (;;)
            {
                DTDItem item = ParseCp();

                Token token = Scanner.Get();

                if ((token.Type == Scanner.PIPE) ||
                    (token.Type == Scanner.COMMA))
                {
                    if ((separator != null) && (separator != token.Type))
                    {
                        throw new DTDParseException(Scanner.GetUriId(),
                            "Can't mix separators in a choice/sequence",
                            Scanner.GetLineNumber(), Scanner.GetColumn());
                    }
                    separator = token.Type;

                    if (cs == null)
                    {
                        if (token.Type == Scanner.PIPE)
                        {
                            cs = new DTDChoice();
                        }
                        else
                        {
                            cs = new DTDSequence();
                        }
                    }
                    cs.Items.Add(item);
                }
                else if (token.Type == Scanner.RPAREN)
                {
                    if (cs == null)
                    {
                        cs = new DTDSequence();
                    }
                    cs.Items.Add(item);
                    return cs;
                }
                else
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                    "Found invalid token in sequence: "+
                        token.Type.Name, Scanner.GetLineNumber(), Scanner.GetColumn());
                }
            }
        }

        protected DTDItem ParseCp()
        {
            Token token = Scanner.Get();

            DTDItem item;

            if (token.Type == Scanner.IDENTIFIER)
            {
                item = new DTDName(token.Value);
            }
            else if (token.Type == Scanner.LPAREN)
            {
                item = ParseChoiceSequence();
            }
            else
            {
                throw new DTDParseException(Scanner.GetUriId(),
                                "Found invalid token in sequence: "+
                                token.Type.Name, Scanner.GetLineNumber(),
                                Scanner.GetColumn());
            }

            item.Cardinal = ParseCardinality();

            return item;
        }

        protected DTDCardinal ParseCardinality()
        {
            Token token = Scanner.Peek();

            if (token.Type == Scanner.QUES)
            {
                Scanner.Get();
                return DTDCardinal.OPTIONAL;
            }
            if (token.Type == Scanner.ASTERISK)
            {
                Scanner.Get();
                return DTDCardinal.ZEROMANY;
            }
            if (token.Type == Scanner.PLUS)
            {
                Scanner.Get();
                return DTDCardinal.ONEMANY;
            }
            return DTDCardinal.NONE;
        }

        protected void ParseAttlist()
        {
            Token token = Expect(Scanner.IDENTIFIER);

            DTDElement element = (DTDElement) Dtd.Elements[token.Value];

            DTDAttlist attlist = new DTDAttlist(token.Value);

            Dtd.Items.Add(attlist);

            if (element == null)
            {
                element = new DTDElement(token.Value);
                Dtd.Elements[token.Value] = element;
            }

            token = Scanner.Peek();

            while (token.Type != Scanner.GT)
            {
                ParseAttdef(Scanner, element, attlist);
                token = Scanner.Peek();
            }
            // MAW Version 1.17
            // Prior to this version, the parser didn't actually consume the > at the
            // end of the ATTLIST definition. Because the parser ignored unexpected tokens
            // at the top level, it was ignoring the >. In parsing DOCBOOK, however, there
            // were two unexpected tokens, bringing this error to light.
            Expect(Scanner.GT);
        }

        protected void ParseAttdef(Scanner scanner, DTDElement element, DTDAttlist attlist)
        {
            Token token = Expect(Scanner.IDENTIFIER);

            DTDAttribute attr = new DTDAttribute(token.Value);

            attlist.Attributes.Add(attr);

            element.Attributes[token.Value] = attr;

            token = scanner.Get();

            if (token.Type == Scanner.IDENTIFIER)
            {
                if (token.Value.Equals("NOTATION"))
                {
                    attr.Type = ParseNotationList();
                }
                else
                {
                    attr.Type = token.Value;
                }
            }
            else if (token.Type == Scanner.LPAREN)
            {
                attr.Type = ParseEnumeration();
            }

            token = scanner.Peek();

            if (token.Type == Scanner.IDENTIFIER)
            {
                scanner.Get();
                if (token.Value.Equals("#FIXED"))
                {
                    attr.Decl = DTDDecl.FIXED;

                    token = scanner.Get();
                    attr.DefaultValue = token.Value;
                }
                else if (token.Value.Equals("#REQUIRED"))
                {
                    attr.Decl = DTDDecl.REQUIRED;
                }
                else if (token.Value.Equals("#IMPLIED"))
                {
                    attr.Decl = DTDDecl.IMPLIED;
                }
                else
                {
                    throw new DTDParseException(scanner.GetUriId(),
                        "Invalid token in attribute declaration: "+
                        token.Value, scanner.GetLineNumber(), scanner.GetColumn());
                }
            }
            else if (token.Type == Scanner.STRING)
            {
                scanner.Get();
                attr.Decl = DTDDecl.VALUE;
                attr.DefaultValue = token.Value;
            }
        }

        protected DTDNotationList ParseNotationList()
        {
            DTDNotationList notation = new DTDNotationList();

            Token token = Scanner.Get();
            if (token.Type != Scanner.LPAREN)
            {
                throw new DTDParseException(Scanner.GetUriId(),
                    "Invalid token in notation: "+
                    token.Type.Name, Scanner.GetLineNumber(),
                    Scanner.GetColumn());
            }

            for (;;)
            {
                token = Scanner.Get();

                if (token.Type != Scanner.IDENTIFIER)
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                    "Invalid token in notation: "+
                                    token.Type.Name, Scanner.GetLineNumber(),
                                    Scanner.GetColumn());
                }

                notation.Items.Add(token.Value);

                token = Scanner.Peek();

                if (token.Type == Scanner.RPAREN)
                {
                    Scanner.Get();
                    return notation;
                }
                if (token.Type != Scanner.PIPE)
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                                "Invalid token in notation: "+
                                                token.Type.Name, Scanner.GetLineNumber(),
                                                Scanner.GetColumn());
                }
                Scanner.Get(); // eat the pipe
            }
        }

        protected DTDEnumeration ParseEnumeration()   
        {
            DTDEnumeration enumeration = new DTDEnumeration();

            for (;;)
            {
                Token token = Scanner.Get();

                if ((token.Type != Scanner.IDENTIFIER) &&
                    (token.Type != Scanner.NMTOKEN))
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                    "Invalid token in enumeration: "+
                                    token.Type.Name, Scanner.GetLineNumber(),
                                    Scanner.GetColumn());
                }

                enumeration.Items.Add(token.Value);

                token = Scanner.Peek();

                if (token.Type == Scanner.RPAREN)
                {
                    Scanner.Get();
                    return enumeration;
                }
                if (token.Type != Scanner.PIPE)
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                                "Invalid token in enumeration: "+
                                                token.Type.Name, Scanner.GetLineNumber(),
                                                Scanner.GetColumn());
                }
                Scanner.Get(); // eat the pipe
            }
        }

        protected void ParseEntity()
        {
            bool isParsed = false;

            Token name = Scanner.Get();

            if (name.Type == Scanner.PERCENT)
            {
                isParsed = true;
                name = Expect(Scanner.IDENTIFIER);
            }
            else if (name.Type != Scanner.IDENTIFIER)
            {
                throw new DTDParseException(Scanner.GetUriId(),
                                "Invalid entity declaration",
                                Scanner.GetLineNumber(), Scanner.GetColumn());
            }

            DTDEntity entity = (DTDEntity) Dtd.Entities[name.Value];

            bool skip = false;

            if (entity == null)
            {
                entity = new DTDEntity(name.Value, DefaultLocation);
                Dtd.Entities[entity.Name] = entity;
            }
            else
            {
                // 070501 MAW: If the entity already exists, create a dummy entity - this way
                // you keep the original definition.  Thanks to Jags Krishnamurthy of object
                // Edge for pointing out this problem and for pointing out the solution
                entity = new DTDEntity(name.Value, DefaultLocation);
                skip = true;
            }

            Dtd.Items.Add(entity);

            entity.IsParsed = isParsed;

            ParseEntityDef(entity);

            if (entity.IsParsed && (entity.Value != null) && !skip)
            {
                Scanner.AddEntity(entity.Name, entity.Value);
            }
        }

        protected void ParseEntityDef(DTDEntity entity)
        {
            Token token = Scanner.Get();

            if (token.Type == Scanner.STRING)
            {
                // Only set the entity value if it hasn't been set yet
                // XML 1.0 spec says that you use the first value of
                // an entity, not the most recent.
                if (entity.Value == null)
                {
                    entity.Value = token.Value;
                }
            }
            else if (token.Type == Scanner.IDENTIFIER)
            {
                if (token.Value.Equals("SYSTEM"))
                {
                    DTDSystem sys = new DTDSystem();
                    token = Expect(Scanner.STRING);

                    sys.System = token.Value;
                    entity.ExternalId = sys;
                }
                else if (token.Value.Equals("PUBLIC"))
                {
                    DTDPublic pub = new DTDPublic();

                    token = Expect(Scanner.STRING);
                    pub.Pub = token.Value;
                    token = Expect(Scanner.STRING);
                    pub.System = token.Value;
                    entity.ExternalId = pub;
                }
                else
                {
                    throw new DTDParseException(Scanner.GetUriId(),
                                    "Invalid External ID specification",
                                    Scanner.GetLineNumber(), Scanner.GetColumn());
                }


                // ISSUE: isParsed is set to TRUE if this is a Parameter Entity
                //     Reference (assuming this is because Parameter Entity
                //     external references are parsed, whereas General Entity
                //     external references are irrelevant for this product).
                //     However, NDATA is only valid if this is
                //     a General Entity Reference. So, "if" conditional should
                //     be (!entity.isParsed) rather than (entity.isParsed).
                //
                //Entity Declaration
                // [70] EntityDecl ::= GEDecl | PEDecl
                // [71] GEDecl ::= '<!ENTITY' S Name S EntityDef S? '>'
                // [72] PEDecl ::= '<!ENTITY' S '%' S Name S PEDef S? '>'
                // [73] EntityDef ::= EntityValue | (ExternalID NDataDecl?)
                // [74] PEDef ::= EntityValue | ExternalID
                //External Entity Declaration
                // [75] ExternalID ::= 'SYSTEM' S SystemLiteral
                //          | 'PUBLIC' S PubidLiteral S SystemLiteral
                // [76] NDataDecl ::= S 'NDATA' S Name [ VC: Notation Declared ]

                if (!entity.IsParsed) // CHANGE 1
                {
                    token = Scanner.Peek();
                    if (token.Type == Scanner.IDENTIFIER)
                    {
                        if (!token.Value.Equals("NDATA"))
                        {
                            throw new DTDParseException(Scanner.GetUriId(),
                                "Invalid NData declaration",
                                Scanner.GetLineNumber(), Scanner.GetColumn());
                        }
                        // CHANGE 2: Add call to scanner.get.
                        //      This gets "NDATA" IDENTIFIER.
                        token = Scanner.Get();
                        // Get the NDATA "Name" IDENTIFIER.
                        token = Expect(Scanner.IDENTIFIER);
                        // Save the ndata value
                        entity.Ndata = token.Value;
                    }
                }
            }
            else
            {
                throw new DTDParseException(Scanner.GetUriId(),
                                "Invalid entity definition",
                                Scanner.GetLineNumber(), Scanner.GetColumn());
            }

            Expect(Scanner.GT);
        }

        protected void ParseNotation()
        {
            
            Token token = Expect(Scanner.IDENTIFIER);
            DTDNotation notation = new DTDNotation(token.Value);

            Dtd.Notations[notation.Name] = notation;
            Dtd.Items.Add(notation);

            token = Expect(Scanner.IDENTIFIER);

            if (token.Value.Equals("SYSTEM"))
            {
                DTDSystem sys = new DTDSystem();
                token = Expect(Scanner.STRING);

                sys.System = token.Value;
                notation.ExternalId = sys;
            }
            else if (token.Value.Equals("PUBLIC"))
            {
                DTDPublic pub = new DTDPublic();
                token = Expect(Scanner.STRING);

                pub.Pub = token.Value;
                pub.System = null;

                // For <!NOTATION>, you can have PUBLIC PubidLiteral without
                // a SystemLiteral
                token = Scanner.Peek();
                if (token.Type == Scanner.STRING)
                {
                    token = Scanner.Get();
                    pub.System = token.Value;
                }

                notation.ExternalId = pub;
            }
            Expect(Scanner.GT);
        }

        public DTDEntity ExpandEntity(string name)
        {
            return (DTDEntity) Dtd.Entities[name];
        }
    }
}