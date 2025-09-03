using System;
using System.Collections.Immutable;
using IAsyncDisposable = Microsoft.VisualStudio.Threading.IAsyncDisposable;

namespace Uno.UI.RemoteControl.VS;

internal class SimpleServiceProvider : IServiceProvider, IDisposable
{
	private ImmutableDictionary<Type, object> _services = ImmutableDictionary<Type, object>.Empty;

	public void Register(Type contract, object instance)
		=> ImmutableInterlocked.AddOrUpdate(ref _services, contract, instance, (_, __) => instance);

	public void Register<T>(T instance)
		=> ImmutableInterlocked.AddOrUpdate(ref _services, typeof(T), instance ?? throw new ArgumentNullException(nameof(instance)), (_, __) => instance);

	/// <inheritdoc />
	public virtual object? GetService(Type serviceType)
		=> _services.TryGetValue(serviceType, out var instance) ? instance : null;

	/// <inheritdoc />
	public void Dispose()
	{
		foreach (var service in _services.Values)
		{
			switch (service)
			{
				case IDisposable disposable:
					disposable.Dispose();
					break;

				case IAsyncDisposable asyncDisposable:
					_ = asyncDisposable.DisposeAsync();
					break;
			}
		}
	}
}
