#nullable enable

using System.Threading;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The shared reference-counted lifetime for <see cref="IDrawingResource"/>. A backend derives from this and frees
/// its native handle in <see cref="Free"/>, which runs exactly once, when the last reference is dropped.
/// </summary>
public abstract class DrawingResource : IDrawingResource
{
	// Creation counts as the creator's own reference, so a resource nobody shares is freed by its Dispose alone.
	private int _refCount = 1;

	public void AddRef() => Interlocked.Increment(ref _refCount);

	public void Release()
	{
		if (Interlocked.Decrement(ref _refCount) == 0)
		{
			Free();
		}
	}

	public void Dispose() => Release();

	/// <summary>Releases the backing native or GPU resource. Called once, when the last reference goes away.</summary>
	protected abstract void Free();
}
