//
// AddinLocalizer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Addins.Localization;

namespace Mono.Addins
{
	public class AddinLocalizer
	{
		IAddinLocalizer localizer;
		IPluralAddinLocalizer pluralLocalizer;
		
		internal AddinLocalizer (IAddinLocalizer localizer)
		{
			this.localizer = localizer;
			pluralLocalizer = localizer as IPluralAddinLocalizer;
		}
		
		public string GetString (string msgid)
		{
			return localizer.GetString (msgid);
		}
		
		public string GetString (string msgid, params string[] args)
		{
			return string.Format (localizer.GetString (msgid), args);
		}
		
		public string GetString (string msgid, params object[] args)
		{
			return string.Format (localizer.GetString (msgid), args);
		}
		
		public string GetPluralString (string msgid, string defaultPlural, int n)
		{
			// If the localizer does not support plural forms, just use GetString to
			// get a translation. It is not correct to check 'n' in this case because
			// there is no guarantee that 'defaultPlural' will be translated.
			
			if (pluralLocalizer != null)
				return pluralLocalizer.GetPluralString (msgid, defaultPlural, n);
			else
				return GetString (msgid);
		}
		
		public string GetPluralString (string singular, string defaultPlural, int n, params string[] args)
		{
			return string.Format (GetPluralString (singular, defaultPlural, n), args);
		}
		
		public string GetPluralString (string singular, string defaultPlural, int n, params object[] args)
		{
			return string.Format (GetPluralString (singular, defaultPlural, n), args);
		}
	}
}
