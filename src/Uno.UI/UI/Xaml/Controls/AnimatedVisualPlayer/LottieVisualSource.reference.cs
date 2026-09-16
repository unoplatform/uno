#nullable disable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	partial class LottieVisualSourceBase
	{
		private partial IAnimatedVisual2 CreatePendingAnimatedVisual(Compositor compositor)
			=> new ReferenceAnimatedVisual(compositor);

		private partial IAnimatedVisual2 TryCreateAnimatedVisualFromJson(Compositor compositor, string animationJson, bool createAnimations, out object diagnostics)
		{
			diagnostics = new NotSupportedException("Lottie visuals are not materialized by the reference assembly.");
			return null;
		}

		private sealed class ReferenceAnimatedVisual(Compositor compositor) : IAnimatedVisual2
		{
			public Visual RootVisual { get; } = compositor.CreateContainerVisual();

			public Vector2 Size { get; } = Vector2.Zero;

			public TimeSpan Duration { get; } = TimeSpan.Zero;

			public void CreateAnimations()
			{
			}

			public void DestroyAnimations()
			{
			}

			public void Dispose()
			{
			}
		}
	}
}
