//
// TypeExtensionPointAttribute.cs
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

namespace Mono.Addins
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=true)]
	public class TypeExtensionPointAttribute: Attribute
	{
		string path;
		string nodeName;
		Type nodeType;
		string desc;
		string name;
		
		public TypeExtensionPointAttribute ()
		{
		}
		
		public TypeExtensionPointAttribute (string path)
		{
			this.path = path;
		}
		
		public string Path {
			get { return path != null ? path : string.Empty; }
			set { path = value; }
		}
		
		public string Description {
			get { return desc != null ? desc : string.Empty; }
			set { desc = value; }
		}
		
		public string NodeName {
			get { return nodeName != null && nodeName.Length > 0 ? nodeName : "Type"; }
			set { nodeName = value; }
		}
		
		public string Name {
			get { return name != null ? name : string.Empty; }
			set { name = value; }
		}

		public Type NodeType {
			get { return nodeType != null ? nodeType : typeof(TypeExtensionNode); }
			set { nodeType = value; }
		}
}
}
