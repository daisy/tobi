using System.Collections;
using System.IO;

namespace DtdParser
{ 
    public class DTDParser : IEntityExpansion
    {
        protected Scanner scanner;
        protected DTD dtd;
        protected object defaultLocation;

    /** Creates a parser that will read from the specified Reader object */
        public DTDParser(StreamReader reader)
        {
            scanner = new Scanner(reader, false, this);
            dtd = new DTD();
        }

    /** Creates a parser that will read from the specified Reader object
     * @param in The input stream to read
     * @param trace True if the parser should print out tokens as it reads them
     *  (used for debugging the parser)
     */
        public DTDParser(StreamReader reader, bool trace)
        {
            scanner = new Scanner(reader, trace, this);
            dtd = new DTD();
        }
 

    /** Creates a parser that will read from the specified File object */
     /*   public DTDParser(File in)
            
        {
            defaultLocation = in.getParentFile();

            scanner = new Scanner(new BufferedReader(new FileReader(in)),
                false, this);
            dtd = new DTD();
        }
    */
    /** Creates a parser that will read from the specified File object
     * @param in The file to read
     * @param trace True if the parser should print out tokens as it reads them
     *  (used for debugging the parser)
     */
     /*   public DTDParser(File in, bool trace)
            
        {
            defaultLocation = in.getParentFile();

            scanner = new Scanner(new BufferedReader(new FileReader(in)),
                trace, this);
            dtd = new DTD();
        }
    */
    /** Creates a parser that will read from the specified URL object */
     /*   public DTDParser(URL in)
            
        {
        //LAM: we need to set the defaultLocation to the directory where
        //the dtd is found so that we don't run into problems parsing any
        //relative external files referenced by the dtd.
            string file = in.getFile();
            defaultLocation = new URL(in.getProtocol(), in.getHost(), in.getPort(), file.substring(0, file.lastIndexOf('/') + 1));

            scanner = new Scanner(new BufferedReader(
                new InputStreamReader(in.openStream())), false, this);
            dtd = new DTD();
        }
    */
    /** Creates a parser that will read from the specified URL object
     * @param in The URL to read
     * @param trace True if the parser should print out tokens as it reads them
     *  (used for debugging the parser)
     */
     /*   public DTDParser(URL in, bool trace)
            
        {
        //LAM: we need to set the defaultLocation to the directory where
        //the dtd is found so that we don't run into problems parsing any
        //relative external files referenced by the dtd.
            string file = in.getFile();
            defaultLocation = new URL(in.getProtocol(), in.getHost(), in.getPort(), file.substring(0, file.lastIndexOf('/') + 1));


            scanner = new Scanner(new BufferedReader(
                new InputStreamReader(in.openStream())), trace, this);
            dtd = new DTD();
        }
    */
    /** Parses the DTD file and returns a DTD object describing the DTD.
        This invocation of parse does not try to guess the root element
        (for efficiency reasons) */
        public DTD parse()     
        {
            return parse(false);
        }

    /** Parses the DTD file and returns a DTD object describing the DTD.
     * @param guessRootElement If true, tells the parser to try to guess the
              root element of the document by process of elimination
     */
        public DTD parse(bool guessRootElement)
        {
            Token token;

            for (;;)
            {
                token = scanner.peek();

                if (token.Type == Scanner.EOF) break;

                parseTopLevelElement();
            }

            if (guessRootElement)
            {
                Hashtable roots = new Hashtable();
                
                foreach (DictionaryEntry de in dtd.elements)
                {
                    DTDElement element = (DTDElement) de.Value;
                    roots[element.name] = element;
                }

                foreach (DictionaryEntry de in dtd.elements)
                {
                    DTDElement element = (DTDElement) de.Value;
                    if (!(element.content is DTDContainer)) continue;
                    
                    //TODO: make sure this loop works.  it used to be based on a java enumeration.
                    foreach (DTDItem dtditem in ((DTDContainer)element.content).items)                    
                    {
                        removeElements(roots, dtd, dtditem);
                    }
                }

                if (roots.Count == 1)
                {
                    IDictionaryEnumerator enumerator = roots.GetEnumerator();
                    enumerator.MoveNext();
                    dtd.rootElement = (DTDElement)enumerator.Current;
                }
                else
                {
                    dtd.rootElement = null;
                }
            }
            else
            {
                dtd.rootElement = null;
            }

            return dtd;
        }

        protected void removeElements(Hashtable h, DTD dtd, DTDItem item)
        {
            if (item is DTDName)
            {
                h.Remove(((DTDName) item).value);
            }
            else if (item is DTDContainer)
            {
                foreach(DTDItem dtditem in ((DTDContainer)item).items)
                {
                    removeElements(h, dtd, dtditem);
                }
            }
        }

        protected void parseTopLevelElement()
            
        {
            Token token = scanner.get();

    // Is <? xxx ?> even valid in a DTD?  I'll ignore it just in case it's there
            if (token.Type == Scanner.LTQUES)
            {
                string textBuffer = "";

                for (;;)
                {
                    string text = scanner.getUntil('?');
                    textBuffer+=(text);

                    token = scanner.peek();
                    if (token.Type == Scanner.GT)
                    {
                        scanner.get();
                        break;
                    }
                    textBuffer+=('?');
                }
                DTDProcessingInstruction instruct =
                    new DTDProcessingInstruction(textBuffer);

                dtd.items.Add(instruct);

                return;
            }
            else if (token.Type == Scanner.CONDITIONAL)
            {
                token = expect(Scanner.IDENTIFIER);

                if (token.Value.Equals("IGNORE"))
                {
                    scanner.skipConditional();
                }
                else
                {
                    if (token.Value.Equals("INCLUDE"))
                    {
                        scanner.skipUntil('[');
                    }
                    else
                    {
                        throw new DTDParseException(scanner.getUriId(),
                            "Invalid token in conditional: "+token.Value,
                            scanner.getLineNumber(), scanner.getColumn());
                    }
                }
            }
            else if (token.Type == Scanner.ENDCONDITIONAL)
            {
                // Don't need to do anything for this token
            }
            else if (token.Type == Scanner.COMMENT)
            {
                dtd.items.Add(
                    new DTDComment(token.Value));
            }
            else if (token.Type == Scanner.LTBANG)
            {

                token = expect(Scanner.IDENTIFIER);

                if (token.Value.Equals("ELEMENT"))
                {
                    parseElement();
                }
                else if (token.Value.Equals("ATTLIST"))
                {
                    parseAttlist();
                }
                else if (token.Value.Equals("ENTITY"))
                {
                    parseEntity();
                }
                else if (token.Value.Equals("NOTATION"))
                {
                    parseNotation();
                }
                else
                {
                    skipUntil(Scanner.GT);
                }
            }
            else
            {
    // MAW Version 1.17
    // Previously, the parser would skip over unexpected tokens at the
    // upper level. Some invalid DTDs would still show up as valid.
                throw new DTDParseException(scanner.getUriId(),
                            "Unexpected token: "+ token.Type.Name+"("+token.Value+")",
                            scanner.getLineNumber(), scanner.getColumn());
            }

        }

        protected void skipUntil(TokenType stopToken)
            
        {
            Token token = scanner.get();

            while (token.Type != stopToken)
            {
                token = scanner.get();
            }
        }

        protected Token expect(TokenType expected)
            
        {
            Token token = scanner.get();

            if (token.Type != expected)
            {
                if (token.Value == null)
                {
                    throw new DTDParseException(scanner.getUriId(),
                                "Expected "+expected.Name+" instead of "+token.Type.Name,
                                scanner.getLineNumber(), scanner.getColumn());
                }
                else
                {
                    throw new DTDParseException(scanner.getUriId(),
                                "Expected "+expected.Name+
                                    " instead of "+ token.Type.Name+"("+token.Value+")",
                                scanner.getLineNumber(), scanner.getColumn());
                }
            }

            return token;
        }

        protected void parseElement()
            
        {
            Token name = expect(Scanner.IDENTIFIER);

            DTDElement element = (DTDElement) dtd.elements[name.Value];

            if (element == null)
            {
                element = new DTDElement(name.Value);
                dtd.elements[element.name] = element;
            }
            else if (element.content != null)
            {
    // 070501 MAW: Since the ATTLIST tag can also cause an element to be created,
    // only throw this exception if the element has content defined, which
    // won't happen when you just create an ATTLIST. Thanks to
    // Jags Krishnamurthy of object Edge for pointing out this problem - 
    // originally the parser would let you define an element more than once.
                throw new DTDParseException(scanner.getUriId(),
                    "Found second definition of element: "+name.Value,
                            scanner.getLineNumber(), scanner.getColumn());
            }

            dtd.items.Add(element);
            parseContentSpec(scanner, element);

            expect(Scanner.GT);
        }

        protected void parseContentSpec(Scanner scanner, DTDElement element)
        {
            Token token = scanner.get();

            if (token.Type == Scanner.IDENTIFIER)
            {
                if (token.Value.Equals("EMPTY"))
                {
                    element.content = new DTDEmpty();
                }
                else if (token.Value.Equals("ANY"))
                {
                    element.content = new DTDAny();
                }
                else
                {
                    throw new DTDParseException(scanner.getUriId(),
                        "Invalid token in entity content spec "+
                            token.Value,
                            scanner.getLineNumber(), scanner.getColumn());
                }
            }
            else if (token.Type == Scanner.LPAREN)
            {
                token = scanner.peek();

                if (token.Type == Scanner.IDENTIFIER)
                {
                    if (token.Value.Equals("#PCDATA"))
                    {
                        parseMixed(element);
                    }
                    else
                    {
                        parseChildren(element);
                    }
                }
                else if (token.Type == Scanner.LPAREN)
                {
                    parseChildren(element);
                }
            }
        }

        protected void parseMixed(DTDElement element)
            
        {
            // MAW Version 1.19
            // Keep track of whether the mixed is #PCDATA only
            // Don't allow * after (#PCDATA), but allow after
            // (#PCDATA|foo|bar|baz)*
            bool isPcdataOnly = true;

            DTDMixed mixed = new DTDMixed();

            mixed.items.Add(new DTDPCData());
            
            scanner.get();

            element.content = mixed;

            for (;;)
            {
                Token token = scanner.get();

                if (token.Type == Scanner.RPAREN)
                {
                    token = scanner.peek();

                    if (token.Type == Scanner.ASTERISK)
                    {
                        scanner.get();
                        mixed.cardinal = DTDCardinal.ZEROMANY;
                    }
                    else
                    {
                        if (!isPcdataOnly)
                        {
                            throw new DTDParseException(scanner.getUriId(),
                                            "Invalid token in Mixed content type, '*' required after (#PCDATA|xx ...): "+
                                            token.Type.Name, scanner.getLineNumber(), scanner.getColumn());
                        }

                        mixed.cardinal = DTDCardinal.NONE;
                    }

                    return;
                }
                else if (token.Type == Scanner.PIPE)
                {
                    token = scanner.get();

                    mixed.items.Add(new DTDName(token.Value));

                    // MAW Ver. 1.19
                    isPcdataOnly = false;
                }
                else
                {
                    throw new DTDParseException(scanner.getUriId(),
                                    "Invalid token in Mixed content type: "+
                                    token.Type.Name, scanner.getLineNumber(), scanner.getColumn());
                }
            }
        }

        protected void parseChildren(DTDElement element)
            
        {
            DTDContainer choiceSeq = parseChoiceSequence();

            Token token = scanner.peek();

            choiceSeq.cardinal = parseCardinality();

            if (token.Type == Scanner.QUES)
            {
                choiceSeq.cardinal = DTDCardinal.OPTIONAL;
            }
            else if (token.Type == Scanner.ASTERISK)
            {
                choiceSeq.cardinal = DTDCardinal.ZEROMANY;
            }
            else if (token.Type == Scanner.PLUS)
            {
                choiceSeq.cardinal = DTDCardinal.ONEMANY;
            }
            else
            {
                choiceSeq.cardinal = DTDCardinal.NONE;
            }

            element.content = choiceSeq;
        }

        protected DTDContainer parseChoiceSequence()
            
        {
            TokenType separator = null;

            DTDContainer cs = null;

            for (;;)
            {
                DTDItem item = parseCP();

                Token token = scanner.get();

                if ((token.Type == Scanner.PIPE) ||
                    (token.Type == Scanner.COMMA))
                {
                    if ((separator != null) && (separator != token.Type))
                    {
                        throw new DTDParseException(scanner.getUriId(),
                            "Can't mix separators in a choice/sequence",
                            scanner.getLineNumber(), scanner.getColumn());
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
                    cs.items.Add(item);
                }
                else if (token.Type == Scanner.RPAREN)
                {
                    if (cs == null)
                    {
                        cs = new DTDSequence();
                    }
                    cs.items.Add(item);
                    return cs;
                }
                else
                {
                    throw new DTDParseException(scanner.getUriId(),
                                    "Found invalid token in sequence: "+
                        token.Type.Name, scanner.getLineNumber(), scanner.getColumn());
                }
            }
        }

        protected DTDItem parseCP()
            
        {
            Token token = scanner.get();

            DTDItem item = null;

            if (token.Type == Scanner.IDENTIFIER)
            {
                item = new DTDName(token.Value);
            }
            else if (token.Type == Scanner.LPAREN)
            {
                item = parseChoiceSequence();
            }
            else
            {
                throw new DTDParseException(scanner.getUriId(),
                                "Found invalid token in sequence: "+
                                token.Type.Name, scanner.getLineNumber(),
                                scanner.getColumn());
            }

            item.cardinal = parseCardinality();

            return item;
        }

        protected DTDCardinal parseCardinality()
        {
            Token token = scanner.peek();

            if (token.Type == Scanner.QUES)
            {
                scanner.get();
                return DTDCardinal.OPTIONAL;
            }
            else if (token.Type == Scanner.ASTERISK)
            {
                scanner.get();
                return DTDCardinal.ZEROMANY;
            }
            else if (token.Type == Scanner.PLUS)
            {
                scanner.get();
                return DTDCardinal.ONEMANY;
            }
            else
            {
                return DTDCardinal.NONE;
            }
        }

        protected void parseAttlist()
        {
            Token token = expect(Scanner.IDENTIFIER);

            DTDElement element = (DTDElement) dtd.elements[token.Value];

            DTDAttlist attlist = new DTDAttlist(token.Value);

            dtd.items.Add(attlist);

            if (element == null)
            {
                element = new DTDElement(token.Value);
                dtd.elements[token.Value] = element;
            }

            token = scanner.peek();

            while (token.Type != Scanner.GT)
            {
                parseAttdef(scanner, element, attlist);
                token = scanner.peek();
            }
    // MAW Version 1.17
    // Prior to this version, the parser didn't actually consume the > at the
    // end of the ATTLIST definition. Because the parser ignored unexpected tokens
    // at the top level, it was ignoring the >. In parsing DOCBOOK, however, there
    // were two unexpected tokens, bringing this error to light.
            expect(Scanner.GT);
        }

        protected void parseAttdef(Scanner scanner, DTDElement element,
            DTDAttlist attlist)
            
        {
            Token token = expect(Scanner.IDENTIFIER);

            DTDAttribute attr = new DTDAttribute(token.Value);

            attlist.attributes.Add(attr);

            element.attributes[token.Value] = attr;

            token = scanner.get();

            if (token.Type == Scanner.IDENTIFIER)
            {
                if (token.Value.Equals("NOTATION"))
                {
                    attr.type = parseNotationList();
                }
                else
                {
                    attr.type = token.Value;
                }
            }
            else if (token.Type == Scanner.LPAREN)
            {
                attr.type = parseEnumeration();
            }

            token = scanner.peek();

            if (token.Type == Scanner.IDENTIFIER)
            {
                scanner.get();
                if (token.Value.Equals("#FIXED"))
                {
                    attr.decl = DTDDecl.FIXED;

                    token = scanner.get();
                    attr.defaultValue = token.Value;
                }
                else if (token.Value.Equals("#REQUIRED"))
                {
                    attr.decl = DTDDecl.REQUIRED;
                }
                else if (token.Value.Equals("#IMPLIED"))
                {
                    attr.decl = DTDDecl.IMPLIED;
                }
                else
                {
                    throw new DTDParseException(scanner.getUriId(),
                        "Invalid token in attribute declaration: "+
                        token.Value, scanner.getLineNumber(), scanner.getColumn());
                }
            }
            else if (token.Type == Scanner.STRING)
            {
                scanner.get();
                attr.decl = DTDDecl.VALUE;
                attr.defaultValue = token.Value;
            }
        }

        protected DTDNotationList parseNotationList()
            
        {
            DTDNotationList notation = new DTDNotationList();

            Token token = scanner.get();
            if (token.Type != Scanner.LPAREN)
            {
                throw new DTDParseException(scanner.getUriId(),
                    "Invalid token in notation: "+
                    token.Type.Name, scanner.getLineNumber(),
                    scanner.getColumn());
            }

            for (;;)
            {
                token = scanner.get();

                if (token.Type != Scanner.IDENTIFIER)
                {
                    throw new DTDParseException(scanner.getUriId(),
                                    "Invalid token in notation: "+
                                    token.Type.Name, scanner.getLineNumber(),
                                    scanner.getColumn());
                }

                notation.items.Add(token.Value);

                token = scanner.peek();

                if (token.Type == Scanner.RPAREN)
                {
                    scanner.get();
                    return notation;
                }
                else if (token.Type != Scanner.PIPE)
                {
                    throw new DTDParseException(scanner.getUriId(),
                                    "Invalid token in notation: "+
                                    token.Type.Name, scanner.getLineNumber(),
                                    scanner.getColumn());
                }
                scanner.get(); // eat the pipe
            }
        }

        protected DTDEnumeration parseEnumeration()
            
        {
            DTDEnumeration enumeration = new DTDEnumeration();

            for (;;)
            {
                Token token = scanner.get();

                if ((token.Type != Scanner.IDENTIFIER) &&
                    (token.Type != Scanner.NMTOKEN))
                {
                    throw new DTDParseException(scanner.getUriId(),
                                    "Invalid token in enumeration: "+
                                    token.Type.Name, scanner.getLineNumber(),
                                    scanner.getColumn());
                }

                enumeration.items.Add(token.Value);

                token = scanner.peek();

                if (token.Type == Scanner.RPAREN)
                {
                    scanner.get();
                    return enumeration;
                }
                else if (token.Type != Scanner.PIPE)
                {
                    throw new DTDParseException(scanner.getUriId(),
                                    "Invalid token in enumeration: "+
                                    token.Type.Name, scanner.getLineNumber(),
                                    scanner.getColumn());
                }
                scanner.get(); // eat the pipe
            }
        }

        protected void parseEntity()
            
        {
            bool isParsed = false;

            Token name = scanner.get();

            if (name.Type == Scanner.PERCENT)
            {
                isParsed = true;
                name = expect(Scanner.IDENTIFIER);
            }
            else if (name.Type != Scanner.IDENTIFIER)
            {
                throw new DTDParseException(scanner.getUriId(),
                                "Invalid entity declaration",
                                scanner.getLineNumber(), scanner.getColumn());
            }

            DTDEntity entity = (DTDEntity) dtd.entities[name.Value];

            bool skip = false;

            if (entity == null)
            {
                entity = new DTDEntity(name.Value, defaultLocation);
                dtd.entities[entity.name] = entity;
            }
            else
            {
    // 070501 MAW: If the entity already exists, create a dummy entity - this way
    // you keep the original definition.  Thanks to Jags Krishnamurthy of object
    // Edge for pointing out this problem and for pointing out the solution
                entity = new DTDEntity(name.Value, defaultLocation);
                skip = true;
            }

            dtd.items.Add(entity);

            entity.isParsed = isParsed;

            parseEntityDef(entity);

            if (entity.isParsed && (entity.value != null) && !skip)
            {
                scanner.addEntity(entity.name, entity.value);
            }
        }

        protected void parseEntityDef(DTDEntity entity)
            
        {
            Token token = scanner.get();

            if (token.Type == Scanner.STRING)
            {
                // Only set the entity value if it hasn't been set yet
                // XML 1.0 spec says that you use the first value of
                // an entity, not the most recent.
                if (entity.value == null)
                {
                    entity.value = token.Value;
                }
            }
            else if (token.Type == Scanner.IDENTIFIER)
            {
                if (token.Value.Equals("SYSTEM"))
                {
                    DTDSystem sys = new DTDSystem();
                    token = expect(Scanner.STRING);

                    sys.system = token.Value;
                    entity.externalID = sys;
                }
                else if (token.Value.Equals("PUBLIC"))
                {
                    DTDPublic pub = new DTDPublic();

                    token = expect(Scanner.STRING);
                    pub.Pub = token.Value;
                    token = expect(Scanner.STRING);
                    pub.system = token.Value;
                    entity.externalID = pub;
                }
                else
                {
                    throw new DTDParseException(scanner.getUriId(),
                                    "Invalid External ID specification",
                                    scanner.getLineNumber(), scanner.getColumn());
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

                if (!entity.isParsed) // CHANGE 1
                {
                    token = scanner.peek();
                    if (token.Type == Scanner.IDENTIFIER)
                    {
                        if (!token.Value.Equals("NDATA"))
                        {
                            throw new DTDParseException(scanner.getUriId(),
                                "Invalid NData declaration",
                                scanner.getLineNumber(), scanner.getColumn());
                        }
                        // CHANGE 2: Add call to scanner.get.
                        //      This gets "NDATA" IDENTIFIER.
                        token = scanner.get();
                        // Get the NDATA "Name" IDENTIFIER.
                        token = expect(Scanner.IDENTIFIER);
                        // Save the ndata value
                        entity.ndata = token.Value;
                    }
                }
            }
            else
            {
                throw new DTDParseException(scanner.getUriId(),
                                "Invalid entity definition",
                                scanner.getLineNumber(), scanner.getColumn());
            }

            expect(Scanner.GT);
        }

        protected void parseNotation()
           // throws java.io.IOException
        {
            
            Token token = expect(Scanner.IDENTIFIER);
            DTDNotation notation = new DTDNotation(token.Value);

            dtd.notations[notation.name] = notation;
            dtd.items.Add(notation);

            token = expect(Scanner.IDENTIFIER);

            if (token.Value.Equals("SYSTEM"))
            {
                DTDSystem sys = new DTDSystem();
                token = expect(Scanner.STRING);

                sys.system = token.Value;
                notation.externalID = sys;
            }
            else if (token.Value.Equals("PUBLIC"))
            {
                DTDPublic pub = new DTDPublic();
                token = expect(Scanner.STRING);

                pub.Pub = token.Value;
                pub.system = null;

    // For <!NOTATION>, you can have PUBLIC PubidLiteral without
    // a SystemLiteral
                token = scanner.peek();
                if (token.Type == Scanner.STRING)
                {
                    token = scanner.get();
                    pub.system = token.Value;
                }

                notation.externalID = pub;
            }
            expect(Scanner.GT);
        }

        public DTDEntity expandEntity(string name)
        {
            return (DTDEntity) dtd.entities[name];
        }
    }
}