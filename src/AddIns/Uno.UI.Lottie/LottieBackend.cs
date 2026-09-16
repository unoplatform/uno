#nullable enable

#if HAS_SKOTTIE

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Lottie;

/// <summary>
/// Entry point to the Skottie-backed Lottie renderer. The host builder normally finds it reflectively, but a head
/// whose image is trimmed or AOT-compiled cannot resolve it that way, so those register it through here instead
/// (<c>builder.LottieRenderer(LottieBackend.CreateLottieRenderer())</c>).
/// </summary>
public static class LottieBackend
{
	/// <summary>Creates the Skottie <see cref="ILottieRenderer"/>. Returns the neutral seam, so callers need no
	/// reference to this assembly's internals.</summary>
	public static ILottieRenderer CreateLottieRenderer() => new SkottieLottieRenderer();
}

#endif
