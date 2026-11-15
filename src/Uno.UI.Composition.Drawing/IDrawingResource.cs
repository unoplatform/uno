#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Base for the backend-created drawing objects the framework has to keep alive: each wraps a native or GPU resource,
/// is immutable once built, and is shared — a brush caches one while any number of recordings still reference it.
/// <para>
/// Reference counting is what makes those two facts compatible. Sharing costs nothing (no snapshot per reference,
/// and caches keyed on identity keep working), and release stays deterministic — these are a few managed bytes in
/// front of an arbitrarily large native allocation, which GC pressure heuristics cannot see, so leaving them to
/// finalization frees them arbitrarily late.
/// </para>
/// <para>
/// The two verbs have different audiences. Creation yields one reference and whoever created it calls
/// <see cref="IDisposable.Dispose"/> — once, so ownership still reads as <c>using</c> at the call site. Anyone
/// holding a resource they did not create took it with <see cref="AddRef"/> and hands it back with
/// <see cref="Release"/>. Neither frees anything on its own; the resource goes away when the last reference does, so
/// never dispose a resource you merely borrowed. <see cref="IFont"/> is deliberately not one of these: fonts are
/// provider-cached for the life of the process and never released.
/// </para>
/// </summary>
public interface IDrawingResource : IDisposable
{
	/// <summary>Takes a reference, keeping the resource alive until the matching <see cref="Release"/>.</summary>
	void AddRef();

	/// <summary>Drops a reference. The backing resource is freed once no references remain.</summary>
	void Release();
}
