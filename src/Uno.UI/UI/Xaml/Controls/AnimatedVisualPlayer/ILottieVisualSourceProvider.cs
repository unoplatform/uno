#nullable enable

using System;

// Keep this using in place until UWP support is dropped.
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides Lottie animated visual sources to Uno.UI, supplied by the Uno.UI.Lottie
	/// add-in through ApiExtensibility.
	/// </summary>
	public interface ILottieVisualSourceProvider
	{
		/// <summary>
		/// Creates an animated visual source from a Lottie JSON asset.
		/// </summary>
		/// <param name="sourceFile">The URI of the Lottie JSON asset.</param>
		/// <returns>An animated visual source backed by the specified asset.</returns>
		IAnimatedVisualSource CreateFromLottieAsset(Uri sourceFile);

		/// <summary>
		/// Creates a themable animated visual source from a Lottie JSON asset.
		/// </summary>
		/// <param name="sourceFile">The URI of the Lottie JSON asset.</param>
		/// <returns>An animated visual source whose colors can be overridden per theme.</returns>
		IThemableAnimatedVisualSource CreateThemableFromLottieAsset(Uri sourceFile);

		/// <summary>
		/// Attempts to obtain a themable animated visual source for an existing animated visual source.
		/// </summary>
		/// <param name="animatedVisualSource">The animated visual source to convert.</param>
		/// <param name="themableAnimatedVisualSource">When this method returns, the themable source, or <c>null</c> if none could be produced.</param>
		/// <returns><c>true</c> if a themable source was produced; otherwise, <c>false</c>.</returns>
		bool TryCreateThemableFromAnimatedVisualSource(IAnimatedVisualSource animatedVisualSource, out IThemableAnimatedVisualSource? themableAnimatedVisualSource);
	}
}
