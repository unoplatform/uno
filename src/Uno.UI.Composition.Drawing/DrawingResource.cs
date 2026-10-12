#nullable enable

using System;
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
	private int _creatorDone;

	public void AddRef() => Interlocked.Increment(ref _refCount);

	public void Release()
	{
		if (Interlocked.Decrement(ref _refCount) == 0)
		{
			Free();
			// Released properly, so the backends that keep a finalizer as a backstop no longer need theirs.
			GC.SuppressFinalize(this);
		}
	}

	// Drops the creation reference, once: aliased straight onto Release, a second Dispose would decrement again and
	// free the resource while a recording still referenced it.
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _creatorDone, 1) == 0)
		{
			Release();
		}
	}

	/// <summary>Releases the backing native or GPU resource. Called once, when the last reference goes away.</summary>
	protected abstract void Free();
}
