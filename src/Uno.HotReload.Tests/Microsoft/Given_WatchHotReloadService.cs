using Uno.HotReload.Microsoft;

namespace Uno.HotReload.Tests.Microsoft;

/// <summary>
/// Tests for the capability handling of the <see cref="WatchHotReloadService"/> shim: the
/// runtime-reported capabilities must be augmented with the implicit one dotnet-watch grants
/// (<c>AddExplicitInterfaceImplementation</c> — supported by .NET and Mono, declared by neither),
/// without which reloadable types with explicitly-implemented members (every generated XAML
/// ResourceDictionary singleton) are rejected with ENC0106/CS9346.
/// </summary>
[TestClass]
public sealed class Given_WatchHotReloadService
{
	[TestMethod]
	public void When_AddImplicitCapabilities_Then_AppendsAddExplicitInterfaceImplementation()
	{
		// The exact capability set reported by the .NET 10 CoreCLR (MetadataUpdater.GetCapabilities()):
		// none of the runtimes reports AddExplicitInterfaceImplementation, the grant must add it.
		var reported = "Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters GenericUpdateMethod GenericAddMethodToExistingType GenericAddFieldToExistingType AddFieldRva".Split(' ');

		var granted = WatchHotReloadService.AddImplicitCapabilities(reported);

		CollectionAssert.AreEqual(reported.Append("AddExplicitInterfaceImplementation").ToArray(), granted.ToArray());
	}
}
