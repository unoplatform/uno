using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Helpers;

[TestClass]
public class ActiveFrameworkReloadPolicyTests
{
	private const string Android = "net10.0-android";
	private const string Ios = "net10.0-ios";
	private const string Desktop = "net10.0-desktop";
	private const string Windows = "net10.0-windows10.0.26100";
	private const string Wasm = "net10.0-browserwasm";

	private static readonly Version _current = new(18, 0);
	private static readonly Version _legacy = new(17, 11);

	[TestMethod]
	[DataRow(Android, "android")]
	[DataRow("net9.0-android35.0", "android35")]
	[DataRow(Ios, "ios")]
	[DataRow(Desktop, "desktop")]
	[DataRow(Windows, "windows10")]
	[DataRow(Wasm, "browserwasm")]
	[DataRow("net10.0", null)]
	public void When_ReadingIdentifier(string framework, string? expected)
		=> ActiveFrameworkReloadPolicy.GetTargetFrameworkIdentifier(framework).Should().Be(expected);

	[TestMethod]
	[DataRow(Android)]
	[DataRow("net9.0-android35.0")]
	[DataRow(Ios)]
	[DataRow(Desktop)]
	[DataRow(Windows)]
	[Description("The SDK only reorders TargetFrameworks for browserwasm, so these evaluate the same with or without the .user value.")]
	public void When_SelectedFrameworkUnset_And_NotWasm_Then_NoReload(string framework)
		=> ActiveFrameworkReloadPolicy.RequiresReload(null, framework, _current, isSelectedFrameworkUnset: true).Should().BeFalse();

	[TestMethod]
	public void When_SelectedFrameworkUnset_And_Wasm_Then_Reload()
		=> ActiveFrameworkReloadPolicy.RequiresReload(null, Wasm, _current, isSelectedFrameworkUnset: true).Should().BeTrue();

	[TestMethod]
	[DataRow(Android, false)]
	[DataRow(Ios, false)]
	[DataRow(Desktop, true)]
	[DataRow(Windows, true)]
	[DataRow(Wasm, true)]
	public void When_SelectedFrameworkUnset_And_LegacyVisualStudio(string framework, bool expected)
		=> ActiveFrameworkReloadPolicy.RequiresReload(null, framework, _legacy, isSelectedFrameworkUnset: true).Should().Be(expected);

	[TestMethod]
	[DataRow(Android)]
	[DataRow(Wasm)]
	[Description("The first report after a load is the framework that was just evaluated, it is not a change.")]
	public void When_NoPreviousFramework_Then_NoReload(string framework)
		=> ActiveFrameworkReloadPolicy.RequiresReload(null, framework, _current, isSelectedFrameworkUnset: false).Should().BeFalse();

	[TestMethod]
	[DataRow(Android, Wasm, true)]
	[DataRow(Wasm, Android, true)]
	[DataRow(Desktop, Wasm, true)]
	[DataRow(Android, Desktop, false)]
	[DataRow(Desktop, Windows, false)]
	[DataRow(Android, Ios, false)]
	[DataRow("net10.0", Wasm, false)]
	public void When_FrameworkChanges(string previous, string next, bool expected)
		=> ActiveFrameworkReloadPolicy.RequiresReload(previous, next, _current, isSelectedFrameworkUnset: false).Should().Be(expected);

	[TestMethod]
	[DataRow(Android, Desktop, true)]
	[DataRow(Windows, Android, true)]
	[DataRow(Android, Ios, false)]
	public void When_FrameworkChanges_And_LegacyVisualStudio(string previous, string next, bool expected)
		=> ActiveFrameworkReloadPolicy.RequiresReload(previous, next, _legacy, isSelectedFrameworkUnset: false).Should().Be(expected);

	[TestMethod]
	[DataRow(Android, Desktop, false)]
	[DataRow(Android, Wasm, true)]
	public void When_VisualStudioVersionUnknown_Then_TreatedAsCurrent(string previous, string next, bool expected)
		=> ActiveFrameworkReloadPolicy.RequiresReload(previous, next, null, isSelectedFrameworkUnset: false).Should().Be(expected);
}
