#pragma warning disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.HotReload;
using StreamJsonRpc;
using Uno.DotNet.Watch;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Host.HotReload;

partial class ServerHotReloadProcessor
{
	private readonly CancellationTokenSource _unoWatchToken = new();
	
	private bool InitializeUnoWatch(ConfigureServer configureServer, Dictionary<string, string> properties)
	{
		_ = OpenUnoWatchPipeAsync(_unoWatchToken.Token);
		return true;
	}

	private async Task OpenUnoWatchPipeAsync(CancellationToken ct)
	{
		var rpcChannelId = Guid.NewGuid().ToString("N");
#pragma warning disable CA2000 // Call System.IDisposable.Dispose : Will be disposed by the JsonRpc instance
		var rpcStream = new NamedPipeServerStream(
			pipeName: rpcChannelId,
			direction: PipeDirection.InOut,
			maxNumberOfServerInstances: 1,
			transmissionMode: PipeTransmissionMode.Byte,
			options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);
#pragma warning restore CA2000
		var pipeConnected = rpcStream.WaitForConnectionAsync(ct);

		//dotnet" build "V:\source\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp.csproj" -f:net10.0-desktop -c:Debug -clp:NoSummary -p:GenerateFullPaths=true -p:UnoForceSingleTFM=true -bl:"V:\source\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp\net10.0-desktop-Debug.binlog"
		//dotnet" build "V:\source\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp.csproj" -f:net10.0-android -c:Debug -r:android-x64 -p:EmbedAssembliesIntoApk=true -clp:NoSummary -p:GenerateFullPaths=true -p:UnoForceSingleTFM=true -bl:"V:\source\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp\net10.0-android-Debug-android-x64.binlog"
		var process = Process.Start(new ProcessStartInfo(
			"dotnet",
			[
				@"C:\Src\GitHub\dotnet\sdk\artifacts\bin\dotnet-watch\Debug\net10.0\dotnet-watch.dll",
				@"--project:V:\source\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp.csproj",
				
				//"-f:net10.0-desktop",
				"-f:net10.0-android",
				"-r:android-x64",
				
				"-c:Debug",
				"--",
				"-clp:NoSummary",
				"-p:GenerateFullPaths=true",
				//"-p:UnoForceSingleTFM=true",
				"-bl:\"V:\\source\\UnoWatchTestApp\\UnoWatchTestApp\\UnoWatchTestApp\\net10.0-desktop-Debug-hot-reload.binlog\""
			])
		{
			WorkingDirectory = @"V:\source\UnoWatchTestApp\UnoWatchTestApp\UnoWatchTestApp\",
			EnvironmentVariables =
			{
				{"DOTNET_WATCH_DEBUG_SDK_DIRECTORY", @"C:\Program Files\dotnet\sdk\10.0.100-rc.2.25502.107"},
				{"UNO_WATCH_DEVSERVER_CHANNEL", rpcChannelId}
			}
		});
		ct.Register(process.Kill);

		//var timeout = Task.Delay(TimeSpan.FromMilliseconds(1000), ct);
		//if (await Task.WhenAny(pipeConnected, timeout) == timeout)
		//{
		//	throw new TimeoutException("Unable to connect to the RPC pipe in a timely manner.");
		//}

		await pipeConnected;

		var rpc = new JsonRpc(rpcStream);
		ct.Register(rpc.Dispose);

		var devServer = new DevServer(this);
		rpc.AddLocalRpcTarget<IDevServer>(devServer, null);
		var unoWatch = rpc.Attach<IUnoWatch>();

		rpc.StartListening();

		
	}

	private class DevServer(ServerHotReloadProcessor owner) : IDevServer
	{
		public async Task<ApplyStatus> ApplyManagedCodeUpdatesAsync(ImmutableArray<HotReloadManagedCodeUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken)
		{
			await SendUpdates(updates);
			
			return ApplyStatus.AllChangesApplied;
		}

		async Task SendUpdates(ImmutableArray<HotReloadManagedCodeUpdate> updates)
		{
			for (var i = 0; i < updates.Length; i++)
			{
				//if (_useHotReloadThruDebugger)
				//{
				//	await _remoteControlServer.SendMessageToIDEAsync(
				//		new Uno.UI.RemoteControl.Messaging.IdeChannel.HotReloadThruDebuggerIdeMessage(
				//			updates[i].ModuleId.ToString(),
				//			Convert.ToBase64String(updates[i].MetadataDelta.ToArray()),
				//			Convert.ToBase64String(updates[i].ILDelta.ToArray()),
				//			Convert.ToBase64String(updates[i].PdbDelta.ToArray())
				//		));
				//}
				//else
				//{
					var updateTypesWriterStream = new MemoryStream();
					var updateTypesWriter = new BinaryWriter(updateTypesWriterStream);
					WriteIntArray(updateTypesWriter, updates[i].UpdatedTypes.ToArray());

					await owner._remoteControlServer.SendFrame(
						new AssemblyDeltaReload
						{
							FilePaths = [],
							ModuleId = updates[i].ModuleId.ToString(),
							PdbDelta = Convert.ToBase64String(updates[i].PdbDelta.ToArray()),
							ILDelta = Convert.ToBase64String(updates[i].ILDelta.ToArray()),
							MetadataDelta = Convert.ToBase64String(updates[i].MetadataDelta.ToArray()),
							UpdatedTypes = Convert.ToBase64String(updateTypesWriterStream.ToArray()),
						});
				//}
			}
		}
		static void WriteIntArray(BinaryWriter binaryWriter, int[] values)
		{
			if (values is null)
			{
				binaryWriter.Write(0);
				return;
			}

			binaryWriter.Write(values.Length);
			foreach (var value in values)
			{
				binaryWriter.Write(value);
			}
		}

		public async Task<ApplyStatus> ApplyStaticAssetUpdatesAsync(ImmutableArray<HotReloadStaticAssetUpdate> updates, bool isProcessSuspended, CancellationToken cancellationToken)
		{
			return ApplyStatus.AllChangesApplied;
		}
	}
}
