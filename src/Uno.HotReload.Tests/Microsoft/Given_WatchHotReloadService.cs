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
	[Description(
		"No runtime DECLARES AddExplicitInterfaceImplementation even though .NET and Mono " +
		"support it (only .NET Framework does not); dotnet-watch grants it implicitly and so " +
		"must the shim — without it, Roslyn refuses to Replace any [CreateNewOnMetadataUpdate] " +
		"type having an explicitly-implemented member (every generated XAML ResourceDictionary " +
		"singleton) with ENC0106/CS9346.")]
	public void When_AddImplicitCapabilities_Then_AppendsAddExplicitInterfaceImplementation()
	{
		// The exact capability set reported by the .NET 10 CoreCLR (MetadataUpdater.GetCapabilities()):
		// none of the runtimes reports AddExplicitInterfaceImplementation, the grant must add it.
		var reported = "Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters GenericUpdateMethod GenericAddMethodToExistingType GenericAddFieldToExistingType AddFieldRva".Split(' ');

		var granted = WatchHotReloadService.AddImplicitCapabilities(reported);

		CollectionAssert.AreEqual(
			reported.Where(c => c != "AddFieldRva").Append("AddExplicitInterfaceImplementation").ToArray(),
			granted.ToArray());
	}

	[TestMethod]
	[Description(
		"AddFieldRva must be WITHDRAWN even though the runtime reports it: CoreCLR's EnC " +
		"metadata layer loses delta-ADDED FieldRVA rows once the table's lookup hash is built " +
		"(25-row threshold, no linear fallback, no hash maintenance on the apply path), which " +
		"kills every update after a few generations on real apps ('The assembly update " +
		"failed'). Without the capability Roslyn emits the historical element-wise " +
		"array-initializer codegen — no FieldRVA rows in deltas, same semantics.")]
	public void When_AddImplicitCapabilities_Then_WithdrawsAddFieldRva()
	{
		var granted = WatchHotReloadService.AddImplicitCapabilities(["Baseline", "AddFieldRva", "NewTypeDefinition"]);

		CollectionAssert.AreEqual(new[] { "Baseline", "NewTypeDefinition", "AddExplicitInterfaceImplementation" }, granted.ToArray());
	}
}
