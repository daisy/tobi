/*
 * This software is licensed according to the "Modified BSD License",
 * where the following substitutions are made in the license template:
 * <OWNER> = Karl Waclawek
 * <ORGANIZATION> = Karl Waclawek
 * <YEAR> = 2004, 2005, 2006
 * It can be obtained from http://opensource.org/licenses/bsd-license.html.
 */

using System;
using System.Collections;
using System.Resources;
using System.Reflection;

namespace Kds.Text
{
  /**<summary>Utility class for string processing.</summary> */
  public class StringUtils
  {
    private StringUtils() {}

    /// <summary>Calculate length of null-terminated string.</summary>
    /// <remarks>Prerequisiste (unchecked!!!): str != null.</remarks>
    public static unsafe int StrLen(char* str)
    {
      int result = 0;
      while (*str != 0) {
        result++;
        str++;
      }
      return result;
    }

    /// <overloads>String hash function, based on Python's implementation.</overloads>
    public static unsafe int StrHash(string str, int initVal)
    {
      uint result = unchecked ((uint)initVal);
      if (str != null) {
        fixed (char* strPtr = str) {
          char* ch = strPtr;
          uint wc = *ch;
          // we use the fact that .NET strings are null terminated
          while (wc != 0) {
            result = unchecked((result * 0xF4243) ^ wc);
            ch++;
            wc = *ch;
          }
        }
      }
      return unchecked ((int)result);
    }

    public static unsafe int StrHash(char[] chars, int initVal)
    {
      uint result = unchecked ((uint)initVal);
      if (chars != null) {
        fixed (char* charsPtr = chars) {
          char* ch = charsPtr;
          char* chEnd = ch + chars.Length;
          while (ch != chEnd) {
            uint wc = *ch;
            result = unchecked((result * 0xF4243) ^ wc);
            ch++;
          }
        }
      }
      return unchecked ((int)result);
    }

    /// <remarks>Prerequisiste (unchecked!!!): len >= 0, start + len &lt;= chars.Length.</remarks>
    public static unsafe int StrHash(char[] chars, int start, int len, int initVal)
    {
      uint result = unchecked ((uint)initVal);
      if (chars != null) {
        fixed (char* charsPtr = &chars[start]) {
          char* ch = charsPtr;
          char* chEnd = ch + len;
          while (ch != chEnd) {
            uint wc = *ch;
            result = unchecked((result * 0xF4243) ^ wc);
            ch++;
          }
        }
      }
      return unchecked ((int)result);
    }

    /// <remarks>Prerequisiste (unchecked!!!): len >= 0, chars at least len characters long.</remarks>
    public static unsafe int StrHash(char* chars, int len, int initVal)
    {
      uint result = unchecked ((uint)initVal);
      if (chars != null) {
        char* chEnd = chars + len;
        while (chars != chEnd) {
          uint wc = *chars;
          result = unchecked((result * 0xF4243) ^ wc);
          chars++;
        }
      }
      return unchecked ((int)result);
    }

    /// <remarks>Prerequisite (unchecked!!!): chars is null-terminated.</remarks>
    public static unsafe int StrHash(char* chars, int initVal)
    {
      uint result = unchecked ((uint)initVal);
      if (chars != null) {
        uint wc = *chars;
        while (wc != 0) {
          result = unchecked((result * 0xF4243) ^ wc);
          chars++;
          wc = *chars;
        }
      }
      return unchecked ((int)result);
    }

  }

  /**<summary>String table for mapping unmanaged (interop) strings.</summary>
   * <remarks>Accepts .NET strings as well as character arrays and null-terminated
   * char* based strings. Can be used for mapping identical external (unmanaged)
   * strings to the same .NET string instance.
   * This is also called <b>string interning</b>.</remarks>
   */
  public unsafe class StringTable
  {
    private string[] items;
    private byte sizeLog;
    private bool autoShrink;
    private int count;
    private int usedCount;
    private static readonly string deleted = "deleted";

    /// <summary>Sets capacity of string hash table as power of 2.</summary>
    /// <param name="value">New size of hash table when raised to the power of 2.</param>
    protected void SetSizeLog(byte value)
    {
      int indx;
      string[] newItems;
      int newSize;
      uint mask;

      newSize = (int)1 << value;
      if ((value < 2) | (newSize < (count << 1))) {
        string msg = Resources.GetString(RsId.SizeTooSmall);
        throw new ArgumentException(String.Format(msg, value), "value");
      }

      mask = unchecked ((uint)(newSize - 1));
      newItems = new string[newSize];

      // re-hash: loop through existing items and insert them into new array
      for (indx = items.Length - 1; indx >= 0; indx--) {
        string str = items[indx];
        // (object) cast forces pointer comparison
        if ((str != null) && ((object)str != (object)deleted)) {
          int keyHash = StringUtils.StrHash(str, 0);
          int newIndx = unchecked (keyHash & (int)mask);
          // don't have to check for deleted items in newItems array
          if (newItems[newIndx] != null) unchecked {
            // see comments to step in FindSlot()
            int step = ((((keyHash & (int)~mask) >> (value - 1)) &
                         (int)(mask >> 2)) | (int)1) & (int)0xFF;
            do {
              newIndx = newIndx - step;
              if (newIndx < 0) newIndx = newIndx + newSize;
            } while (newItems[newIndx] != null);
          }
          newItems[newIndx] = str;
        }
      }
      items = newItems;
      sizeLog = value;
      usedCount = count;
    }

    /// <summary>Increments string count, increasing string table capacity as needed.</summary>
    protected void IncCount(bool emptySlot)
    {
      count++;
      if (count >= ((int)1 << (sizeLog - 1)))
        SetSizeLog((byte)(sizeLog + 1));
      else if (emptySlot)
        usedCount++;
    }

    /// <summary>
    /// Decrements item count, increments count of deleted slots, rebuilding hash table if needed.
    /// </summary>
    protected void DecCount() {
      count--;
      int limit = (int)1 << (sizeLog - 1);
      if (autoShrink && count < (limit >> 1) && sizeLog > 3)
        SetSizeLog((byte)(sizeLog - 1));
      else if (usedCount > limit)
        Rebuild();
    }

    /// <summary>Finds slot in string hash table that matches a given key.</summary>
    /// <remarks>This method always finds a slot. The return value differentiates
    /// between unused and occupied slots, making it possible to use it for both,
    /// insertions of new strings, and searches for existing ones.</remarks>
    /// <param name="key">Pointer to first character of key. 
    /// Must be != <c>null</c>, which is not checked.</param>
    /// <param name="len">Length of key. Must be >= 0, which is not checked.</param>
    /// <param name="indx">Index of matching slot.</param>
    /// <returns><c>true</c> if slot occupied, <c>false</c> if slot still unused.</returns>
    protected bool FindSlot(char* key, int len, out int indx)
    {
      int delIndx, startIndx;
      string str;
      int keyHash;
      uint mask;
      byte log;

      keyHash = StringUtils.StrHash(key, len, 0);
      log = sizeLog;
      mask = ((uint)1 << log) - 1;  // topmost bit is never set!
      indx = unchecked(keyHash & (int)mask);
      str = items[indx];

      // if slot is empty, then we can return right away
      if (str == null)
        return false;

      // otherwise, scan for an empty, deleted or duplicate slot
      delIndx = -1;
      startIndx = indx;
      /* For probing (after a collision) we need a step size relative prime
       * to the hash table size, which is a power of 2. We use double-hashing,
       * since we can calculate a second hash value cheaply by taking those bits
       * of the first hash value that were discarded (masked out) when the table
       * index was calculated: index = hash & mask, where mask = table-size-1.
       * The maximum step size should fit into a byte and be less than table-size/4.
       * It must be an odd number, since that is relative prime to a power of 2.
       */
      int step = unchecked (((((keyHash & (int)~mask) >> (log - 1)) &
                              (int)(mask >> 2)) | (int)1) & (int)0xFF);
      do {
        // (object) cast forces pointer comparison
        if ((object)str == (object)deleted) {
          // remember first deleted position
          if (delIndx < 0) delIndx = indx;
        }
        else if (str.Length == len) {
          // if duplicate found, return its position
          fixed (char* strPtr = str) {
            char* compCh = strPtr + len;
            char* keyCh = key + len;
            while (keyCh != key) {
              compCh--;
              keyCh--;
              if (*compCh != *keyCh) goto NoMatch;
            };
          }
          return true;
        }
      NoMatch:
        unchecked {
          indx = indx - step;
          if (indx < 0)
            indx = indx + (int)mask + 1;
          str = items[indx];
        }
      // due to forced rebuilds (see IncCount(), DecCount()) we will always have empty slots
      } while (str != null);

      if (delIndx >= 0) indx = delIndx;
      return false;
    }

    /// <summary>Internal array where strings are stored.</summary>
    protected string[] Items
    {
      get { return items; }
    }

    /// <summary>Initializes new <see cref="StringTable"/> instance.</summary>
    /// <param name="sizeLog">Base 2 logarithm of the initial capacity.</param>
    public StringTable(byte sizeLog)
    {
      items = new string[1];
      count = 0;
      usedCount = 0;
      autoShrink = true;
      SetSizeLog(sizeLog);
    }

    public StringTable(): this(6) {}

    /// <overloads>
    /// <summary>Interns a string.</summary>
    /// <remarks>The empty string is always interned.</remarks>
    /// <returns>Interned string instance, never <c>null</c>.</returns>
    /// </overloads>
    /// <param name="value">String to be interned.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value == null</c>.</exception>
    public string Intern(string value)
    {
      if (value == null)
        throw new ArgumentNullException("value");
      else if (value.Length == 0)
        return String.Empty;
      int indx;
      bool found;
      fixed (char* strPtr = value) {
        found = FindSlot(strPtr, value.Length, out indx);
      }
      if (found)
        return items[indx];
      else {
        object item = items[indx];
        items[indx] = value;
        IncCount(item == null);
        return value;
      }
    }

    /// <param name="value">Character array representing string to be interned.
    /// May be <c>null</c> or empty, which indicates the empty string.</param>
    public string Intern(char[] value)
    {
      if (value == null || value.Length == 0)
        return String.Empty;
      int indx;
      bool found;
      fixed (char* strPtr = value) {
        found = FindSlot(strPtr, value.Length, out indx);
      }
      if (found)
        return items[indx];
      else {
        string result = new string(value);
        object item = items[indx];
        items[indx] = result;
        IncCount(item == null);
        return result;
      }
    }

    private void IllegalLengthError(string paramName, int len)
    {
      string msg = Resources.GetString(RsId.IllegalLength);
      throw new ArgumentOutOfRangeException(paramName, String.Format(msg, len));
    }

    private void ArrayOutOfBoundsError(string paramName, int index)
    {
      string msg = Resources.GetString(RsId.ArrayOutOfBounds);
      throw new ArgumentOutOfRangeException(paramName, String.Format(msg, index));
    }

    /// <param name="value">Character array containing string to be interned.
    /// May be <c>null</c>, in which case <c>len</c> must be zero.</param>
    /// <param name="start">Array index of first character of string.</param>
    /// <param name="len">Length of string. A zero length indicates the empty string.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>len &lt; 0</c>, when
    /// <c>value == null and len > 0</c>, or when <c>start + len > value.Length</c>.</exception>
    public string Intern(char[] value, int start, int len)
    {
      if (len == 0)
        return String.Empty;
      else if (len < 0 || value == null) 
        IllegalLengthError("len", len);
      else if ((start + len) > value.Length) 
        ArrayOutOfBoundsError("start + len", start + len);
      int indx;
      bool found;
      fixed (char* strPtr = &value[start]) {
        found = FindSlot(strPtr, len, out indx);
      }
      if (found)
        return items[indx];
      else {
        string result = new string(value, start, len);
        object item = items[indx];
        items[indx] = result;
        IncCount(item == null);
        return result;
      }
    }

    /// <remarks>String argument does not have to be null-terminated.</remarks>
    /// <param name="value">Pointer to first character of string to be interned.
    /// May be <c>null</c>, in which case <c>len</c> must be zero.</param>
    /// <param name="len">Length of string. A zero length indicates the empty string.
    /// Must not reach beyond the end of the string.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>len &lt; 0</c>
    /// or when <c>value == null</c> and <c>len > 0</c>.</exception>
    public string Intern(char* value, int len)
    {
      if (len == 0)
        return String.Empty;
      else if (len < 0 || value == null)
        IllegalLengthError("len", len);
      int indx;
      if (FindSlot(value, len, out indx))
        return items[indx];
      else {
        string result = new string(value, 0, len);
        object item = items[indx];
        items[indx] = result;
        IncCount(item == null);
        return result;
      }
    }

    /// <remarks>String argument must be null-terminated, otherwise an access
    /// violation could follow.</remarks>
    /// <param name="value">Pointer to first character of null-terminated string to be
    /// interned. May be <c>null</c>, which is interpreted as the empty string.</param>
    public string Intern(char* value)
    {
      if (value == null)
        return String.Empty;
      int len = StringUtils.StrLen(value);
      if (len == 0)
        return String.Empty;
      int indx;
      if (FindSlot(value, len, out indx))
        return items[indx];
      else {
        string result = new string(value, 0, len);
        object item = items[indx];
        items[indx] = result;
        IncCount(item == null);
        return result;
      }
    }

    /// <overloads>
    /// <summary>Removes (un-interns) a string from the string table.</summary>
    /// <remarks>The empty string cannot be removed.</remarks>
    /// <returns><c>true</c> if successful, <c>false</c> if no matching string was found,
    /// or when trying to remove the empty string.</returns>
    /// </overloads>
    /// <param name="value">String to be removed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value == null</c>.</exception>
    public bool Remove(string value)
    {
      if (value == null)
        throw new ArgumentNullException("value");
      else if (value.Length == 0)
        return false;
      fixed (char* strPtr = value) {
        int indx;
        bool result = FindSlot(strPtr, value.Length, out indx);
        if (result) {
          items[indx] = deleted;
          DecCount();
        }
        return result;
      }
    }

    /// <param name="value">Character array representing string to be removed.
    /// May be <c>null</c> or empty, which indicates the empty string.</param>
    public bool Remove(char[] value)
    {
      if ((value == null) || (value.Length == 0))
        return false;
      fixed (char* strPtr = value) {
        int indx;
        bool result = FindSlot(strPtr, value.Length, out indx);
        if (result) {
          items[indx] = deleted;
          DecCount();
        }
        return result;
      }
    }

    /// <param name="value">Character array containing string to be removed.
    /// May be <c>null</c>, in which case <c>len</c> must be zero.</param>
    /// <param name="start">Array index of first character of string.</param>
    /// <param name="len">Length of string. A zero length indicates the empty string.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>len &lt; 0</c>, when
    /// <c>value == null and len > 0</c>, or when <c>start + len > value.Length</c>.</exception>
    public bool Remove(char[] value, int start, int len)
    {
      if (len == 0)
        return false;
      else if (len < 0 || value == null) 
        IllegalLengthError("len", len);
      else if ((start + len) > value.Length) 
        ArrayOutOfBoundsError("start + len", start + len);
      fixed (char* strPtr = &value[start]) {
        int indx;
        bool result = FindSlot(strPtr, len, out indx);
        if (result) {
          items[indx] = deleted;
          DecCount();
        }
        return result;
      }
    }

    /// <remarks>String argument does not have to be null-terminated.</remarks>
    /// <param name="value">Pointer to first character of string to be removed.
    /// May be <c>null</c>, in which case <c>len</c> must be zero.</param>
    /// <param name="len">Length of string. A zero length indicates the empty string.
    /// Must not reach beyond the end of the string.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>len &lt; 0</c>
    /// or when <c>value == null</c> and <c>len > 0</c>.</exception>
    public bool Remove(char* value, int len)
    {
      if (len == 0)
        return false;
      else if (len < 0 || value == null)
        IllegalLengthError("len", len);
      int indx;
      bool result = FindSlot(value, len, out indx);
      if (result) {
        items[indx] = deleted;
        DecCount();
      }
      return result;
    }

    /// <remarks>String argument must be null-terminated, otherwise an access
    /// violation could follow.</remarks>
    /// <param name="value">Pointer to first character of null-terminated string to be
    /// removed. May be <c>null</c>, which is interpreted as the empty string.</param>
    public bool Remove(char* value)
    {
      if (value == null)
        return false;
      int len = StringUtils.StrLen(value);
      if (len == 0)
        return false;
      int indx;
      bool result = FindSlot(value, len, out indx);
      if (result) {
        items[indx] = deleted;
        DecCount();
      }
      return result;
    }

    /// <overloads>
    /// <summary>Searches for a string in the string table.</summary>
    /// <remarks>A search for the empty string will always succeed.</remarks>
    /// <returns>Returns string matching the argument, or <c>null</c> if none found.</returns>
    /// </overloads>
    /// <param name="value">String to find.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value == null</c>.</exception>
    public string Find(string value)
    {
      if (value == null)
        throw new ArgumentNullException("value");
      else if (value.Length == 0)
        return String.Empty;
      int indx;
      bool found;
      fixed (char* strPtr = value) {
        found = FindSlot(strPtr, value.Length, out indx);
      }
      if (found)
        return items[indx];
      else
        return null;
    }

    /// <param name="value">Character array representing string to find.
    /// May be <c>null</c> or empty, which indicates the empty string.</param>
    public string Find(char[] value)
    {
      if ((value == null) || (value.Length == 0))
        return String.Empty;
      int indx;
      bool found;
      fixed (char* strPtr = value) {
        found = FindSlot(strPtr, value.Length, out indx);
      }
      if (found)
        return items[indx];
      else
        return null;
    }

    /// <param name="value">Character array containing string to find.
    /// May be <c>null</c>, in which case <c>len</c> must be zero.</param>
    /// <param name="start">Array index of first character of string.</param>
    /// <param name="len">Length of string. A zero length indicates the empty string.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>len &lt; 0</c>, when
    /// <c>value == null and len > 0</c>, or when <c>start + len > value.Length</c>.</exception>
    public string Find(char[] value, int start, int len)
    {
      if (len == 0)
        return String.Empty;
      else if (len < 0 || value == null)
        IllegalLengthError("len", len);
      else if ((start + len) > value.Length)
        ArrayOutOfBoundsError("start + len", start + len);
      int indx;
      bool found;
      fixed (char* strPtr = &value[start]) {
        found = FindSlot(strPtr, len, out indx);
      }
      if (found)
        return items[indx];
      else
        return null;
    }

    /// <remarks>String argument does not have to be null-terminated.</remarks>
    /// <param name="value">Pointer to first character of string to find.
    /// May be <c>null</c>, in which case <c>len</c> must be zero.</param>
    /// <param name="len">Length of string. A zero length indicates the empty string.
    /// Must not reach beyond the end of the string.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>len &lt; 0</c>
    /// or when <c>value == null</c> and <c>len > 0</c>.</exception>
    public string Find(char* value, int len)
    {
      if (len == 0)
        return String.Empty;
      else if (len < 0 || value == null)
        IllegalLengthError("len", len);
      int indx;
      if (FindSlot(value, len, out indx))
        return items[indx];
      else
        return null;
    }

    /// <remarks>String argument must be null-terminated, otherwise an access
    /// violation could follow.</remarks>
    /// <param name="value">Pointer to first character of null-terminated string to be
    /// find. May be <c>null</c>, which is interpreted as the empty string.</param>
    public string Find(char* value)
    {
      if (value == null)
        return String.Empty;
      int len = StringUtils.StrLen(value);
      if (len == 0)
        return String.Empty;
      int indx;
      if (FindSlot(value, len, out indx))
        return items[indx];
      else
        return null;
    }

    /// <summary>Clears table of all interned strings.</summary>
    public void Clear()
    {
      int indx;
      for (indx = 0; indx < items.Length; indx++)
        items[indx] = null;
      count = 0;
      usedCount = 0;
    }

    /// <summary>Removes deleted entries to reclaim lost performance.</summary>
    public void Rebuild() {
      int indx, tempIndx;
      uint mask = ((uint)1 << sizeLog) - 1;
      string[] tempItems = new string[count];

      // re-hash: loop through existing items and store them in temporary array
      tempIndx = 0;
      for (indx = items.Length - 1; indx >= 0; indx--) {
        string str = items[indx];
        // (object) cast forces pointer comparison
        if ((str != null) && ((object)str != (object)deleted)) {
          tempItems[tempIndx] = str;
          tempIndx++;
        }
      }
      // Debug.Assert(count == tempIndx);

      // clear entries
      ((IList)items).Clear();
      usedCount = count;

      for (tempIndx = tempItems.Length - 1; tempIndx >= 0; tempIndx--) {
        string str = tempItems[tempIndx];
        int keyHash = StringUtils.StrHash(str, 0);
        int newIndx = unchecked(keyHash & (int)mask);
        // don't have to check for deleted strings in cleared items array
        if (items[newIndx] != null) 
          unchecked {
            // see comments to step in FindSlot()
            int step = ((((keyHash & (int)~mask) >> (sizeLog - 1)) &
                         (int)(mask >> 2)) | (int)1) & (int)0xFF;
            do {
              newIndx = newIndx - step;
              if (newIndx < 0)
                newIndx = newIndx + (int)mask + 1;
            } while (items[newIndx] != null);
          }
        items[newIndx] = str;
      }
    }

    /// <summary>Returns iterator cookie for starting a new iteration.</summary>
    public int StartIter()
    {
      return 0;
    }

    /// <summary>Retrieves next string and advances iterator.</summary>
    /// <param name="iter">Iterator cookie representing current iterator state.</param>
    /// <returns>Interned string if iterator is valid, or <c>null</c> if end has been
    /// reached.</returns>
    public string Next(ref int iter)
    {
      int indx;
      for (indx = iter; indx < items.Length; indx++) {
        string result = items[indx];
        if ((result != null) && ((object)result != (object)deleted)) {
          iter = indx + 1;
          return result;
        }
      }
      return null;
    }

    /// <summary>Base 2 logarithm of string table capacity.</summary>
    /// <remarks>The capacity must be at least twice the number of entries,
    /// which means: 2^SizeLog >= (2 * Count).</remarks>
    public byte SizeLog
    {
      get { return sizeLog; }
      set { SetSizeLog(value); }
    }

    /// <summary>Number of entries in string table.</summary>
    public int Count
    {
      get { return count; }
    }

    /// <summary>
    /// Allows capacity to shrink automatically when count decreases (to about 25%).
    /// </summary>
    public bool AutoShrink
    {
      get { return autoShrink; }
      set { autoShrink = value; }
    }
  }

}
