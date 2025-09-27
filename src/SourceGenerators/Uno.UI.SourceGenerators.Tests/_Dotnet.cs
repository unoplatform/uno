using System;
using System.Linq;
using Microsoft.CodeAnalysis.Testing;

namespace Uno.UI.SourceGenerators.Tests;

internal record class _Dotnet(string Moniker, ReferenceAssemblies ReferenceAssemblies)
{
	public ReferenceAssemblies WithUnoPackage(string version = "5.0.118")
		=> ReferenceAssemblies.AddPackages([new PackageIdentity("Uno.WinUI", version)]);

	public static _Dotnet Previous = new("net9.0", ReferenceAssemblies.Net.Net80);

	public static _Dotnet Current = new("net10.0", ReferenceAssemblies.Net.Net90);

	public static _Dotnet CurrentAndroid = new("net10.0-android", ReferenceAssemblies.Net.Net90Android);
}
