#nullable enable

using System;
using System.Collections.Generic;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The result of decoding an image (or uploading raw pixels) through <see cref="IDrawingBackend"/>: one or more
/// decoded frames plus their durations. Owns the frames' lifetime — dispose it to release the backing resources.
/// A still image has a single frame; an animated image (GIF/APNG/WebP) has several.
/// </summary>
internal interface IImageFrames : IDisposable
{
	/// <summary>The decoded frames, in display order. Never empty.</summary>
	IReadOnlyList<IImage> Frames { get; }

	/// <summary>Per-frame display duration in milliseconds; same length as <see cref="Frames"/>.</summary>
	IReadOnlyList<int> DurationsMs { get; }
}
