using System;
using System.Reflection;
using System.Security;

[assembly: AssemblyProduct("Uno")]
[assembly: AssemblyCompany("nventive")]

#if !METRO && !WINPRT && !SILVERLIGHT
[assembly: SecurityTransparent]
#endif

#if SILVERLIGHT
#elif WINDOWS_PHONE
#elif METRO
#elif XAMARIN
#else
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityRules(SecurityRuleSet.Level1)]
#endif

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCopyright("Copyright (C) 2009-2019 nVentive.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.888")]
[assembly: AssemblyInformationalVersion("2.3.888")]
