#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	partial class LottieVisualSourceBase
	{
		private partial IAnimatedVisual2 CreatePendingAnimatedVisual(Compositor compositor)
			=> LottieAnimatedVisual.CreatePending(compositor);

		private partial IAnimatedVisual2? TryCreateAnimatedVisualFromJson(Compositor compositor, string animationJson, bool createAnimations, out object diagnostics)
			=> LottieAnimatedVisual.TryCreate(compositor, animationJson, out diagnostics);

		private sealed class LottieAnimatedVisual : IAnimatedVisual2
		{
			private readonly object _gate = new();
			private readonly LottieContentVisual _rootVisual;

			private ILottieAnimation? _animation;
			private bool _isDisposed;

			private LottieAnimatedVisual(Compositor compositor)
			{
				_rootVisual = new LottieContentVisual(this, compositor);
				// The player drives this same "Progress" scalar (via an expression animation bound to its own
				// Progress); AddContext repaints the visual each time it ticks.
				_rootVisual.Properties.InsertScalar("Progress", 0.0f);
				_rootVisual.Properties.AddContext(_rootVisual, null);
			}

			public Visual RootVisual => _rootVisual;

			public Vector2 Size { get; private set; }

			public TimeSpan Duration { get; private set; }

			public static LottieAnimatedVisual CreatePending(Compositor compositor)
				=> new(compositor);

			public static IAnimatedVisual2? TryCreate(Compositor compositor, string animationJson, out object diagnostics)
			{
				if (LottieRenderer.Current is not { } renderer)
				{
					// No Lottie renderer registered (the Skottie add-in wasn't referenced / resolved): the player shows
					// its fallback content rather than silently rendering nothing — but log so the dev isn't left guessing.
					if (typeof(LottieAnimatedVisual).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(LottieAnimatedVisual).Log().Warn("No ILottieRenderer is registered (reference the Uno.UI.Lottie add-in or call .LottieRenderer(...)); Lottie playback is unavailable and the player will show its fallback content.");
					}

					diagnostics = new InvalidOperationException("No ILottieRenderer is registered; Lottie playback is unavailable.");
					return null;
				}

				try
				{
					if (renderer.Load(animationJson) is not { } animation)
					{
						if (typeof(LottieAnimatedVisual).Log().IsEnabled(LogLevel.Warning))
						{
							typeof(LottieAnimatedVisual).Log().Warn("The Lottie renderer could not load the animation (unrecognized/invalid JSON); the player will show its fallback content.");
						}

						diagnostics = new InvalidOperationException("Failed to load animation.");
						return null;
					}

					var visual = new LottieAnimatedVisual(compositor);
					visual.Initialize(animation);
					diagnostics = null!;
					return visual;
				}
				catch (Exception e)
				{
					if (typeof(LottieAnimatedVisual).Log().IsEnabled(LogLevel.Error))
					{
						typeof(LottieAnimatedVisual).Log().Error("The Lottie renderer threw while loading the animation.", e);
					}

					diagnostics = e;
					return null;
				}
			}

			public void CreateAnimations()
			{
				// The renderer draws directly from the current Progress scalar each frame; there are no separate
				// composition-side animation objects to create up front.
			}

			public void DestroyAnimations()
			{
				// See CreateAnimations(). There are no separate composition-side animations to tear down.
			}

			public void Dispose()
			{
				lock (_gate)
				{
					if (_isDisposed)
					{
						return;
					}

					_isDisposed = true;
					_rootVisual.Properties.RemoveContext(_rootVisual, null);
					_animation?.Dispose();
					_animation = null;
				}
			}

			private void Initialize(ILottieAnimation animation)
			{
				_animation = animation;
				Size = animation.Size;
				Duration = animation.Duration;
				_rootVisual.Size = Size;
				_rootVisual.Invalidate();
			}

			// Draws the current frame through the neutral drawing session — the renderer picks the fast (SKCanvas) or
			// texture path for the active backend, so this is backend-agnostic.
			private void Render(in Visual.PaintingSession paintingSession)
			{
				lock (_gate)
				{
					if (_isDisposed || _animation is not { } animation)
					{
						return;
					}

					var session = paintingSession.Session;
					var area = new Rect(0, 0, _rootVisual.Size.X, _rootVisual.Size.Y);
					var save = session.Save();
					session.ClipRect(area);
					animation.Render(session, GetProgress(), area);
					session.RestoreToCount(save);
				}
			}

			private float GetProgress()
				=> _rootVisual.Properties.TryGetScalar("Progress", out var progress) == CompositionGetValueStatus.Succeeded
					? Math.Clamp(progress, 0.0f, 1.0f)
					: 0.0f;

			// The animated content's composition visual: paints the current Lottie frame through the neutral session.
			private sealed class LottieContentVisual : ContainerVisual
			{
				private readonly LottieAnimatedVisual _owner;

				public LottieContentVisual(LottieAnimatedVisual owner, Compositor compositor) : base(compositor)
					=> _owner = owner;

				internal override bool CanPaint() => true;

				internal override IGeometry? Paint(in PaintingSession session)
				{
					_owner.Render(session);
					return null;
				}

				public void Invalidate() => Compositor.InvalidateRender(this);
			}
		}
	}
}
