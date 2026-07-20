#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

/// <summary>
/// Builds the right <see cref="IFrameProvider"/> for a set of decoded <see cref="IImageFrames"/>: a still image
/// (single frame) gets a <see cref="SingleFrameProvider"/>, an animated one an <see cref="AnimatedImageFrameProvider"/>.
/// Decoding itself is the backend's job (<see cref="IDrawingBackend.TryDecodeImage"/>); this only picks the cadence.
/// </summary>
internal static class FrameProviderFactory
{
	public static IFrameProvider Create(IImageFrames frames, Action? onFrameChanged)
		=> frames.Frames.Count < 2
			? new SingleFrameProvider(frames)
			: new AnimatedImageFrameProvider(frames, onFrameChanged ?? (static () => { }));
}
