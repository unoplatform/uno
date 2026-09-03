// Whole-file HAS_UNO guard rather than the [PlatformCondition] the runtime-test rules prescribe:
// on the WinAppSDK head these types come from the platform projection, so neither the expected
// assembly name nor the Uno.UI sweep has any meaning there - the test should not compile at all.
#if HAS_UNO
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_Windows_ApplicationModel_Resources;

[TestClass]
public class Given_MrtCoreAssemblyIdentity
{
	private const string MrtCoreNamespace = "Microsoft.Windows.ApplicationModel.Resources";

	// The assembly and the package id happen to match here; 7.0 renamed this assembly from "Uno".
	private const string ExpectedAssemblyName = "Uno.WinRT";

	private const string RepairHint = "Fix the namespace routing in src/Uno.WinAppSDKSyncGenerator/Generator.cs, "
		+ "and the relocation entries in build/PackageDiffIgnore.xml.";

	[TestMethod]
	[DataRow(typeof(IResourceContext))]
	[DataRow(typeof(IResourceManager))]
	[DataRow(typeof(KnownResourceQualifierName))]
	[DataRow(typeof(MrtCoreContract))]
	[DataRow(typeof(ResourceCandidate))]
	[DataRow(typeof(ResourceCandidateKind))]
	[DataRow(typeof(ResourceContext))]
	[DataRow(typeof(ResourceLoader))]
	[DataRow(typeof(ResourceManager))]
	[DataRow(typeof(ResourceMap))]
	[DataRow(typeof(ResourceNotFoundEventArgs))]
	public void When_MrtCoreType_Then_Declared_In_The_Uno_WinRT_Assembly(Type type)
		=> Assert.AreEqual(
			ExpectedAssemblyName,
			type.Assembly.GetName().Name,
			$"{type.FullName} must be declared in '{ExpectedAssemblyName}'. {RepairHint}");

	[TestMethod]
	public void When_Legacy_WinRT_ResourceLoader_Then_Also_In_The_Uno_WinRT_Assembly()
		=> Assert.AreEqual(
			ExpectedAssemblyName,
			typeof(global::Windows.ApplicationModel.Resources.ResourceLoader).Assembly.GetName().Name,
			"The pre-MRT loader has always lived in the WinRT assembly; the migration note says so explicitly.");

	[TestMethod]
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Regarding Assembly.GetTypes(): the trimmer may remove types, which can only shrink the swept set. The anchor assertion below fails loudly if the assembly itself is not the expected one.")]
	public void When_Uno_UI_Then_Declares_No_MrtCoreType()
	{
		var unoUI = typeof(Microsoft.UI.Xaml.UIElement).Assembly;

		Assert.AreEqual("Uno.UI", unoUI.GetName().Name, "Sweep anchor moved out of Uno.UI - retarget this test.");

		// The DataRows above only cover today's types; this catches a routing regression that
		// declares a *new* MRT Core type back in Uno.UI.
		var strays = unoUI.GetTypes()
			.Where(type => type.Namespace?.StartsWith(MrtCoreNamespace, StringComparison.Ordinal) == true)
			.Select(type => type.FullName)
			.ToArray();

		Assert.AreEqual(
			string.Empty,
			string.Join(", ", strays),
			$"{MrtCoreNamespace} must not be declared in Uno.UI. {RepairHint}");
	}
}
#endif
