﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Tobi.Modules.NavigationPane")]
[assembly: AssemblyDescription("Tobi, Accessible DAISY Multimedia Authoring")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("DAISY Consortium")]
[assembly: AssemblyProduct("Tobi")]
[assembly: AssemblyCopyright("Open-Source, Free, LGPL")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: ComVisibleAttribute(false)]

#if !NET_3_5 // NET_4_0 || BOOTSTRAP_NET_4_0
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif