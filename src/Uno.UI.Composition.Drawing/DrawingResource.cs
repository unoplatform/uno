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
			// A backend whose handles would otherwise be stranded by a missed Release keeps a finalizer as the
			// backstop; releasing properly is the common path, so take it back off the finalization queue here.
			GC.SuppressFinalize(this);
		}
	}

	// Drops the creation reference, once. Aliasing this straight onto Release would break the idempotence
	// IDisposable promises: a second Dispose would decrement a second time and free the resource while a recording
	// still referenced it.
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
