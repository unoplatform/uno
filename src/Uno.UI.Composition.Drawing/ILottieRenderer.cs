#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// App-registerable seam for turning Lottie (Bodymovin JSON) markup into a renderable, seekable animation.
/// Independent of the graphics backend (like <see cref="ISvgRenderer"/> / <see cref="FontProvider"/>): the returned
/// <see cref="ILottieAnimation"/> draws through the neutral <see cref="IDrawingSession"/>, so it works under any
/// backend. Register via the host builder; the default (the Skottie add-in when present) is resolved at Build() time,
/// and Lottie playback is simply unavailable — the player shows its fallback content — when none is registered.
/// </summary>
public interface ILottieRenderer
{
	/// <summary>Parses a Lottie animation from its JSON, or null when the text isn't an animation the renderer can handle.</summary>
	ILottieAnimation? Load(string animationJson);
}

/// <summary>
/// A parsed, seekable Lottie animation — the <em>retained</em> representation. <see cref="Render"/> replays it at a
/// given progress into any <see cref="IDrawingSession"/> (a live per-frame session, or an offscreen to rasterize).
/// The animation owns no backend resources; the caller supplies the session (and thus the backend).
/// </summary>
public interface ILottieAnimation : IDisposable
{
	/// <summary>The animation's intrinsic composition size.</summary>
	Vector2 Size { get; }

	/// <summary>The animation's total duration (one loop).</summary>
	TimeSpan Duration { get; }

	/// <summary>
	/// Draws the animation's frame at <paramref name="progress"/> (0..1 of <see cref="Duration"/>) into
	/// <paramref name="session"/>, scaled to fill <paramref name="area"/>.
	/// </summary>
	void Render(IDrawingSession session, float progress, Rect area);
}

/// <summary>
/// Holds the registered <see cref="ILottieRenderer"/>, resolved by the host builder at Build() time (the Skottie
/// add-in when present). Null when no renderer is registered — Lottie then simply doesn't play, and the
/// <c>AnimatedVisualPlayer</c> falls back to its fallback content. Mirrors the <see cref="SvgRenderer"/> holder.
/// </summary>
public static class LottieRenderer
{
	private static ILottieRenderer? _current;

	/// <summary>The active Lottie renderer, or null on a head with no Lottie renderer at all.</summary>
	public static ILottieRenderer? Current
	{
		get => _current;
		internal set => _current = value;
	}

	/// <summary>Registers <paramref name="renderer"/> only if none is set yet (the resolved default).</summary>
	internal static void RegisterDefault(ILottieRenderer renderer) => _current ??= renderer;
}
