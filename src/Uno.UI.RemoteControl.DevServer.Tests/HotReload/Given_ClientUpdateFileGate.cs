using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.DevServer.Tests.HotReload;

/// <summary>
/// Unit tests for the <see cref="ClientHotReloadProcessor.TryUpdateFilesAsync"/> initialization
/// gate: a request issued before the first hot-reload status notification must fail with a
/// typed error in the result instead of throwing (startup timing window).
/// </summary>
[TestClass]
public class Given_ClientUpdateFileGate
{
	[TestMethod]
	[DataRow(false, DisplayName = "write/persist-only request (WaitForHotReload = false)")]
	[DataRow(true, DisplayName = "hot-reload request (WaitForHotReload = true)")]
	public async Task When_UpdateFileBeforeFirstStatus_Then_FailsWithTypedError(bool waitForHotReload)
	{
		// A processor that never received any status notification from the engine.
		var sut = new ClientHotReloadProcessor(new NoOpRemoteControlClient());
		var req = new ClientHotReloadProcessor.UpdateRequest("some/File.xaml", OldText: null, NewText: "<Page />", WaitForHotReload: waitForHotReload);

		var result = await sut.TryUpdateFilesAsync(req, CancellationToken.None);

		result.Error.Should().BeOfType<InvalidOperationException>(
			because: "the gate must reject un-trackable requests with an actionable error, not an NRE");
	}

	private sealed class NoOpRemoteControlClient : IRemoteControlClient
	{
		public Type AppType => typeof(Given_ClientUpdateFileGate);

		// Never reached: the initialization gate returns before any server exchange.
		public Task SendMessage(IMessage message) => Task.CompletedTask;
	}
}
