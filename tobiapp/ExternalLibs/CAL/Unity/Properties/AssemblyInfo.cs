//===============================================================================
// Microsoft patterns & practices
// Unity Application Block
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================

using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.Practices.Unity")]
[assembly: AssemblyDescription("Unity Dependency Injection Framework")]

[assembly: ComVisibleAttribute(false)]

#if !NET_3_5 // NET_4_0 || BOOTSTRAP_NET_4_0
//[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif