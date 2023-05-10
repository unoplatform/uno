using System;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Media;

/// <summary>
/// Disposable that returns a <see cref="ManagedWeakReference"/> to the pool.
/// </summary>
internal struct BrushChangedDisposable : IDisposable
{
	private readonly Brush _brush;
	private readonly WeakReferenceReturnDisposable _callbackDisposable;

	public BrushChangedDisposable(Brush brush, WeakReferenceReturnDisposable callbackDisposable)
	{
		_brush = brush;
		_callbackDisposable = callbackDisposable;
	}

	public void Dispose()
	{
		_callbackDisposable.Dispose();
	}
}
