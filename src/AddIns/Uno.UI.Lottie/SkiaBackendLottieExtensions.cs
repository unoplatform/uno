#nullable enable

#if HAS_SKOTTIE

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Skia;

/// <summary>
/// Adds the Skottie renderer to <see cref="SkiaBackend"/> when this add-in is referenced. The host builder finds it by
/// name and makes it the default <see cref="ILottieRenderer"/>, over the managed engine; a head whose image is trimmed
/// or AOT-compiled cannot resolve it that way, so those register it explicitly
/// (<c>builder.LottieRenderer(SkiaBackend.CreateLottieRenderer())</c>). An explicit registration wins over both.
/// </summary>
public static class SkiaBackendLottieExtensions
{
	extension(SkiaBackend)
	{
		/// <summary>Creates the Skottie <see cref="ILottieRenderer"/>.</summary>
		public static ILottieRenderer CreateLottieRenderer() => new global::Uno.UI.Lottie.SkottieLottieRenderer();
	}
}

#endif
