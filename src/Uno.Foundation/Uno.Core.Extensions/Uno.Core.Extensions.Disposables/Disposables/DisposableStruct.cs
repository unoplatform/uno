namespace Uno.Disposables;

/// <summary>
/// This Disposable is used with using statements to imitate golang-style defer statements and RAII-like logic
/// without allocating or boxing.
/// </summary>
/// <param name="disposingAction">The action that is fired on disposing.</param>
/// <param name="argToAction">The argument supplied to the <paramref name="disposingAction"/></param>
public struct DisposableStruct<T>(Action<T> disposingAction, T argToAction) : IDisposable
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
