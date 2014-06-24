// NO WARRANTY!  This code is in the Public Domain.
// Written by Karl Waclawek (karl@waclawek.net)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Org.System.Xml.Namespaces
{
  /**<summary>Defines constants for the <see cref="Org.System.Xml.Namespaces"/> namespace.</summary> */
  public class Constants
  {
    private Constants(){}

    /// <summary>Prefix for namespace declarations.</summary>
    public const string XmlNs = "xmlns";
    /// <summary>Namespace URI for reserved prefix 'xml'.</summary>
    public const string XmlUri = "http://www.w3.org/XML/1998/namespace";
    /// <summary>Namespace URI for reserved prefix 'xmlns'.</summary>
    public const string XmlNsUri = "http://www.w3.org/2000/xmlns/";
  }

  /**<summary>Exception class specific to namespace processing.</summary> */
  public class XmlNamespacesException: ApplicationException
  {
    public XmlNamespacesException() {}

    public XmlNamespacesException(string message): base(message) {}

    public XmlNamespacesException(string message, Exception e): base(message, e) {}
  }

  /**<summary>Establishes a mapping between a prefix and an URI.</summary>
   * <remarks>Can be linked to other mappings in a list or stack.</remarks>
   */
  public class NamespaceMapping
  {
    internal ActiveMapping prefix;
    internal NamespaceScope scope;
    internal string uri;
    internal bool declared;
    internal NamespaceMapping next;  // for linking to other instances

    /* Public Interface */

    /// <summary><see cref="ActiveMapping"/> instance for the prefix of this mapping.</summary>
    public ActiveMapping Prefix
    {
      get { return prefix; }
    }

    /// <summary>Namespace scope in which this mapping was established.</summary>
    public NamespaceScope Scope
    {
      get { return scope; }
    }

    /// <summary>URI of this mapping.</summary>
    public string Uri
    {
      get { return uri; }
    }

    /// <summary>Returns if this mapping is in a "declared" or "undeclared" state.</summary>
    public bool Declared
    {
      get { return declared; }
    }
  }

  /**<summary>Exposes the "active" (most recently added) mapping for a prefix.</summary>
   * <remarks>The active mapping for a prefix can also indicate that the prefix
   * is currently "undeclared".</remarks>
   */
  public class ActiveMapping
  {
    private string prefix;
    private XmlNamespaces namespaces;

    internal NamespaceMapping mappingsTop;
    internal ActiveMapping next;

    /// <summary>Top of stack of <see cref="NamespaceMapping"/> instances.
    /// Denotes the most recently added, that is, "current" instance.</summary>
    protected NamespaceMapping MappingsTop
    {
      get { return mappingsTop; }
    }

    /// <summary>Initializes <see cref="ActiveMapping"/> instance for re-use
    /// with a different prefix.</summary>
    /// <param name="prefix">Prefix to be associated with this instance.</param>
    protected internal void Init(string prefix)
    {
      this.prefix = prefix;
      next = null;
    }

    /// <summary>Prepares <see cref="ActiveMapping"/> instance for re-use, 
    /// clearing stack.</summary>
    protected internal void Reset()
    {
      if (mappingsTop != null) {
        namespaces.ReturnNSMappingList(mappingsTop);
        mappingsTop = null;
      }
    }

    /// <summary>Establishes a new mapping for the prefix associated with this 
    /// instance, to an URI in a namespace scope, and pushes it on the stack.</summary>
    /// <param name="scope">Namespace scope the new mapping will be part of.</param>
    /// <param name="uri">URI to be mapped to this instance's prefix.</param>
    /// <returns>Newly added <see cref="NamespaceMapping"/>.</returns>
    protected internal NamespaceMapping PushMapping(string uri, NamespaceScope scope)
    {
      NamespaceMapping result = namespaces.NewNSMapping();
      result.prefix = this;
      result.scope = scope;
      result.declared = uri != null;
      if (result.declared)
        result.uri = uri;
      else
        result.uri = this.Uri;
      result.next = mappingsTop;
      mappingsTop = result;
      return result;
    }

    /// <summary>Discards most recently added <see cref="NamespaceMapping"/>,
    /// popping it from stack and returning it to the associated 
    /// <see cref="XmlNamespaces"/> instance for re-use.</summary>
    protected internal void PopMapping()
    {
      NamespaceMapping nsMapping = mappingsTop;
      // protect against extra calls
      if (nsMapping == null) return;
      mappingsTop = nsMapping.next;
      namespaces.ReturnNSMapping(nsMapping);
      nsMapping.uri = null;  // release uri
    }

    /* Public Interface */

    /// <summary>Initializes new instance.</summary>
    /// <param name="prefix">Prefix to be associated with this instance.</param>
    /// <param name="namespaces"><see cref="XmlNamespaces"/> object which owns this instance.</param>
    public ActiveMapping(string prefix, XmlNamespaces namespaces)
    {
      if (namespaces == null)
        throw new ArgumentNullException("namespaces");
      this.prefix = prefix;
      this.namespaces = namespaces;
    }

    /// <summary>The prefix for this instance.</summary>
    public string Prefix
    {
      get { return prefix; }
    }

    /// <summary><see cref="XmlNamespaces"/> object which controls
    /// the life-cycle of this instance.</summary>
    public XmlNamespaces Namespaces
    {
      get { return namespaces; }
    }

    /// <summary>Links this instance to next in list.</summary>
    public ActiveMapping Next
    {
      get { return next; }
    }

    /// <summary>The URI of the "current" (most recently added)
    /// <see cref="NamespaceMapping"/>, or <c>null</c> if there is no mapping.</summary>
    /// <remarks>A namespace URI must not be an empty string.</remarks>
    public string Uri
    {
      get {
        if (mappingsTop != null)
          return mappingsTop.uri;
        else
          return null;
      }
    }

    /// <summary>Indicates the "declared" state of the "current" 
    /// (most recently added) <see cref="NamespaceMapping"/>.</summary>
    /// <remarks>Is <c>true</c> if the the namespace mapping is "declared",
    /// or <c>false</c> if it is "undeclared" or if there is no mapping.</remarks>
    public bool IsDeclared
    {
      get {
        if (mappingsTop != null)
          return mappingsTop.declared;
        else
          return false;
      }
    }
  }

  /**<summary>Represents a set of namespace prefix mappings that
   * are established and released together.</summary>
   * <remarks>
   * <list type="bullet">
   *   <item>Namespace scopes are established when a start element tag
   *     is processed while parsing or writing an XML document, and they
   *     are released when the matching end element tag is encountered.
   *   </item>
   *   <item>One can iterate over all mappings added to a 
   *     <see cref="NamespaceScope"/> by walking the linked list of active
   *     mappings exposed through the <see cref="Mappings"/> property.
   *     Follow <see cref="ActiveMapping.Next"/> until it returns null.
   *   </item>
   * </list>
   * </remarks>
   */
  public class NamespaceScope
  {
    private XmlNamespaces namespaces;
    private ActiveMapping mappings;

    internal NamespaceScope parent;
    internal Stack<NamespaceMapping> nsMappings;
    internal int emptyLevels;

    /// <summary>Stack of namespace prefix mappings added to this scope.</summary>
    protected Stack<NamespaceMapping> NSMappings
    {
      get { return nsMappings; }
    }

    /// <summary> Indicates how many empty scopes are "open" after this one.</summary>
    /// <remarks>This helps <see cref="XmlNamespaces"/> avoid pushing empty
    /// <see cref="NamespaceScope"/> instances on its stack.</remarks>
    protected int EmptyLevels
    {
      get { return emptyLevels; }
    }

    /// <summary> Adds a namespace mapping to this scope.</summary>
    /// <remarks>To undeclare the namespace mapping for a given prefix,
    /// pass <c>null</c> as <c>uri</c> argument.</remarks>
    /// <param name="prefix">Prefix part of namespace mapping.</param>
    /// <param name="uri">URI part of namespace mapping.</param>
    /// <returns>The newly added <see cref="NamespaceMapping"/> instance, or <c>null</c>
    /// if it already exists.</returns>
    protected internal NamespaceMapping AddMapping(string prefix, string uri)
    {
      // find prefix mapping in use
      ActiveMapping activeMapping = namespaces.GetPrefixMapping(prefix);
      // if not in use, re-use existing instance if possible
      if (activeMapping == null) {
        activeMapping = namespaces.NewActiveMapping(prefix);
        // since we have acquired an unused ActiveMapping instance we need
        // to keep track of it so that we can give it back later for re-use
        activeMapping.next = mappings;
        mappings = activeMapping;
      }
      else if (InScope(activeMapping))  // duplicate found
        return null;
      NamespaceMapping result = activeMapping.PushMapping(uri, this);
      nsMappings.Push(result);
      return result;
    }

    /// <overloads>
    /// <summary>Finds the namespace mapping for a given prefix in this scope.</summary>
    /// <returns>see cref="NamespaceMapping"/> to look for, or <c>null</c>
    /// if none exists.</returns>
    /// </overloads>
    /// <param name="prefix">Prefix to find mapping for.</param>
    protected internal NamespaceMapping FindByPrefix(string prefix)
    {
      NamespaceMapping result;
      Stack<NamespaceMapping>.Enumerator iter = nsMappings.GetEnumerator();
      while (iter.MoveNext()) {
        result = iter.Current;
        if (result.prefix.Prefix == prefix)
          return result;
      }
      return null;
    }

    /// <param name="prefixStr">String containing prefix sub-string.</param>
    /// <param name="start">Start index of prefix sub-string.</param>
    /// <param name="len">Length of prefix sub-string.</param>
    protected internal
    NamespaceMapping FindByPrefix(string prefixStr, int start, int len)
    {
      NamespaceMapping result;
      Stack<NamespaceMapping>.Enumerator iter = nsMappings.GetEnumerator();
      while (iter.MoveNext()) {
        result = iter.Current;
        string prefix = result.prefix.Prefix;
        if (prefix == null && len == 0)
          return result;
        if (prefix.Length == len &&
            String.CompareOrdinal(prefix, 0, prefixStr, start, len) == 0)
          return result;
      }
      return null;
    }

    /// <summary>Finds the namespace mapping that was added last in this
    /// scope for a given namespace URI.</summary>
    /// <param name="uri">Namespace URI to find mapping for.</param>
    /// <returns><see cref="NamespaceMapping"/> to look for, or <c>null</c>
    /// if none exists.</returns>
    protected internal NamespaceMapping FindByUri(string uri)
    {
      NamespaceMapping result;
      Stack<NamespaceMapping>.Enumerator iter = nsMappings.GetEnumerator();
      while (iter.MoveNext()) {
        result = iter.Current;
        if (result.uri == uri)
          return result;
      }
      return null;
    }

    /// <summary>Releases all prefix mappings and returns them to the
    /// associated <see cref="XmlNamespaces"/> object for re-use.</summary>
    /// <remarks>Must not be called unless this instance is the most recent scope,
    /// that is, is the last scope in which <see cref="AddMapping"/> was called, 
    /// otherwise the state of affected <see cref="ActiveMapping"/> instances
    /// will get corrupted.</remarks>
    protected internal void ClearMappings()
    {
      Debug.Assert(namespaces.IsLastScope(this), Resources.GetString(RsId.InternalNsError));
      NamespaceMapping nsMapping;
      ActiveMapping activeMapping;
      // returns mapping records back to Namespaces for re-use
      while (nsMappings.Count != 0) {
        nsMapping = (NamespaceMapping)nsMappings.Pop();
        Debug.Assert(nsMapping != null, Resources.GetString(RsId.InternalNsError));
        nsMapping.prefix.PopMapping();
      }
      // returns ActiveMapping objects back for re-use
      while ((activeMapping = mappings) != null) {
        mappings = activeMapping.next;
        activeMapping.Reset();
        namespaces.ReturnActiveMapping(activeMapping);
      }
    }

    /* Public Interface */

    /// <summary>Initializes new instance.</summary>
    /// <param name="namespaces"><see cref="XmlNamespaces"/> object which owns this instance.</param>
    public NamespaceScope(XmlNamespaces namespaces)
    {
      if (namespaces == null)
        throw new ArgumentNullException("namespaces");
      this.namespaces = namespaces;
      nsMappings = new Stack<NamespaceMapping>(4);
    }

    /// <summary><see cref="XmlNamespaces"/> object (owner) which controls
    /// the life-cycle of this instance.</summary>
    public XmlNamespaces Namespaces
    {
      get { return namespaces; }
    }

    /// <summary>Instance associated with parent element.</summary>
    public NamespaceScope Parent
    {
      get { return parent; }
    }

    /// <summary>First element (head) in list of mappings created in this scope.</summary>
    /// <remarks>This list can be walked by following the <see cref="ActiveMapping.Next"/>
    /// property until it returns <c>null</c>.</remarks>
    public ActiveMapping Mappings
    {
      get { return mappings; }
    }

    /// <summary> Indicates if a mapping for a given prefix has been added
    /// to this scope, even if only to "undeclare" the prefix.</summary>
    /// <param name="prefix"><see cref="ActiveMapping"/> instance associated with prefix.</param>
    /// <returns><c>true</c> if mapping has ben added, <c>false</c> otherwise.</returns>
    public bool InScope(ActiveMapping prefix)
    {
      NamespaceMapping nsMapping;
      Stack<NamespaceMapping>.Enumerator iter = nsMappings.GetEnumerator();
      while (iter.MoveNext()) {
        nsMapping = iter.Current;
        if (nsMapping.prefix == prefix)
          return true;
      }
      return false;
    }
  }

  /**<summary>Manages namespace prefix mappings for an XML document.</summary>
   * <remarks>
   *   <list type="bullet">
   *     <item>A new namespace scope is activated by calling <see cref="PushScope"/>.
   *       When parsing, this should be done when a start element tag is encountered,
   *       but after the namespace declarations for this start tag have been processed.
   *       When writing, this should be done just before the start element tag is
   *       written out.
   *     </item>
   *     <item>All prefix mappings must have been added (using <see cref="AddMapping"/>)
   *       to the new namespace scope before it is activated, or in other words,
   *       <see cref="AddMapping"/> calls apply to the <b>next</b> scope to be activated.
   *       When parsing, these calls should be made when namespace declarations
   *       attributes are processed. When writing, these calls should be made before
   *       <see cref="PushScope"/> is called for the next element's start tag.
   *     </item>
   *     <item>To leave (deactivate) a namespace scope one must call
   *       <see cref="PopScope"/>, which releases all namespace prefix
   *       mappings established in that scope. When parsing, this should be
   *       done right after an end element tag was encountered. When writing,
   *       this should be done right after the end element tag was written out.
   *     </item>
   *     <item>This class is designed to use <see cref="PushScope"/> and
   *       <see cref="PopScope"/> at every level of element nesting - even when
   *       no new prefix mappings are declared. This way it can keep track of
   *       empty namespace scopes and does not require the programmer to match
   *       <see cref="PopScope"/> calls to <see cref="PushScope"/> calls at the
   *       right level.
   *     </item>
   *     <item>To declare prefix mappings for the default namespace just use
   *       a null prefix, for example: <c>AddMapping(null, "http://myuri")</c>.
   *     </item>
   *     <item>To "undeclare" a prefix mapping just add a mapping with a
   *       <c>null</c> URI: <c>AddMapping("myprefix", null)</c>. This works for
   *       default and regular namespaces, although the latter is *only* legal
   *       in the Namespaces for XML 1.1 specification.
   *     </item>
   *     <item>To re-use an instance for another XML document one must
   *       call <see cref="Reset"/> before calling any other methods.
   *     </item>
   *   </list>
   * </remarks>
   */
  public class XmlNamespaces
  {
    // stack of re-usable NamespaceMapping instances
    private NamespaceMapping freeNSMappings;
    // stack of re-usable ActiveMapping instances
    private ActiveMapping freeActiveMappings;
    // stack of re-usable scopes; top of stack = next scope to be pushed
    private NamespaceScope nextScope;
    // most recently activated scope
    private NamespaceScope topScope;

    // Returns NamespaceMapping instance properly initialized for new use,
    // re-using old instance if possible.
    internal NamespaceMapping NewNSMapping()
    {
      if (freeNSMappings == null)
        return new NamespaceMapping();
      else {
        NamespaceMapping result = freeNSMappings;
        freeNSMappings = result.next;
        result.next = null;
        return result;
      }
    }

    // Accepts NamespaceMapping instance that is no longer needed, storing
    // it for re-use. Note: do not pass null argument - this is not checked.
    internal void ReturnNSMapping(NamespaceMapping mapping)
    {
      mapping.next = freeNSMappings;
      freeNSMappings = mapping;
    }

    // Accepts linked list of NamespaceMapping instances that is no longer needed,
    // storing them for re-use. Note: do not pass null argument - not checked.
    internal void ReturnNSMappingList(NamespaceMapping mapping)
    {
      NamespaceMapping lastMapping = mapping;
      // find last node
      while (lastMapping.next != null)
        lastMapping = lastMapping.next;
      lastMapping.next = freeNSMappings;
      freeNSMappings = mapping;
    }

    // Returns ActiveMapping instance properly initialized for new use,
    // re-using old instance if possible.
    internal ActiveMapping NewActiveMapping(string prefix)
    {
      if (freeActiveMappings == null)
        return new ActiveMapping(prefix, this);
      else {
        ActiveMapping result = freeActiveMappings;
        freeActiveMappings = result.next;
        result.Init(prefix);
        return result;
      }
    }

    // Accepts ActiveMapping instance that is no longer needed, storing it
    // for re-use. Note: do not pass null argument - this is not checked.
    internal void ReturnActiveMapping(ActiveMapping mapping)
    {
      mapping.next = freeActiveMappings;
      freeActiveMappings = mapping;
    }

    /// <summary>Indicates if a namespace scope is the most recent scope.</summary>
    /// <param name="scope"><see cref="NamespaceScope"/> to be checked.</param>
    /// <returns><c>true</c> if <c>scope</c> is most recent, <c>false</c> otherwise.</returns>
    protected internal bool IsLastScope(NamespaceScope scope)
    {
      return scope == topScope || scope == nextScope;
    }

    /* Public Interface */

    /// <summary>Initializes new instance.</summary>
    public XmlNamespaces()
    {
      nextScope = new NamespaceScope(this);
      // the 'xml' prefix is always declared
      nextScope.AddMapping("xml", Constants.XmlUri);
    }

    /// <overloads>
    /// <summary>Gets <see cref="ActiveMapping"/> instance for a given prefix.</summary>
    /// <returns><see cref="ActiveMapping"/> instance for <c>prefix</c>, or <c>null</c>
    /// if no such prefix mapping has been declared yet.</returns>
    /// </overloads>
    /// <param name="prefix">Prefix to find <see cref="ActiveMapping"/> instance for.</param>
    public ActiveMapping GetPrefixMapping(string prefix)
    {
      NamespaceScope scope = topScope;
      while (scope != null) {
        NamespaceMapping nsMapping = scope.FindByPrefix(prefix);
        if (nsMapping != null)
          return nsMapping.prefix;
        scope = scope.parent;
      }
      return null;
    }

    /// <param name="prefixStr">String containing prefix sub-string.</param>
    /// <param name="start">Start index of prefix sub-string.</param>
    /// <param name="len">Length of prefix sub-string.</param>
    public ActiveMapping GetPrefixMapping(string prefixStr, int start, int len)
    {
      NamespaceScope scope = topScope;
      while (scope != null) {
        NamespaceMapping nsMapping = scope.FindByPrefix(prefixStr, start, len);
        if (nsMapping != null)
          return nsMapping.prefix;
        scope = scope.parent;
      }
      return null;
    }

    /// <summary> Adds a prefix mapping to the next scope that will be activated.</summary>
    /// <remarks><list type="bullet">
    /// <item>The namespace URI must be a valid URI and must not be
    ///   an empty string. This is not checked.</item>
    /// <item>To undeclare a prefix mapping pass null as <c>uri</c>
    ///   argument.</item>
    /// <item>To declare a mapping for the default namespace, pass null
    ///   as <c>prefix</c> argument.</item>
    /// </list></remarks>
    /// <param name="prefix">Prefix part of namespace mapping.</param>
    /// <param name="uri">URI part of namespace mapping.</param>
    /// <returns>The new <see cref="ActiveMapping"/> instance, or <c>null</c>,
    /// if such a mapping has already been added.</returns>
    public ActiveMapping AddMapping(string prefix, string uri)
    {
      NamespaceMapping nsMapping = nextScope.AddMapping(prefix, uri);
      if (nsMapping == null)
        return null;
      else
        return nsMapping.prefix;
    }

    /// <summary>Activates new namespace scope.</summary>
    /// <remarks>All namespace mappings that were added since the last
    /// call to <see cref="PushScope"/>are going into effect.</remarks>
    public void PushScope()
    {
      if (nextScope.nsMappings.Count == 0) {
        nextScope.emptyLevels++;
      }
      else {
        NamespaceScope newNextScope = nextScope.parent;
        nextScope.parent = topScope;
        topScope = nextScope;  // assume it was reset
        if (newNextScope == null)
          newNextScope = new NamespaceScope(this);
        else
          newNextScope.emptyLevels = 0;
        nextScope = newNextScope;
      }
    }

    /// <summary>Deactivates namespace scope.</summary>
    /// <remarks>All namespace mappings that became active with this scope
    /// are going out of effect.</remarks>
    public void PopScope()
    {
      if (nextScope.emptyLevels > 0)
        nextScope.emptyLevels--;
      else if (topScope == null) {  // don't pop too many times
        string msg = Resources.GetString(RsId.NoActiveNsScope);
        throw new InvalidOperationException(msg);
      }
      else {
        NamespaceScope oldNextScope = nextScope;
        // clear pending declarations
        oldNextScope.ClearMappings();
        nextScope = topScope;
        topScope = topScope.parent;
        nextScope.parent = oldNextScope;
        nextScope.ClearMappings();
      }
    }

    /// <summary>Re-initializes internal state to be ready for re-use.</summary>
    public void Reset()
    {
      // some declarations may be pending in nextScope
      nextScope.ClearMappings();
      // store active scopes for re-use
      NamespaceScope scope = topScope;
      if (scope != null) {
        // need to reset active scopes before storing them for re-use
        scope.ClearMappings();
        while (scope.parent != null) {
          scope = scope.parent;
          scope.ClearMappings();
        }
        // attach free scopes to end of active scopes
        scope.parent = nextScope;
        // move the whole stack from topScope to nextScope
        nextScope = topScope;
        topScope = null;
      }
      // the 'xml' prefix is always declared
      nextScope.AddMapping("xml", Constants.XmlUri);
    }

    /// <summary>Last activated namespace scope.</summary>
    /// <remarks>Returns non-<c>null</c> only if the last <see cref="PushScope"/>
    /// did actually add new namespace mappings.</remarks>
    public NamespaceScope ActiveScope
    {
      get {
        if (nextScope.emptyLevels == 0)
          return topScope;
        else
          return null;
      }
    }

    /// <summary>Returns the most recently added namespace mapping "in effect"
    /// for a given URI.</summary>
    /// <remarks>"In effect" means that the prefix mapping must <b>not</b> have
    /// been "undeclared".</remarks>
    /// <param name="uri">Namespace URI to get mapping for.</param>
    /// <returns><see cref="ActiveMapping"/> instance for <c>uri</c>, or <c>null</c>
    /// if there is no such mapping.</returns>
    public ActiveMapping GetUriMapping(string uri)
    {
      NamespaceScope scope = topScope;
      while (scope != null) {
        NamespaceMapping nsMapping = scope.FindByUri(uri);
        if (nsMapping != null && nsMapping.declared)
          return nsMapping.prefix;
        scope = scope.parent;
      }
      return null;
    }

    /// <summary>Given a prefix mapping for a specific URI, this returns
    /// the prefix mapping previously in effect for the same URI.</summary>
    /// <remarks>This can be used to iterate backwards through the stack of
    /// prefix mappings for a given URI.</remarks>
    /// <param name="prefix"><see cref="ActiveMapping"/> instance for which
    /// we want to find the instance previously in effect with the same URI.</param>
    /// <returns><see cref="ActiveMapping"/> instance to look for,
    /// or <c>null</c> if none exists.</returns>
    public ActiveMapping PreviousUriMapping(ActiveMapping prefix)
    {
      string uri = prefix.Uri;
      // check active mapping record
      NamespaceMapping nsMapping = prefix.mappingsTop;
      if (nsMapping == null || !nsMapping.declared) {
        string msg = Resources.GetString(RsId.UndeclaredMapping);
        throw new XmlNamespacesException(String.Format(msg, prefix.Prefix));
      }
      // find a parent scope where the same URI maps to an "active" prefix
      NamespaceScope scope = nsMapping.scope.parent;
      while (scope != null) {
        nsMapping = scope.FindByUri(uri);
        if (nsMapping != null) {
          ActiveMapping result = nsMapping.prefix;
          // result must be different from prefix, since the same prefix
          // cannot be "declared" with two different URIs at the same time
          if (result.IsDeclared) {
            Debug.Assert(result != prefix, Resources.GetString(RsId.InternalNsError));
            return result;
          }
        }
        scope = scope.parent;
      }
      return null;
    }
  }

}

