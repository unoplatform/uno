using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.HotReload.Messages;

[assembly: Uno.UI.RemoteControl.Host.ServerProcessorAttribute(typeof(Uno.UI.RemoteControl.Host.HotReload.FileUpdateProcessor))]

namespace Uno.UI.RemoteControl.Host.HotReload;

partial class FileUpdateProcessor : IServerProcessor, IDisposable
{
	private readonly IRemoteControlServer _remoteControlServer;

	public FileUpdateProcessor(IRemoteControlServer remoteControlServer)
	{
		_remoteControlServer = remoteControlServer;
	}

	public string Scope => HotReloadConstants.TestingScopeName;

	public void Dispose()
	{
	}

	public Task ProcessFrame(Frame frame)
	{
		switch (frame.Name)
		{
			case nameof(UpdateFile):
				ProcessUpdateFile(JsonConvert.DeserializeObject<UpdateFile>(frame.Content)!);
				break;
		}

		return Task.CompletedTask;
	}

	private void ProcessUpdateFile(UpdateFile? message)
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
