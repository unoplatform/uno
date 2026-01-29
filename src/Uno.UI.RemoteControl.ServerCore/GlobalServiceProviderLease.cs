using System;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Tracks the lifetime of the global (host-level) <see cref="IServiceProvider"/>.
/// </summary>
internal sealed class GlobalServiceProviderLease : IAsyncDisposable
{
	private readonly bool _disposeProvider;

	public GlobalServiceProviderLease(IServiceProvider serviceProvider, bool disposeProvider)
	{
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_disposeProvider = disposeProvider;
	}

	/// <summary>
	/// Gets the root service provider shared across all devserver connections.
	/// </summary>
	public IServiceProvider ServiceProvider { get; }

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (!_disposeProvider)
		{
			return;
		}

		switch (ServiceProvider)
		{
			case IAsyncDisposable asyncDisposable:
				await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				break;
			case IDisposable disposable:
				disposable.Dispose();
				break;
		}
	}
}
