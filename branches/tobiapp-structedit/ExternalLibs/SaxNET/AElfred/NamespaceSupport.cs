using System;
using System.Collections;

// NamespaceSupport.cs, based on NamespaceSupport.java - generic Namespace support for SAX.
// http://www.saxproject.org
// Original Developer: David Megginson
// This class is in the Public Domain. NO WARRANTY!
// Based on NamespaceSupport.java,v 1.15

/// <summary>
/// Encapsulate Namespace logic for use by applications using SAX,
/// or internally by SAX drivers.
/// <blockquote>
/// <em>This module, both source code and documentation, is in the
/// Public Domain, and comes with <strong>NO WARRANTY</strong>.</em>
/// See <a href='http://www.saxproject.org'>http://www.saxproject.org</a>
/// for further information.
/// </blockquote>
/// </summary>
public class NamespaceSupport
{

  ////////////////////////////////////////////////////////////////////
  // Constants.
  ////////////////////////////////////////////////////////////////////
  
  /// <summary>
  /// The XML Namespace URI as a constant.
  /// The value is <code>http://www.w3.org/XML/1998/namespace</code>
  /// as defined in the "Namespaces in XML"/// recommendation.
  /// <p>This is the Namespace URI that is automatically mapped
  /// to the "xml" prefix.</p>
  /// </summary>
  public static string XMLNS = "http://www.w3.org/XML/1998/namespace";

  /// <summary>
  /// The namespace declaration URI as a constant.
  /// The value is <code>http://www.w3.org/2000/xmlns/</code>, as defined
  /// in a backwards-incompatible erratum to the "Namespaces in XML"
  /// recommendation.  Because that erratum postdated SAX2, SAX2 defaults 
  /// to the original recommendation, and does not normally use this URI.
  /// 
  /// <p>This is the Namespace URI that is optionally applied to
  /// <em>xmlns</em> and <em>xmlns:*</em> attributes, which are used to
  /// declare namespaces.  </p>
  /// </summary>
  public static string NSDECL = "http://www.w3.org/2000/xmlns/";
  
  /// <summary>
  /// An empty enumeration.
  /// </summary>
  private static ArrayList EMPTY_ARRAYLIST = new ArrayList();

  ////////////////////////////////////////////////////////////////////
  // Constructor.
  ////////////////////////////////////////////////////////////////////

  /// <summary>
  /// Create a new Namespace support object.
  /// </summary>
  public NamespaceSupport ()
  {
    Reset();
  }

  ////////////////////////////////////////////////////////////////////
  // Context management.
  ////////////////////////////////////////////////////////////////////

  /// <summary>
  /// Reset this Namespace support object for reuse.
  /// <p>It is necessary to invoke this method before reusing the
  /// Namespace support object for a new session.  If namespace
  /// declaration URIs are to be supported, that flag must also
  /// be set to a non-default value.
  /// </p>
  /// <see cref="NamespaceDeclUris"/>
  /// </summary>
  public void Reset ()
  {
    contexts = new Context[32];
    contextPos = 0;
    contexts[contextPos] = currentContext = new Context(namespaceDeclUris);
    currentContext.DeclarePrefix("xml", XMLNS);
  }

  /// <summary>
  /// Start a new Namespace context. The new context will automatically inherit
  /// the declarations of its parent context, but it will also keep track of 
  /// which declarations were made within this context.
  ///
  /// <p>Event callback code should start a new context once per element.
  /// This means being ready to call this in either of two places.
  /// For elements that don't include namespace declarations, the
  /// <em>ContentHandler.startElement()</em> callback is the right place.
  /// For elements with such a declaration, it'd done in the first
  /// <em>ContentHandler.startPrefixMapping()</em> callback.
  /// A bool flag can be used to
  /// track whether a context has been started yet.  When either of
  /// those methods is called, it checks the flag to see if a new context
  /// needs to be started.  If so, it starts the context and sets the
  /// flag.  After <em>ContentHandler.startElement()</em>
  /// does that, it always clears the flag.</p>
  ///
  /// <p>Normally, SAX drivers would push a new context at the beginning
  /// of each XML element.  Then they perform a first pass over the
  /// attributes to process all namespace declarations, making
  /// <em>ContentHandler.startPrefixMapping()</em> callbacks.
  /// Then a second pass is made, to determine the namespace-qualified
  /// names for all attributes and for the element name.
  /// Finally all the information for the
  /// <em>ContentHandler.startElement()</em> callback is available,
  /// so it can then be made.</p>
  ///
  /// <p>The Namespace support object always starts with a base context
  /// already in force: in this context, only the "xml" prefix is
  /// declared.</p>
  /// <see cref="PopContext"/> 
  /// </summary>
  public void PushContext ()
  {
    int max = contexts.Length;

    contexts [contextPos].declsOK = false;
    contextPos++;

    // Extend the array if necessary
    if (contextPos >= max) 
    {
      Context[] newContexts = new Context[max*2];
      System.Array.Copy(contexts, 0, newContexts, 0, max);
      max *= 2;
      contexts = newContexts;
    }

    // Allocate the context if necessary.
    currentContext = contexts[contextPos];
    if (currentContext == null) 
    {
      contexts[contextPos] = currentContext = new Context(namespaceDeclUris);
    }

    // Set the parent, if any.
    if (contextPos > 0) 
    {
      currentContext.SetParent(contexts[contextPos - 1]);
    }
  }

  /// <summary>
  /// Revert to the previous Namespace context.
  ///
  /// <p>Normally, you should pop the context at the end of each
  /// XML element.  After popping the context, all Namespace prefix
  /// mappings that were previously in force are restored.</p>
  ///
  /// <p>You must not attempt to declare additional Namespace
  /// prefixes after popping a context, unless you push another
  /// context first.</p>
  /// <see cref="PushContext"/>
  /// </summary>
  public void PopContext ()
  {
    contexts[contextPos].Clear();
    contextPos--;
    if (contextPos < 0) 
    {
      throw new InvalidOperationException();
    }
    currentContext = contexts[contextPos];
  }

  ////////////////////////////////////////////////////////////////////
  // Operations within a context.
  ////////////////////////////////////////////////////////////////////
    
  /// <summary>
  /// Declare a Namespace prefix.  All prefixes must be declared
  /// before they are referenced.  For example, a SAX driver (parser)
  /// would scan an element's attributes
  /// in two passes:  first for namespace declarations,
  /// then a second pass using <see cref="ProcessName"/> to
  /// interpret prefixes against (potentially redefined) prefixes.
  ///
  /// <p>This method declares a prefix in the current Namespace
  /// context; the prefix will remain in force until this context
  /// is popped, unless it is shadowed in a descendant context.</p>
  ///
  /// <p>To declare the default element Namespace, use the empty string as
  /// the prefix.</p>
  ///
  /// <p>Note that you must <em>not</em> declare a prefix after
  /// you've pushed and popped another Namespace context, or
  /// treated the declarations phase as complete by processing
  /// a prefixed name.</p>
  ///
  /// <p>Note that there is an asymmetry in this library: 
  /// <see cref="GetPrefix"/> will not return the "" prefix,
  /// even if you have declared a default element namespace.
  /// To check for a default namespace,
  /// you have to look it up explicitly using <see cref="GetURI"/>.
  /// This asymmetry exists to make it easier to look up prefixes
  /// for attribute names, where the default prefix is not allowed.</p>
  /// <see cref="ProcessName"/>
  /// <see cref="GetURI"/>
  /// <see cref="GetPrefix"/>
  /// </summary>
  /// <param name="prefix">The prefix to declare, or the empty string to
  ///	indicate the default element namespace.  This may never have
  ///	the value "xml" or "xmlns".</param>
  /// <param name="uri">The Namespace URI to associate with the prefix.</param>
  /// <returns>true if the prefix was legal, false otherwise</returns>
  public bool DeclarePrefix (string prefix, string uri)
  {
    if (prefix == "xml" || prefix == "xmlns") 
    {
      return false;
    } 
    else 
    {
      currentContext.DeclarePrefix(prefix, uri);
      return true;
    }
  }

  /// <summary>
  /// Process a raw XML qualified name, after all declarations in the
  /// current context have been handled by <see cref="DeclarePrefix"/>.
  ///
  /// <p>This method processes a raw XML qualified name in the
  /// current context by removing the prefix and looking it up among
  /// the prefixes currently declared.  The return value will be the
  /// array supplied by the caller, filled in as follows:</p>
  ///
  /// <dl>
  /// <dt>parts[0]</dt>
  /// <dd>The Namespace URI, or an empty string if none is
  ///  in use.</dd>
  /// <dt>parts[1]</dt>
  /// <dd>The local name (without prefix).</dd>
  /// <dt>parts[2]</dt>
  /// <dd>The original raw name.</dd>
  /// </dl>
  ///
  /// <p>All of the strings in the array will be internalized.  If
  /// the raw name has a prefix that has not been declared, then
  /// the return value will be null.</p>
  ///
  /// <p>Note that attribute names are processed differently than
  /// element names: an unprefixed element name will receive the
  /// default Namespace (if any), while an unprefixed attribute name
  /// will not.</p>
  /// </summary>
  /// <param name="qName">The XML qualified name to be processed.</param>
  /// <param name="parts">An array supplied by the caller, capable of
  ///        holding at least three members.</param>
  /// <param name="isAttribute">A flag indicating whether this is an
  ///        attribute name (true) or an element name (false).</param>
  /// <returns>The supplied array holding three internalized strings 
  ///        representing the Namespace URI (or empty string), the
  ///        local name, and the XML qualified name; or null if there
  ///        is an undeclared prefix.</returns>
  public string[] ProcessName (string qName, string[] parts,
    bool isAttribute)
  {
    string[] myParts = currentContext.ProcessName(qName, isAttribute);
    if (myParts == null) 
    {
      return null;
    } 
    else 
    {
      parts[0] = myParts[0];
      parts[1] = myParts[1];
      parts[2] = myParts[2];
      return parts;
    }
  }


  /// <summary>
  /// Look up a prefix and get the currently-mapped Namespace URI.
  ///
  /// <p>This method looks up the prefix in the current context.
  /// Use the empty string ("") for the default Namespace.</p>
  ///
  /// <see cref="GetPrefix"/>
  /// <see cref="GetPrefixes"/>
  /// </summary>
  /// <param name="prefix">The prefix to look up.</param>
  /// <returns>The associated Namespace URI, or null if the prefix
  ///         is undeclared in this context.</returns>
  public string GetURI (string prefix)
  {
    return currentContext.GetURI(prefix);
  }

    
  /// <summary>
  /// Return an enumeration of all prefixes whose declarations are
  /// active in the current context.
  /// This includes declarations from parent contexts that have
  /// not been overridden.
  ///
  /// <p><strong>Note:</strong> if there is a default prefix, it will not be
  /// returned in this enumeration; check for the default prefix
  /// using the <see cref="GetURI"/> with an argument of "".</p>
  ///
  /// @return 
  /// <see cref="GetDeclaredPrefixes"/>
  /// <see cref="GetURI"/>
  /// </summary>
  /// <returns>An enumeration of prefixes (never empty).</returns>
  public ArrayList GetPrefixes ()
  {
    return currentContext.GetPrefixes();
  }
  
  /// <summary>
  /// Return one of the prefixes mapped to a Namespace URI.
  ///
  /// <p>If more than one prefix is currently mapped to the same
  /// URI, this method will make an arbitrary selection; if you
  /// want all of the prefixes, use the <see cref="GetPrefixes"/>
  /// method instead.</p>
  ///
  /// <p><strong>Note:</strong> this will never return the empty (default) prefix;
  /// to check for a default prefix, use the <see cref="GetURI"/>
  /// method with an argument of "".</p>    
  /// <see cref="GetPrefixes(string)"/>
  /// <see cref="GetURI"/>
  /// </summary>
  /// <param name="uri">the namespace URI</param>
  /// <returns>One of the prefixes currently mapped to the URI supplied,
  ///         or null if none is mapped or if the URI is assigned to
  ///         the default namespace</returns>
  public string GetPrefix (string uri)
  {
    return currentContext.GetPrefix(uri);
  }
  
  /// <summary>
  /// Return an enumeration of all prefixes for a given URI whose
  /// declarations are active in the current context.
  /// This includes declarations from parent contexts that have
  /// not been overridden.
  ///
  /// <p>This method returns prefixes mapped to a specific Namespace
  /// URI.  The xml: prefix will be included.  If you want only one
  /// prefix that's mapped to the Namespace URI, and you don't care 
  /// which one you get, use the <see cref="GetPrefix"/>
  ///  method instead.</p>
  ///
  /// <p><strong>Note:</strong> the empty (default) prefix is <em>never</em> included
  /// in this enumeration; to check for the presence of a default
  /// Namespace, use the <see cref="GetURI"/> method with an
  /// argument of "".</p>
  ///
  /// <see cref="GetPrefix"/>
  /// <see cref="GetDeclaredPrefixes"/>
  /// <see cref="GetURI"/>
  /// </summary>
  /// <param name="uri">The Namespace URI.</param>
  /// <returns>An enumeration of prefixes (never empty).</returns>
  public ArrayList GetPrefixes (string uri)
  {
    ArrayList prefixes = new ArrayList();
    ArrayList allPrefixes = GetPrefixes();
    foreach (string prefix in allPrefixes) 
    {
      if (uri == GetURI(prefix)) 
      {
        prefixes.Add(prefix);
      }
    }
    return prefixes;
  }
  
  /// <summary>
  /// Return an enumeration of all prefixes declared in this context.
  ///
  /// <p>The empty (default) prefix will be included in this 
  /// enumeration; note that this behaviour differs from that of
  /// <see cref="GetPrefix"/> and <see cref="GetPrefixes"/>.</p>
  ///
  /// <see cref="GetPrefixes"/>
  /// <see cref="GetURI"/>
  /// </summary>
  /// <returns>An enumeration of all prefixes declared in this
  ///         context.</returns>
  public ArrayList GetDeclaredPrefixes ()
  {
    return currentContext.GetDeclaredPrefixes();
  }

  /// <summary>
  /// Controls whether namespace declaration attributes are placed
  /// into the <see cref="NSDECL"/> namespace by <see cref="ProcessName"/>.
  /// This may only be changed before any contexts have been pushed.
  /// </summary>
  /// <remarks>Set tu <c>true</c> if namespace declaration attributes are placed
  /// into the <see cref="NSDECL"/> namespace.</remarks>
  /// <exception cref="InvalidOperationException">When attempting to set this
  ///	after any context has been pushed.</exception>
  public bool NamespaceDeclUris 
  {
    get 
    {
      return namespaceDeclUris;
    }
    set 
    {
      if (contextPos != 0)
        throw new InvalidOperationException ();
      if (value == namespaceDeclUris)
        return;
      namespaceDeclUris = value;
      if (value)
        currentContext.DeclarePrefix ("xmlns", NSDECL);
      else 
      {
        contexts[contextPos] = currentContext = new Context(namespaceDeclUris);
        currentContext.DeclarePrefix("xml", XMLNS);
      }
    }
  }
  
  ////////////////////////////////////////////////////////////////////
  // Internal state.
  ////////////////////////////////////////////////////////////////////

  private Context[] contexts;
  private Context currentContext;
  private int contextPos;
  private bool namespaceDeclUris;

  ////////////////////////////////////////////////////////////////////
  // Internal classes.
  ////////////////////////////////////////////////////////////////////
  
  /// <summary>
  /// Internal class for a single Namespace context.
  ///
  /// <p>This module caches and reuses Namespace contexts,
  /// so the number allocated
  /// will be equal to the element depth of the document, not to the total
  /// number of elements (i.e. 5-10 rather than tens of thousands).
  /// Also, data structures used to represent contexts are shared when
  /// possible (child contexts without declarations) to further reduce
  /// the amount of memory that's consumed.
  /// </p>
  /// </summary>
  class Context 
  {
          
    /// <summary>
    /// Create the root-level Namespace context.
    /// </summary>
    internal Context (bool nsDeclUris)
    {
      this.contextNamespaceDeclUris = nsDeclUris;
      CopyTables();
    }
	
    /// <summary>
    /// (Re)set the parent of this Namespace context.
    /// The context must either have been freshly constructed,
    /// or must have been cleared.
    /// </summary>
    /// <param name="parent">The parent Namespace context object.</param>
    internal void SetParent (Context parent)
    {
      this.parent = parent;
      declarations = null;
      prefixTable = parent.prefixTable;
      uriTable = parent.uriTable;
      elementNameTable = parent.elementNameTable;
      attributeNameTable = parent.attributeNameTable;
      defaultNS = parent.defaultNS;
      declSeen = false;
      declsOK = true;
    }

    /// <summary>
    /// Makes associated state become collectible,
    /// invalidating this context.
    /// <see cref="SetParent"/> must be called before
    /// this context may be used again.
    /// </summary>
    internal void Clear ()
    {
      parent = null;
      prefixTable = null;
      uriTable = null;
      elementNameTable = null;
      attributeNameTable = null;
      defaultNS = string.Empty;
    }
		       
    /// <summary>
    /// Declare a Namespace prefix for this context.
    /// <see cref="NamespaceSupport.DeclarePrefix"/>
    /// </summary>
    /// <param name="prefix">The prefix to declare.</param>
    /// <param name="uri">The associated Namespace URI.</param>
    internal void DeclarePrefix (string prefix, string uri)
    {
      // Lazy processing...
      if (!declsOK)
        throw new InvalidOperationException ("Can't declare any more prefixes in this context");
      if (!declSeen) 
      {
        CopyTables();
      }
      if (declarations == null) 
      {
        declarations = new ArrayList();
      }
	    
      prefix = string.Intern(prefix);
      uri = string.Intern(uri);
      if (prefix == string.Empty) 
      {
        if (uri == string.Empty) 
        {
          defaultNS = string.Empty;
        } 
        else 
        {
          defaultNS = uri;
        }
      } 
      else 
      {
        prefixTable[prefix] = uri;
        uriTable[uri] = prefix; // may wipe out another prefix
      }
      declarations.Add(prefix);
    }
      
    /// <summary>
    /// Process an XML qualified name in this context.
    /// </summary>
    /// <param name="qName">The XML qualified name.</param>
    /// <param name="isAttribute">true if this is an attribute name.</param>
    /// <returns>An array of three strings containing the
    ///         URI part (or empty string), the local part,
    ///         and the raw name, all internalized, or null
    ///         if there is an undeclared prefix.</returns>
    internal string[] ProcessName (string qName, bool isAttribute)
    {
      string[] name;
      Hashtable table;
	    
      // detect errors in call sequence
      declsOK = false;

      // Select the appropriate table.
      if (isAttribute) 
      {
        table = attributeNameTable;
      } 
      else 
      {
        table = elementNameTable;
      }
	    
      // Start by looking in the cache, and
      // return immediately if the name
      // is already known in this content
      name = (string[])table[qName];
      if (name != null) 
      {
        return name;
      }
	    
      // We haven't seen this name in this
      // context before.  Maybe in the parent
      // context, but we can't assume prefix
      // bindings are the same.
      name = new string[3];
      name[2] = string.Intern(qName);
      int index = qName.IndexOf(':');
	    
	    
      // No prefix.
      if (index == -1) 
      {
        if (isAttribute) 
        {
          if (qName == "xmlns" && contextNamespaceDeclUris)
            name[0] = NSDECL;
          else
            name[0] = "";
        } 
        else 
        {
          name[0] = defaultNS;
        }
        name[1] = name[2];
      }
	    else 
      {
        // Prefix
        string prefix = qName.Substring(0, index);
        string local = qName.Substring(index+1);
        string uri;
        if (prefix == string.Empty) 
        {
          uri = defaultNS;
        } 
        else 
        {
          uri = (string)prefixTable[prefix];
        }
        if (uri == null || (!isAttribute && "xmlns" == prefix)) 
        {
          return null;
        }
        name[0] = uri;
        name[1] = string.Intern(local);
      }
	    
      // Save in the cache for future use.
      // (Could be shared with parent context...)
      table[name[2]] = name;
      return name;
    }
	        
    /// <summary>
    /// Look up the URI associated with a prefix in this context.
    /// <see cref="NamespaceSupport.GetURI"/>
    /// </summary>
    /// <param name="prefix">The prefix to look up.</param>
    /// <returns>The associated Namespace URI, or null if none is
    ///         declared.</returns>
    internal string GetURI (string prefix)
    {
      if (prefix == string.Empty) 
      {
        return defaultNS;
      } 
      else if (prefixTable == null) 
      {
        return null;
      } 
      else 
      {
        return (string)prefixTable[prefix];
      }
    }        

    /// <summary>
    /// Look up one of the prefixes associated with a URI in this context.
    ///
    /// <p>Since many prefixes may be mapped to the same URI,
    /// the return value may be unreliable.</p>
    /// <see cref="NamespaceSupport.GetPrefix"/>
    /// </summary>
    /// <param name="uri">The URI to look up.</param>
    /// <returns>The associated prefix, or null if none is declared.</returns>
    internal string GetPrefix (string uri)
    {
      if (uriTable == null) 
      {
        return null;
      } 
      else 
      {
        return (string)uriTable[uri];
      }
    }
	
          
    /// <summary>
    /// Return an enumeration of prefixes declared in this context.
    /// <see cref="NamespaceSupport.GetDeclaredPrefixes"/>
    /// </summary>
    /// <returns>An enumeration of prefixes (possibly empty).</returns>
    internal ArrayList GetDeclaredPrefixes ()
    {
      if (declarations == null) 
      {
        return EMPTY_ARRAYLIST;
      } 
      else 
      {
        return declarations;
      }
    }
	
          
    /// <summary>
    /// Return an enumeration of all prefixes currently in force.
    ///
    /// <p>The default prefix, if in force, is <em>not</em>
    /// returned, and will have to be checked for separately.</p>
    /// <see cref="NamespaceSupport.GetPrefixes"/>
    /// </summary>
    /// <returns>An enumeration of prefixes (never empty).</returns>
    internal ArrayList GetPrefixes ()
    {
      if (prefixTable == null) 
      {
        return EMPTY_ARRAYLIST;
      } 
      else 
      {
        return new ArrayList(prefixTable.Keys);
      }
    }
	
    ////////////////////////////////////////////////////////////////
    // Internal methods.
    ////////////////////////////////////////////////////////////////       

    /// <summary>
    /// Copy on write for the internal tables in this context.
    ///
    /// <p>This class is optimized for the normal case where most
    /// elements do not contain Namespace declarations.</p>
    /// </summary>
    private void CopyTables ()
    {
      if (prefixTable != null) 
      {
        prefixTable = (Hashtable)prefixTable.Clone();
      } 
      else 
      {
        prefixTable = new Hashtable();
      }
      if (uriTable != null) 
      {
        uriTable = (Hashtable)uriTable.Clone();
      } 
      else 
      {
        uriTable = new Hashtable();
      }
      elementNameTable = new Hashtable();
      attributeNameTable = new Hashtable();
      declSeen = true;
    }

    ////////////////////////////////////////////////////////////////
    // Protected state.
    ////////////////////////////////////////////////////////////////
	
    internal Hashtable prefixTable;
    internal Hashtable uriTable;
    internal Hashtable elementNameTable;
    internal Hashtable attributeNameTable;
    internal string defaultNS = string.Empty;
    internal bool declsOK = true;
	
    ////////////////////////////////////////////////////////////////
    // Internal state.
    ////////////////////////////////////////////////////////////////
	
    private ArrayList declarations = null;
    private bool declSeen = false;
    private Context parent = null;
    private bool contextNamespaceDeclUris = false;
  }
}


