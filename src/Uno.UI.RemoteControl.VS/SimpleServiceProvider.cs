using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.VS.Helpers;
using IVsAsyncDisposable = Microsoft.VisualStudio.Threading.IAsyncDisposable;

namespace Uno.UI.RemoteControl.VS;

internal sealed class SimpleServiceProvider(ILogger? log = null) : IServiceProvider, IAsyncDisposable
{
	private ImmutableList<(Type contract, object instance)> _services = ImmutableList<(Type contract, object instance)>.Empty;

	public void Register(Type contract, object instance)
	{
		if (contract is null)
		{
			throw new ArgumentNullException(nameof(contract));
		}
		if (instance is null)
		{
			throw new ArgumentNullException(nameof(instance));
		}

		ImmutableInterlocked.Update(
			ref _services,
			static (services, entry) => services.RemoveAll(s => s.contract == entry.contract).Add(entry),
			(contract, instance));
	}

	public void Register<T>(T instance)
		where T : notnull
		=> Register(typeof(T), instance);

	/// <inheritdoc />
	public object? GetService(Type serviceType)
	{
		foreach (var (contract, instance) in _services)
		{
			if (contract == serviceType)
			{
				return instance;
			}
		}

		return null;
	}

	/// <summary>
	/// Disposes the registered services in reverse registration order, awaiting async ones and isolating failures.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		var services = Interlocked.Exchange(ref _services, ImmutableList<(Type contract, object instance)>.Empty);

		for (var i = services.Count - 1; i >= 0; i--)
		{
			var (contract, instance) = services[i];
			try
			{
				switch (instance)
				{
					case IAsyncDisposable asyncDisposable:
						await asyncDisposable.DisposeAsync();
						break;

					case IVsAsyncDisposable vsAsyncDisposable:
						await vsAsyncDisposable.DisposeAsync();
						break;

					case IDisposable disposable:
						disposable.Dispose();
						break;
				}
			}
			catch (Exception e)
			{
				log?.Error($"Failed to dispose service {contract.Name}: {e}");
			}
		}
	}
}
