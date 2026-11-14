using Uno.UI.RemoteControl.HotReload;

namespace Uno.UI.RemoteControl.DevServer.Tests.HotReload;

/// <summary>
/// Tests for <see cref="ClientHotReloadProcessor.IsMetadataUpdateRejectedByRuntime"/>: a desktop app applying
/// dev-server deltas itself must be reported as unable to hot reload when the runtime does not allow
/// assemblies to be modified (started without DOTNET_MODIFIABLE_ASSEMBLIES=debug), and only then.
/// </summary>
[TestClass]
public class Given_MetadataUpdateRuntimeSupport
{
	[TestMethod]
	public void When_DesktopAppAppliesDeltasAndRuntimeRejectsThem_Then_Rejected()
		=> ClientHotReloadProcessor.IsMetadataUpdateRejectedByRuntime(
			devServerEnabled: true,
			isDesktop: true,
			runningInsideVSCodeExtension: false,
			debuggerAttached: false,
			runtimeSupportsMetadataUpdates: false).Should().BeTrue();

	[TestMethod]
	public void When_RuntimeSupportsMetadataUpdates_Then_NotRejected()
		=> ClientHotReloadProcessor.IsMetadataUpdateRejectedByRuntime(
			devServerEnabled: true,
			isDesktop: true,
			runningInsideVSCodeExtension: false,
			debuggerAttached: false,
			runtimeSupportsMetadataUpdates: true).Should().BeFalse();

	[TestMethod]
	[DataRow(false, true, false, false, DisplayName = "IDE-driven (VS): the dev-server does not produce deltas")]
	[DataRow(true, false, false, false, DisplayName = "Mono target: variable set by Uno.WinUI.DevServer.targets")]
	[DataRow(true, true, true, false, DisplayName = "VS Code extension: the debugger applies the deltas")]
	[DataRow(true, true, false, true, DisplayName = "Debugger attached: the debugger applies the deltas")]
	public void When_AppDoesNotApplyDeltasItself_Then_NotRejected(bool devServerEnabled, bool isDesktop, bool runningInsideVSCodeExtension, bool debuggerAttached)
		=> ClientHotReloadProcessor.IsMetadataUpdateRejectedByRuntime(
			devServerEnabled,
			isDesktop,
			runningInsideVSCodeExtension,
			debuggerAttached,
			runtimeSupportsMetadataUpdates: false).Should().BeFalse();
}
