using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

[assembly: Uno.UI.RemoteControl.Host.ServerProcessor<FileUpdateProcessor>]

namespace Uno.UI.RemoteControl.Host.HotReload;

partial class FileUpdateProcessor : IServerProcessor, IDisposable
{
	// *******************************************
	// *******************************************
	// ***************** WARNING *****************
	// *******************************************
	// *******************************************
	//
	// This processor is present only for legacy purposes.
	// The Scope of the UpdateFile message has been changed from WellKnownScopes.Testing to WellKnownScopes.HotReload.
	// This processor will only handle requests made on the old scope, like old version of the runtime-test engine.
	// The new processor that is handling those messages is now the ServerHotReloadProcessor.

	public FileUpdateProcessor(IRemoteControlServer remoteControlServer)
	{
		// Parameter is unused, but required by the DI system.
	}

	public string Scope => WellKnownScopes.Testing;

	public void Dispose()
	{
	}

	public Task ProcessFrame(Frame frame)
	{
		switch (frame.Name)
		{
			case nameof(UpdateSingleFileRequest):
				ProcessUpdateFile(JsonConvert.DeserializeObject<UpdateSingleFileRequest>(frame.Content)!);
				break;
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task ProcessIdeMessage(IdeMessage message, CancellationToken ct)
		=> Task.CompletedTask;

	private void ProcessUpdateFile(UpdateSingleFileRequest? message)
	{
		if (message?.IsValid() is not true)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Got an invalid update file frame ({message})");
			}

			return;
		}

		if (!File.Exists(message.FilePath))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Requested file '{message.FilePath}' does not exists.");
			}

			return;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug($"Apply Changes to {message.FilePath}");
		}

		var originalContent = File.ReadAllText(message.FilePath);
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().LogTrace($"Original content: {message.FilePath}");
		}

		var updatedContent = originalContent.Replace(message.OldText, message.NewText);
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().LogTrace($"Updated content: {message.FilePath}");
		}

		File.WriteAllText(message.FilePath, updatedContent);
	}
}
