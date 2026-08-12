using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Testing;

namespace Uno.UI.SourceGenerators.Tests;

internal record class _Dotnet(string Moniker, ReferenceAssemblies ReferenceAssemblies)
{
	// https://github.com/dotnet/roslyn-sdk/blob/cc62642b58a306b2560a2367f1a0c11167ea3b2b/src/Microsoft.CodeAnalysis.Testing/Microsoft.CodeAnalysis.Analyzer.Testing/ReferenceAssemblies.cs#L1216-L1231
	private static ReferenceAssemblies Net110 = new ReferenceAssemblies(
		"net11.0",
		new PackageIdentity(
			"Microsoft.NETCore.App.Ref",
			"11.0.0-preview.6.26359.118"),
		Path.Combine("ref", "net11.0")
	);


	// https://github.com/dotnet/roslyn-sdk/blob/cc62642b58a306b2560a2367f1a0c11167ea3b2b/src/Microsoft.CodeAnalysis.Testing/Microsoft.CodeAnalysis.Analyzer.Testing/ReferenceAssemblies.cs#L1245-L1249
	private static ReferenceAssemblies Net110Android = Net110
		.AddPackages(
			ImmutableArray.Create(
				new PackageIdentity("Microsoft.Android.Ref.37", "37.0.0-preview.6.59")));

	public ReferenceAssemblies WithUnoPackage(string version = "5.0.118")
		=> ReferenceAssemblies.AddPackages([new PackageIdentity("Uno.WinUI", version)]);

	public static _Dotnet Previous = new("net10.0", ReferenceAssemblies.Net.Net100);

	public static _Dotnet Current = new("net11.0", Net110);

	public static _Dotnet CurrentAndroid = new("net11.0-android", Net110Android);
}
