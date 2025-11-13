#nullable enable

#if HAS_SKOTTIE

// SkiaSharp.Views.Uno uses the underlying canvas for hardware acceleration.
#if !__SKIA__ && !__MACCATALYST__
#define USE_HARDWARE_ACCELERATION
#endif

using System;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Uno.Disposables;
using SkiaSharp;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;
using Windows.System;
using System.Diagnostics;
using SkiaSharp.SceneGraph;
using Microsoft.UI.Xaml.Media;
using System.Text;
using System.IO;

#if __SKIA__
using Uno.WinUI.Graphics2DSK;
#endif

#if HAS_UNO_WINUI
using SkiaSharp.Views.Windows;
using Windows.UI.Core;
#else
using SkiaSharp.Views.UWP;
using Microsoft.UI.Xaml.Controls;
#endif

#if HAS_UNO_WINUI
namespace CommunityToolkit.WinUI.Lottie
#else
namespace Microsoft.Toolkit.Uwp.UI.Lottie
#endif
{
	partial class LottieVisualSourceBase
	{
		private UIElement? _renderSurface;
		private SkiaSharp.Skottie.Animation? _animation;


		private bool _wasPlaying;
		private DispatcherQueueTimer? _timer;
		private object _gate = new();

#if USE_HARDWARE_ACCELERATION
		private SKSwapChainPanel? _hardwareCanvas;
#elif __SKIA__
		private SKCanvasElement? _skCanvasElement;
#else
		private SKXamlCanvas? _softwareCanvas;
#endif

		private Uri? _lastSource;
		private PlayState? _playState;

		private record PlayState(double FromProgress, double ToProgress, bool Looped)
		{
			public TimeSpan GetFromProgressUsingDuration(TimeSpan duration)
				=> TimeSpan.FromSeconds(duration.TotalSeconds * FromProgress);

			public TimeSpan GetToProgressUsingDuration(TimeSpan duration)
				=> TimeSpan.FromSeconds(duration.TotalSeconds * ToProgress);
		}

		private Stopwatch _stopwatch = new Stopwatch();
		private TimeSpan? _progress;

		private InvalidationController? _invalidationController;
		private readonly SerialDisposable _animationDataSubscription = new SerialDisposable();

		async Task InnerUpdate(CancellationToken ct)
		{
			var player = _player;

			if (player == null)
			{
				return;
			}

			await SetProperties();

			async Task SetProperties()
			{
				try
				{
					var sourceUri = UriSource;
					if (_lastSource == null || !_lastSource.Equals(sourceUri))
					{
						_lastSource = sourceUri;
						if ((await TryLoadDownloadJson(sourceUri, ct)) is { } jsonStream)
						{
							var cacheKey = sourceUri.OriginalString;
							_animationDataSubscription.Disposable = null;
							_animationDataSubscription.Disposable =
								LoadAndObserveAnimationData(jsonStream, cacheKey, OnJsonChanged);

							void OnJsonChanged(string updatedJson, string updatedCacheKey)
							{
								try
								{
									var stream = new MemoryStream(Encoding.UTF8.GetBytes(updatedJson));

									if (SkiaSharp.Skottie.Animation.TryCreate(stream, out var animation))
									{
										animation.Seek(0);

										if (this.Log().IsEnabled(LogLevel.Debug))
										{
											this.Log().Debug($"Version: {animation.Version} Duration: {animation.Duration} Fps:{animation.Fps} InPoint: {animation.InPoint} OutPoint: {animation.OutPoint}");
										}
									}
									else
									{
										throw new InvalidOperationException("Failed to load animation.");
									}

									SetAnimation(animation);

									if (_playState != null)
									{
										var (fromProgress, toProgress, looped) = _playState;
										Play(fromProgress, toProgress, looped);
									}
								}
								catch (Exception ex)
								{
									throw new InvalidOperationException("Failed load the animation", ex);
								}
							}
						}
						else
						{
							throw new NotSupportedException($"Failed to load animation: {sourceUri}");
						}

						// Force layout to recalculate
						player.InvalidateMeasure();
						player.InvalidateArrange();

						if (_playState != null)
						{
							var (fromProgress, toProgress, looped) = _playState;
							Play(fromProgress, toProgress, looped);
						}
						else if (player.AutoPlay)
						{
							Play(0, 1, true);
						}

					}

					if (_animation == null)
					{
						return;
					}

					var duration = _animation.Duration;
					player.SetValue(AnimatedVisualPlayer.DurationProperty, duration);

					var isLoaded = duration > TimeSpan.Zero;
					player.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, isLoaded);

					Invalidate();
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to update lottie player for [{UriSource}]", e);
					}
				}
			}
		}

		private void SetAnimation(SkiaSharp.Skottie.Animation animation)
		{
			if (!ReferenceEquals(_animation, animation))
			{
#if __APPLE_UIKIT__
				_renderSurface?.RemoveFromSuperview();
#elif __SKIA__ || __ANDROID__
				_player?.RemoveChild(_renderSurface);
#endif
			}

			_renderSurface = BuildRenderSurface();

#if __APPLE_UIKIT__
			_player?.Add(_renderSurface);
#elif __SKIA__ || __ANDROID__
			_player?.AddChild(_renderSurface);
#endif

			_animation = animation;
		}

		private UIElement BuildRenderSurface()
		{
			ClearRenderSurface();

#if USE_HARDWARE_ACCELERATION
			_hardwareCanvas = new();
			_hardwareCanvas.PaintSurface += OnHardwareCanvas_PaintSurface;

#if __ANDROID__ || __APPLE_UIKIT__
			AdjustHardwareCanvasOpacity();
#endif
			return _hardwareCanvas;
#elif __SKIA__
			_skCanvasElement = new LottieSKCanvasElement(this);
			return _skCanvasElement;
#else
			_softwareCanvas = new();
			_softwareCanvas.PaintSurface += OnSoftwareCanvas_PaintSurface;

#if __APPLE_UIKIT__
			_softwareCanvas.Opaque = false;
#endif
			return _softwareCanvas;
#endif
		}

#if USE_HARDWARE_ACCELERATION && (__ANDROID__ || __APPLE_UIKIT__)
		private void AdjustHardwareCanvasOpacity()
		{
			if (_hardwareCanvas != null)
			{
				static void UpdateTransparency(object s, object e)
				{
					if (s is SKSwapChainPanel swapChainPanel)
					{

						// The SKGLTextureView is opaque by default, so we poke at the tree
						// to change the opacity of the first view of the SKSwapChainPanel
						// to make it transparent.
#if __ANDROID__
						if (swapChainPanel.ChildCount == 1
							&& swapChainPanel.GetChildAt(0) is Android.Views.TextureView texture)
						{
							texture.SetOpaque(false);
						}
#elif __APPLE_UIKIT__
						if (swapChainPanel.Subviews.Length == 1
							&& swapChainPanel.Subviews[0] is GLKit.GLKView texture)
						{
							texture.Opaque = false;
						}
#endif
					}
				}

				_hardwareCanvas.Loaded += UpdateTransparency;
			}
		}
#endif

		private void ClearRenderSurface()
		{
#if USE_HARDWARE_ACCELERATION
			if (_hardwareCanvas != null)
			{
				_hardwareCanvas.PaintSurface -= OnHardwareCanvas_PaintSurface;
			}
#elif !__SKIA__
			if (_softwareCanvas != null)
			{
				_softwareCanvas.PaintSurface -= OnSoftwareCanvas_PaintSurface;
			}
#endif
		}

		private void OnSoftwareCanvas_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
		{
			Render(e.Surface.Canvas, saveRestoreAndCleanCanvas: true);
		}

		private void OnHardwareCanvas_PaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
		{
			Render(e.Surface.Canvas, saveRestoreAndCleanCanvas: true);
		}

#if __SKIA__
		private void OnRenderOverride(SKCanvas canvas, Size area)
		{
			Render(canvas, saveRestoreAndCleanCanvas: false);
		}
#endif

		private void Render(SKCanvas canvas, bool saveRestoreAndCleanCanvas)
		{
			lock (_gate)
			{
				var animation = _animation;
				if (animation is null || _player is null)
				{
					return;
				}

				if (_invalidationController is null)
				{
					_invalidationController = new SkiaSharp.SceneGraph.InvalidationController();
					_invalidationController.Begin();
				}

				var frameTime = GetFrameTime();

				var localSize = canvas.LocalClipBounds.Size;

				var scale = ImageSizeHelper.BuildScale(_player.Stretch, localSize.ToSize(), animation.Size.ToSize());
				var scaledSize = new Windows.Foundation.Size(animation.Size.Width * scale.x, animation.Size.Height * scale.y);

				var x = (localSize.Width - scaledSize.Width) / 2;
				var y = (localSize.Height - scaledSize.Height) / 2;

				animation.SeekFrameTime(frameTime, _invalidationController);

				if (saveRestoreAndCleanCanvas)
				{
					canvas.Save();
					canvas.Clear(GetBackgroundColor());
				}

				canvas.Translate((float)x, (float)y);
				canvas.Scale((float)(scaledSize.Width / animation.Size.Width), (float)(scaledSize.Height / animation.Size.Height));

				animation.Render(canvas, new SKRect(0, 0, animation.Size.Width, animation.Size.Height));

				if (saveRestoreAndCleanCanvas)
				{
					canvas.Restore();
				}

				_invalidationController.Reset();
			}
		}

		private SKColor GetBackgroundColor()
		{
			if (_player?.Background is SolidColorBrush sb)
			{
				return new SKColor(alpha: sb.ColorWithOpacity.A, red: sb.ColorWithOpacity.R, green: sb.ColorWithOpacity.G, blue: sb.ColorWithOpacity.B);
			}

			return SKColors.Transparent;
		}

		private TimeSpan GetFrameTime()
		{
			if (_animation is null || _timer is null || !(_playState is { } playState) || _player is null)
			{
				return _progress ?? TimeSpan.Zero;
			}

			var frameTime = TimeSpan.FromSeconds((_stopwatch.Elapsed + playState.GetFromProgressUsingDuration(_animation.Duration)).TotalSeconds * _player.PlaybackRate);

			if (frameTime > playState.GetToProgressUsingDuration(_animation.Duration))
			{
				if (playState.Looped)
				{
					_stopwatch.Restart();
					_invalidationController?.End();
					_invalidationController?.Begin();
				}
				else
				{
					// Free the animation at the "to" progress value
					_progress = frameTime;

					Stop();
				}
			}

			return frameTime;
		}

		public void Play(double fromProgress, double toProgress, bool looped)
		{

			if (_animation != null)
			{
				if (_stopwatch.IsRunning)
				{
					Stop();
				}

				_playState = new(fromProgress, toProgress, looped);

				_progress = null;

				_timer = Windows.System.DispatcherQueue.GetForCurrentThread().CreateTimer();
				_timer.Tick += (s, e) => Invalidate();

				_timer.Interval = TimeSpan.FromSeconds(Math.Max(1 / 120d, 1 / _animation.Fps));
				_timer.Start();
				_stopwatch.Restart();

				SetIsPlaying(true);
			}
			else
			{
				_playState = new(fromProgress, toProgress, looped);
			}
		}

		private void Invalidate()
		{
#if USE_HARDWARE_ACCELERATION
			_hardwareCanvas?.Invalidate();
#elif __SKIA__
			_skCanvasElement?.Invalidate();
#else
			_softwareCanvas?.Invalidate();
#endif
		}

		public void Stop()
		{
			void DoStop()
			{
				_playState = null;
				SetIsPlaying(false);
				_timer?.Stop();
				_stopwatch.Stop();
				_invalidationController?.End();
			}

			if (Dispatcher.HasThreadAccess)
			{
				DoStop();
			}
			else
			{
				_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, DoStop);
			}
		}

		public void Pause()
		{
			_timer?.Stop();
			_stopwatch.Stop();

			SetIsPlaying(false);
		}

		public void Resume()
		{
			_stopwatch.Start();
			_timer?.Start();

			SetIsPlaying(true);
		}

		public void SetProgress(double progress)
		{
			var clampedProgress = Math.Max(0, Math.Min(1, progress));

			if (_animation != null)
			{
				Stop();
				_progress = TimeSpan.FromSeconds(_animation.Duration.TotalSeconds * clampedProgress);
				Invalidate();
			}
		}

		public void Load()
		{
			if (_wasPlaying)
			{
				_wasPlaying = false;
				Resume();
			}
		}

		public void Unload()
		{
			if (_player?.IsPlaying ?? false)
			{
				_wasPlaying = true;
				Pause();
			}
		}

		private Size CompositionSize
			=> _animation?.Size is { } size
				? new Size(size.Width, size.Height)
				: default;

#if __SKIA__
		private partial class LottieSKCanvasElement(LottieVisualSourceBase owner) : SKCanvasElement
		{
			protected override void RenderOverride(SKCanvas canvas, Size area) => owner.OnRenderOverride(canvas, area);
		}
#endif
	}
}
#endif
