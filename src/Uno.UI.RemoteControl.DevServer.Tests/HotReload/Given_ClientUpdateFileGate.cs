using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
/// typed error in the result instead of throwing (startup timing window), while requests issued
/// after a status — including a Disabled one — must still reach the server.
/// </summary>
[TestClass]
public class Given_ClientUpdateFileGate
{
	[TestMethod]
	[Description(
		"Requests issued before the first hot-reload status must fail with a typed, actionable error "
		+ "(HotReloadNotInitializedException) instead of an NRE, without any server exchange — for both "
		+ "persist-only and hot-reload requests.")]
	[DataRow(false, DisplayName = "write/persist-only request (WaitForHotReload = false)")]
	[DataRow(true, DisplayName = "hot-reload request (WaitForHotReload = true)")]
	public async Task When_UpdateFileBeforeFirstStatus_Then_FailsWithTypedError(bool waitForHotReload)
	{
		// A processor that never received any status notification from the engine.
		var client = new RecordingRemoteControlClient();
		var sut = new ClientHotReloadProcessor(client);
		var req = new ClientHotReloadProcessor.UpdateRequest("some/File.xaml", OldText: null, NewText: "<Page />", WaitForHotReload: waitForHotReload);

		var result = await sut.TryUpdateFilesAsync(req, CancellationToken.None);

		result.Error.Should().BeOfType<HotReloadNotInitializedException>(
			because: "the gate must reject un-trackable requests with an actionable error, not an NRE");
		client.SentMessages.Should().BeEmpty(
			because: "the gate must reject the request before any server exchange");
	}

	[TestMethod]
	[Description(
		"Once the first hot-reload status has been received, the initialization gate must let requests "
		+ "through: the request reaches the server (and only fails here because the fake server never replies).")]
	public async Task When_UpdateFileAfterFirstStatus_Then_PassesGate()
	{
		var client = new RecordingRemoteControlClient();
		var sut = new ClientHotReloadProcessor(client);
		await PublishStatus(sut, HotReloadState.Ready);
		var req = new ClientHotReloadProcessor.UpdateRequest("some/File.xaml", OldText: null, NewText: "<Page />", WaitForHotReload: false)
		{
			ServerUpdateTimeout = TimeSpan.FromMilliseconds(100),
		};

		var result = await sut.TryUpdateFilesAsync(req, CancellationToken.None);

		result.Error.Should().BeOfType<TimeoutException>(
			because: "a received status un-gates the call, which then fails only on the unanswered server exchange");
		client.SentMessages.Should().ContainSingle(msg => msg is UpdateFileRequest,
			because: "the request must be sent to the server once the gate is open");
	}

	[TestMethod]
	[Description(
		"When hot reload is disabled for the session, updates must NOT be rejected: the request is still "
		+ "forwarded to the server so files can be written/persisted (the disabled state is surfaced in logs only).")]
	public async Task When_UpdateFileWhileDisabled_Then_StillProcessed()
	{
		var client = new RecordingRemoteControlClient();
		var sut = new ClientHotReloadProcessor(client);
		await PublishStatus(sut, HotReloadState.Disabled);
		var req = new ClientHotReloadProcessor.UpdateRequest("some/File.xaml", OldText: null, NewText: "<Page />", WaitForHotReload: false)
		{
			ServerUpdateTimeout = TimeSpan.FromMilliseconds(100),
		};

		var result = await sut.TryUpdateFilesAsync(req, CancellationToken.None);

		result.Error.Should().NotBeOfType<HotReloadNotInitializedException>(
			because: "a Disabled status is still a status — the initialization gate must not fire");
		client.SentMessages.Should().ContainSingle(msg => msg is UpdateFileRequest,
			because: "with hot reload disabled the update must still be forwarded so files can be persisted");
	}

	private static async Task PublishStatus(ClientHotReloadProcessor sut, HotReloadState state)
	{
		var status = new HotReloadStatusMessage(state, ImmutableList<HotReloadServerOperationData>.Empty);

		await sut.ProcessFrame(Frame.Create(1, status.Scope, HotReloadStatusMessage.Name, status));
	}

	private sealed class RecordingRemoteControlClient : IRemoteControlClient
	{
		private readonly List<IMessage> _sentMessages = [];

		public Type AppType => typeof(Given_ClientUpdateFileGate);

		public IReadOnlyList<IMessage> SentMessages => _sentMessages;

		public Task SendMessage(IMessage message)
		{
			_sentMessages.Add(message);
			return Task.CompletedTask;
		}
	}
}
