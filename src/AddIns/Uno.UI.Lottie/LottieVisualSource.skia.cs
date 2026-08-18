#nullable enable

#if HAS_SKOTTIE

using System;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using SkiaSharp;
using SkiaSharp.SceneGraph;
using Uno.UI.Lottie;
using Uno.WinUI.Graphics2DSK;

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
			private readonly InvalidationController _invalidationController = new();
			private readonly LottieCanvasElement _canvasElement;
			private readonly Visual _rootVisual;

			private SkiaSharp.Skottie.Animation? _animation;
			private bool _isDisposed;

			private LottieAnimatedVisual(Compositor compositor)
			{
				_canvasElement = new LottieCanvasElement(this);
				ElementCompositionPreview.SetElementVisualCompositor(_canvasElement, compositor);
				_rootVisual = ElementCompositionPreview.GetElementVisual(_canvasElement);
				_rootVisual.Properties.InsertScalar("Progress", 0.0f);
				_rootVisual.Properties.AddContext(_rootVisual, null);
				_invalidationController.Begin();
			}

			public Visual RootVisual => _rootVisual;

			public Vector2 Size { get; private set; }

			public TimeSpan Duration { get; private set; }

			public static LottieAnimatedVisual CreatePending(Compositor compositor)
				=> new(compositor);

			public static IAnimatedVisual2? TryCreate(Compositor compositor, string animationJson, out object diagnostics)
			{
				try
				{
					var animation = CreateAnimation(animationJson);
					if (animation is null)
					{
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
					diagnostics = e;
					return null;
				}
			}

			public void CreateAnimations()
			{
				// Skottie renders directly from the current Progress scalar each frame and does not
				// materialize separate composition animation objects that need to be created up front.
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
					_invalidationController.End();
					_animation?.Dispose();
					_animation = null;
				}
			}

			private static SkiaSharp.Skottie.Animation? CreateAnimation(string animationJson)
			{
				using var stream = new Utf8StringStream(animationJson);
				return SkiaSharp.Skottie.Animation.TryCreate(stream, out var animation)
					? animation
					: null;
			}

			private void Initialize(SkiaSharp.Skottie.Animation animation)
			{
				_animation = animation;
				Size = new Vector2(animation.Size.Width, animation.Size.Height);
				Duration = animation.Duration;
				_rootVisual.Size = Size;
				_canvasElement.Invalidate();
			}

			private void Render(SKCanvas canvas)
			{
				lock (_gate)
				{
					if (_isDisposed || _animation is null)
					{
						return;
					}

					canvas.Clear(SKColors.Transparent);

					var progress = GetProgress();
					var frameTime = TimeSpan.FromTicks((long)(Duration.Ticks * progress));
					_animation.SeekFrameTime(frameTime, _invalidationController);
					_animation.Render(canvas, new SKRect(0, 0, _animation.Size.Width, _animation.Size.Height));
					_invalidationController.Reset();
				}
			}

			private float GetProgress()
			{
				return _rootVisual.Properties.TryGetScalar("Progress", out var progress) == CompositionGetValueStatus.Succeeded
					? Math.Clamp(progress, 0.0f, 1.0f)
					: 0.0f;
			}

			private sealed class LottieCanvasElement(LottieAnimatedVisual owner) : SKCanvasElement
			{
				protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
					=> owner.Render(canvas);
			}
		}
	}
}

#endif
