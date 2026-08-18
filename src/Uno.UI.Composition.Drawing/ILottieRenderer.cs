#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// App-registerable seam for turning Lottie (Bodymovin JSON) markup into a renderable, seekable animation,
/// independent of the graphics backend: the returned <see cref="ILottieAnimation"/> draws through the neutral
/// <see cref="IDrawingSession"/>. Register via the host builder; the default (the Skottie add-in when present) is
/// resolved at Build() time, and Lottie playback is unavailable when none is registered.
/// </summary>
public interface ILottieRenderer
{
	/// <summary>Parses a Lottie animation from its JSON, or null when the text isn't an animation the renderer can handle.</summary>
	ILottieAnimation? Load(string animationJson);
}

/// <summary>
/// A parsed, seekable Lottie animation — the retained representation. <see cref="Render"/> replays it at a given
/// progress into any <see cref="IDrawingSession"/>. The animation owns no backend resources; the caller supplies the session.
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
/// add-in when present). Null when none is registered — Lottie then doesn't play and <c>AnimatedVisualPlayer</c>
/// shows its fallback content.
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
