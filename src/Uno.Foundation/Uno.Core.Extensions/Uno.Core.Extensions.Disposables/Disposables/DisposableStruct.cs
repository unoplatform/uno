using System;

namespace Uno.Disposables;

#pragma warning disable CA1815 // Override equals and operator equals on value types

/// <summary>
/// This Disposable is used with using statements to imitate golang-style defer statements and RAII-like logic
/// without allocating or boxing.
/// </summary>
/// <remarks>
/// Make sure that the <paramref name="disposingAction"/> doesn't capture anything (or even better, is static)
/// to avoid allocations. If you need multiple parameters for the <paramref name="disposingAction"/>, you can
/// bundle them in a tuple.
/// </remarks>
/// <param name="disposingAction">The action that is fired on disposing.</param>
/// <param name="argToAction">The argument supplied to the <paramref name="disposingAction"/></param>
internal struct DisposableStruct<T>(Action<T> disposingAction, T argToAction) : IDisposable
{
	private bool _disposed;

	public void Dispose()
	{
		if (_disposed)
		{
			throw new InvalidOperationException($"Disposing {nameof(DisposableStruct<T>)} twice");
		}
		_disposed = true;
		disposingAction(argToAction);
	}
}
