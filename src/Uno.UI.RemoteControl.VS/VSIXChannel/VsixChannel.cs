using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.VS;

/// <summary>
/// JSON-RPC channel to the Uno VSIX: exposes the services it provides and publishes the local ones it consumes.
/// </summary>
internal sealed class VsixChannel : IAsyncDisposable
{
	// The VSIX serves the pipe before handing over its name; a broken hand-off must not wait forever.
	private const int ConnectTimeout = 30_000;

	private readonly NamedPipeClientStream _stream;
	private readonly JsonRpc _rpc;
	private readonly SimpleServiceProvider _services;

	private VsixChannel(NamedPipeClientStream stream, JsonRpc rpc, SimpleServiceProvider services)
	{
		_stream = stream;
		_rpc = rpc;
		_services = services;
	}

	public IServiceProvider Services => _services;

	public static async Task<VsixChannel> ConnectAsync(
		string vsixChannelHandle,
		Type[] remoteServices,
		(Type type, object instance)[] localServices,
		ILogger logger,
		CancellationToken ct)
	{
		var stream = new NamedPipeClientStream(
			serverName: ".",
			pipeName: vsixChannelHandle,
			direction: PipeDirection.InOut,
			options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);

		JsonRpc? rpc = null;
		try
		{
			await stream.ConnectAsync(ConnectTimeout, ct).ConfigureAwait(false);

			rpc = new JsonRpc(stream);
			foreach (var service in localServices)
			{
				rpc.AddLocalRpcTarget(service.type, service.instance, null);
			}

			var services = new SimpleServiceProvider(logger);
			foreach (var service in remoteServices)
			{
				services.Register(service, rpc.Attach(service));
			}

			rpc.StartListening();

			return new VsixChannel(stream, rpc, services);
		}
		catch
		{
			rpc?.Dispose();
			stream.Dispose();
			throw;
		}
	}

	// Thread-agnostic: nothing here is UI-thread-affine.
	public async ValueTask DisposeAsync()
	{
		await _services.DisposeAsync();
		_rpc.Dispose();
		_stream.Dispose();
	}
}
