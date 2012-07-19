/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections.Generic;
using Org.System.Xml.Sax;
using Org.System.Xml.Sax.Helpers;

namespace Kds.Xml.Sax.Helpers
{
  /**<summary>Implements the <see cref="IProperty&lt;T>"/> interface 
   * for the <see cref="IDefaultHandler"/> property type.</summary> 
   */
  public class DefaultHandlerProperty: IProperty<IDefaultHandler>
  {
    private IDefaultHandler handler;
    private OnPropertyChange<IDefaultHandler> onChange;

    public DefaultHandlerProperty(IDefaultHandler handler,
                                  OnPropertyChange<IDefaultHandler> onChange)
    {
      this.handler = handler;
      this.onChange = onChange;
    }

    public string Name
    {
      get { return Sax.Constants.DefaultHandlerProperty; }
    }

    public IDefaultHandler Value
    {
      get { return handler; }
      set {
        IDefaultHandler newValue = value;
        if (onChange != null) onChange(this, newValue);
        handler = newValue;
      }
    }
  }

  /**<summary>Base class for <see cref="AttDecl"/>. Serves as key for
   * looking up entries in the <see cref="AttributeDecls"/> container.</summary>
   */
  internal class AttDeclKey
  {
    public string ElmName;
    public string AttName;

    public AttDeclKey() {}

    public AttDeclKey(string elmName, string attName)
    {
      this.ElmName = elmName;
      this.AttName = attName;
    }

    // Precondition: ElmName != null, AttName != null
    public override int GetHashCode()
    {
      return ElmName.GetHashCode() ^ AttName.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj == null || GetType() != obj.GetType())
        return false;
      AttDeclKey key = (AttDeclKey)obj;
      return ElmName == key.ElmName && AttName == key.AttName;
    }
  }

  /**<summary>Represents attribute declaration.</summary>
   * <remarks>Used by <see cref="AttributeDecls"/>.</remarks>
   */
  internal class AttDecl: AttDeclKey
  {
    public string Type;
    public string Mode;
    public string Value;

    public AttDecl(string elmName,
                   string attName,
                   string type,
                   string mode,
                   string value): base(elmName, attName)
    {
      this.Type = type;
      this.Mode = mode;
      this.Value = value;
    }
  }

  /**<summary>Opaque reference to an entry in the attribute declarations table.</summary> */
  public struct AttDeclEntry
  {
    internal AttDecl Decl;

    /// <summary>Indicates if this instance refers to an existing table entry.</summary>
    public bool Exists
    {
      get { return Decl != null; }
    }
  }

  /**<summary>Searchable container for attribute declarations.</summary>
   * <remarks>Dictionary (Hash table) based. Useful for processing DTDs.</remarks>
   */
  public class AttributeDecls
  {
    private Dictionary<AttDeclKey, AttDecl> declTable;
    private AttDeclKey searchKey = new AttDeclKey();

    public AttributeDecls()
    {
      declTable = new Dictionary<AttDeclKey, AttDecl>();
    }

    private void CheckNames(string elmName, string attName)
    {
      string msg;
      if (elmName == null)
        msg = Constants.GetString(RsId.MissingElementName);
      else if (attName == null)
        msg = Constants.GetString(RsId.MissingAttributeName);
      else
        return;
      throw new ArgumentException(msg);
    }

    /// <summary>Adds an attribute declaration to <see cref="AttributeDecls"/>.</summary>
    /// <returns><c>true</c> if successful, <c>false</c> if <see cref="AttributeDecls"/>
    /// already contains an identical attribute declaration.</returns>
    public bool Add(string elmName,
                    string attName,
                    string attType,
                    string attMode,
                    string attValue)
    {
      CheckNames(elmName, attName);
      searchKey.ElmName = elmName;
      searchKey.AttName = attName;
      bool result = !declTable.ContainsKey(searchKey);
      if (result) {
        AttDecl decl = new AttDecl(elmName, attName, attType, attMode, attValue);
        declTable[searchKey] = decl;
      }
      return result;
    }

    /// <summary>Gets attribute declaration entry for attribute name.</summary>
    /// <returns>An <see cref="AttDeclEntry"/> instance reflecting the search result.</returns>
    public AttDeclEntry GetEntry(string elmName, string attName)
    {
      AttDeclEntry result;
      CheckNames(elmName, attName);
      searchKey.ElmName = elmName;
      searchKey.AttName = attName;
      result.Decl = declTable[searchKey];
      return result;
    }

    /// <summary>Removes all declarations.</summary>
    public void Clear()
    {
      declTable.Clear();
    }

    /// <summary>Retrieves attribute declaration values from entry.</summary>
    public void GetValue(AttDeclEntry entry,
                         out string attType,
                         out string attMode,
                         out string attValue)
    {
      attType = entry.Decl.Type;
      attMode = entry.Decl.Mode;
      attValue = entry.Decl.Value;
    }

    /// <summary>Retrieves attribute type from entry.</summary>
    public string GetAttType(AttDeclEntry entry)
    {
      return entry.Decl.Type;
    }

    /// <summary>Returns an <see cref="IEnumerator&lt;AttDeclEntry>"/> instance
    /// that can iterate through the attribute declarations.</summary>
    /// <remarks>Use the value of <see cref="IEnumerator&lt;T>.Current"/>
    /// as argument for <see cref="GetValue"/> or <see cref="GetAttType"/>.</remarks>
    public IEnumerator<AttDeclEntry> GetEnumerator()
    {
      Dictionary<AttDeclKey, AttDecl>.Enumerator attEnum = declTable.GetEnumerator();
      while (attEnum.MoveNext()) {
        AttDeclEntry current;
        current.Decl = attEnum.Current.Value;
        yield return current;
      }
    }
  }

  /**<summary>Base class for <see cref="ParEntDecl"/>. Serves as key for
   * looking up entries in the <see cref="ParameterEntityDecls"/> container.</summary>
   */
  internal class ParEntDeclKey
  {
    public string PublicId;
    public string SystemId;

    public ParEntDeclKey() {}

    public ParEntDeclKey(string publicId, string systemId)
    {
      this.PublicId = publicId;
      this.SystemId = systemId;
    }

    // Precondition: SystemId and PublicId are not both null at the same time
    public override int GetHashCode()
    {
      if (PublicId == null)
        return SystemId.GetHashCode();
      else if (SystemId == null)
        return PublicId.GetHashCode();
      else
        return PublicId.GetHashCode() ^ SystemId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj == null || GetType() != obj.GetType())
        return false;
      ParEntDeclKey key = (ParEntDeclKey)obj;
      return SystemId == key.SystemId && PublicId == key.PublicId;
    }
  }

  /**<summary>Represents parameter entity declaration.</summary>
   * <remarks>Used by <see cref="ParameterEntityDecls"/>
   * to store parameter entity declarations.</remarks>
   */
  internal class ParEntDecl: ParEntDeclKey
  {
    public string Name;

    public ParEntDecl(string publicId, string systemId, string name): 
      base(publicId, systemId)
    {
      this.Name = name;
    }
  }

  /**<summary>Opaque reference to an entry in the parameter entity declarations table.</summary> */
  public struct ParEntDeclEntry
  {
    internal ParEntDecl Decl;

    /// <summary>Indicates if this instance refers to an existing table entry.</summary>
    public bool Exists
    {
      get { return Decl != null; }
    }
  }

  /**<summary>Searchable container for parameter entity declarations.</summary>
   * <remarks>Dictionary (Hash table) based. Useful for processing DTDs.</remarks>
   */
  public class ParameterEntityDecls
  {
    private Dictionary<ParEntDeclKey, ParEntDecl> declTable;
    private ParEntDeclKey searchKey = new ParEntDeclKey();

    public ParameterEntityDecls()
    {
      declTable = new Dictionary<ParEntDeclKey, ParEntDecl>();
    }

    private void CheckExternalId(string publicId, string systemId)
    {
      if (publicId == null && systemId == null) {
        string msg = Constants.GetString(RsId.MissingExternalId);
        throw new ArgumentException(msg);
      }
    }

    /// <summary>Adds new parameter entity declaration.</summary>
    /// <returns><c>true</c> if successful, <c>false</c> if duplicate.</returns>
    public bool Add(string name, string publicId, string systemId)
    {
      CheckExternalId(publicId, systemId);
      searchKey.PublicId = publicId;
      searchKey.SystemId = systemId;
      bool result = !declTable.ContainsKey(searchKey);
      if (result) {
        ParEntDecl decl = new ParEntDecl(publicId, systemId, name);
        declTable[searchKey] = decl;
      }
      return result;
    }

    /// <summary>Gets entry for parameter entity declaration based on external id.</summary>
    /// <returns>A <see cref="ParEntDeclEntry"/> instance reflecting the search result.</returns>
    public ParEntDeclEntry GetEntry(string publicId, string systemId)
    {
      ParEntDeclEntry result;
      CheckExternalId(publicId, systemId);
      searchKey.PublicId = publicId;
      searchKey.SystemId = systemId;
      result.Decl = declTable[searchKey];
      return result;
    }

    /// <summary>Removes all declarations.</summary>
    public void Clear()
    {
      declTable.Clear();
    }

    /// <summary>Retrieves parameter entity name from entry.</summary>
    public string GetName(ParEntDeclEntry entry)
    {
      return entry.Decl.Name;
    }

    /// <summary>Returns an <see cref="IEnumerator&lt;ParEntDeclEntry>"/>
    /// instance that can iterate through the parameter entity declarations.</summary>
    /// <remarks>Use the value of <see cref="IEnumerator&lt;T>.Current"/>
    /// as argument for <see cref="GetName"/>.</remarks>
    public IEnumerator<ParEntDeclEntry> GetEnumerator()
    {
      Dictionary<ParEntDeclKey, ParEntDecl>.Enumerator parEnum = declTable.GetEnumerator();
      while (parEnum.MoveNext()) {
        ParEntDeclEntry current;
        current.Decl = parEnum.Current.Value;
        yield return current;
      }
    }
  }

}
