using System;
using System.Globalization;
using System.Threading;
using Uno.Foundation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using System.Threading.Tasks;
using Uno.Disposables;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#endif

#pragma warning disable CA1305 // Specify IFormatProvider

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSourceBase
	{
		private AnimatedVisualPlayer? _initializedPlayer;
		private Uri? _lastSource;
		private Size _compositionSize = new Size(0, 0);

		private (double fromProgress, double toProgress, bool looped)? _playState;
		private bool _isUpdating;
		private bool _domLoaded;

		private readonly SerialDisposable _animationDataSubscription = new SerialDisposable();

		async Task InnerUpdate(CancellationToken ct)
		{
			var player = _player;
			if (_initializedPlayer != player)
			{
				_initializedPlayer = player;
				player?.RegisterHtmlCustomEventHandler("lottie_state", OnStateChanged, isDetailJson: false);
				player?.RegisterHtmlCustomEventHandler("animation_dom_loaded", OnAnimationDomLoaded);
			}

			if (player == null || _isUpdating)
			{
				return;
			}

#if !NET7_0_OR_GREATER
			string[] js;
#endif

			var sourceUri = UriSource;
			if (_lastSource == null || !_lastSource.Equals(sourceUri))
			{
				_lastSource = sourceUri;

				if ((await TryLoadDownloadJson(sourceUri, ct)) is { } jsonStream)
				{
					var firstLoad = true;

					var cacheKey = sourceUri.OriginalString;
					_animationDataSubscription.Disposable = null;
					_animationDataSubscription.Disposable =
						LoadAndObserveAnimationData(jsonStream, cacheKey, OnJsonChanged);

					void OnJsonChanged(string updatedJson, string updatedCacheKey)
					{
						var play = _playState != null || (firstLoad && player.AutoPlay);
						_domLoaded = false;

						if (play && _playState == null)
						{
							_playState = (0, 1, true);
						}
						else if (!play)
						{
							_playState = null;
						}

						firstLoad = false;

#if NET7_0_OR_GREATER
						_isUpdating = true;

						NativeMethods.SetAnimationProperties(
							player.HtmlId,
							null,
							play,
							player.Stretch.ToString(),
							player.PlaybackRate,
							updatedCacheKey,
							updatedJson);

						_isUpdating = false;
#else
						js = new[]
						{
							"Uno.UI.Lottie.setAnimationProperties({",
							"elementId:",
							player.HtmlId.ToString(),
							",jsonPath: null,autoplay:",
							play ? "true" : "false",
							",stretch:\"",
							player.Stretch.ToString(),
							"\",rate:",
							player.PlaybackRate.ToStringInvariant(),
							",cacheKey:\"",
							updatedCacheKey,
							"\"},",
							updatedJson,
							");"
						};

						ExecuteJs(js);
#endif

						if (_playState != null && _domLoaded)
						{
							var (fromProgress, toProgress, looped) = _playState.Value;
							Play(fromProgress, toProgress, looped);
						}
					}
				}
				else
				{
					var documentPath = UriSource.Scheme.Equals("http") || UriSource.Scheme.Equals("https")
					? UriSource.OriginalString
					: Windows.Storage.Helpers.AssetsPathBuilder.BuildAssetUri(UriSource?.PathAndQuery);

#if NET7_0_OR_GREATER
					_isUpdating = true;

					NativeMethods.SetAnimationProperties(
						player.HtmlId,
						documentPath ?? string.Empty,
						player.AutoPlay,
						player.Stretch.ToString(),
						player.PlaybackRate,
						documentPath ?? "-n-");

					_isUpdating = false;
#else
					js = new[]
					{
						"Uno.UI.Lottie.setAnimationProperties({",
						"elementId:",
						player.HtmlId.ToString(),
						",jsonPath:\"",
						documentPath ?? "",
						"\",autoplay:",
						player.AutoPlay ? "true" : "false",
						",stretch:\"",
						player.Stretch.ToString(),
						"\",rate:",
						player.PlaybackRate.ToStringInvariant(),
						",cacheKey:\"",
						documentPath ?? "-n-",
						"\"});"
					};
					ExecuteJs(js);
#endif

					if (player.AutoPlay)
					{
						_playState = (0, 1, true);
					}
					else
					{
						_playState = null;
					}
				}

				ApplyPlayState();
			}

#if !NET7_0_OR_GREATER
			void ExecuteJs(string[] js)
			{
				_isUpdating = true;

				InvokeJs(js);

				_isUpdating = false;
			}
#endif
		}

		private void ApplyPlayState()
		{
			if (_playState != null)
			{
				var (fromProgress, toProgress, looped) = _playState.Value;
				Play(fromProgress, toProgress, looped);
			}
		}

		private void OnAnimationDomLoaded(object? sender, HtmlCustomEventArgs e)
		{
			_domLoaded = true;
			ApplyPlayState();
		}

		private void OnStateChanged(object? sender, HtmlCustomEventArgs e) => ParseStateString(e.Detail);

		private void ParseStateString(string stateString)
		{
			if (_player == null)
			{
				return;
			}

			var parts = stateString.Split('|');
			double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var w);
			double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var h);
			var loaded = parts[2].Equals("true", StringComparison.Ordinal);
			var paused = parts[3].Equals("true", StringComparison.Ordinal);

			_player.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, loaded);
			_player.SetValue(AnimatedVisualPlayer.IsPlayingProperty, !paused);
			if (double.TryParse(parts[4], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var duration))
			{
				if (double.IsNaN(duration))
				{
					duration = 0d;
				}
				_player.SetValue(AnimatedVisualPlayer.DurationProperty, TimeSpan.FromSeconds(duration));
			}

			_compositionSize = new Size(w, h);
		}

		public void Play(double fromProgress, double toProgress, bool looped)
		{
			_playState = (fromProgress, toProgress, looped);

			if (_player == null || !_domLoaded)
			{
				return;
			}

#if NET7_0_OR_GREATER
			NativeMethods.Play(_player.HtmlId, fromProgress, toProgress, looped);
#else
			var js = new[]
			{
				"Uno.UI.Lottie.play(",
				_player.HtmlId.ToString(),
				",",
				fromProgress.ToStringInvariant(),
				",",
				toProgress.ToStringInvariant(),
				",",
				looped ? "true" : "false",
				");"
			};

			InvokeJs(js);
#endif
		}

		void IAnimatedVisualSource.Stop()
		{
			_playState = null;

			if (_player == null)
			{
				return;
			}

#if NET7_0_OR_GREATER
			NativeMethods.Stop(_player.HtmlId);
#else
			var js = new[]
			{
				"Uno.UI.Lottie.stop(",
				_player.HtmlId.ToString(),
				");"
			};

			InvokeJs(js);
#endif
		}

		void IAnimatedVisualSource.Pause()
		{
			if (_player == null)
			{
				return;
			}

#if NET7_0_OR_GREATER
			NativeMethods.Pause(_player.HtmlId);
#else
			var js = new[]
			{
				"Uno.UI.Lottie.pause(",
				_player.HtmlId.ToString(),
				");"
			};

			InvokeJs(js);
#endif
		}

		void IAnimatedVisualSource.Resume()
		{
			if (_player == null)
			{
				return;
			}

#if NET7_0_OR_GREATER
			NativeMethods.Resume(_player.HtmlId);
#else
			var js = new[]
			{
				"Uno.UI.Lottie.resume(",
				_player.HtmlId.ToString(),
				");" 
			};

			InvokeJs(js);
#endif
		}

		public void SetProgress(double progress)
		{
			if (_player == null)
			{
				return;
			}

#if NET7_0_OR_GREATER
			NativeMethods.SetProgress(_player.HtmlId, progress);
#else
			var js = new[]
			{
				"Uno.UI.Lottie.setProgress(",
				_player.HtmlId.ToString(),
				",",
				progress.ToStringInvariant(),
				");"
			};

			InvokeJs(js);
#endif
		}

		void IAnimatedVisualSource.Load()
		{
			if (_player == null)
			{
				return;
			}

			if (_playState == null)
			{
				return;
			}

			ApplyPlayState();

#if NET7_0_OR_GREATER
			NativeMethods.Resume(_player.HtmlId);
#else
			var js = new[]
			{
					"Uno.UI.Lottie.resume(",
					_player.HtmlId.ToString(),
					");"
				};

			InvokeJs(js);
#endif
		}

#if !NET7_0_OR_GREATER
		private static string InvokeJs(string[] js) => WebAssemblyRuntime.InvokeJS(string.Concat(js));
#endif

		void IAnimatedVisualSource.Unload()
		{
			if (_player == null || _playState == null)
			{
				return;
			}

#if NET7_0_OR_GREATER
			NativeMethods.Pause(_player.HtmlId);
#else
			var js = new[]
			{
					"Uno.UI.Lottie.pause(",
					_player.HtmlId.ToString(),
					");"
				};

			InvokeJs(js);
#endif
		}

		private Size CompositionSize => _compositionSize;

#if NET7_0_OR_GREATER
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Uno.UI.Lottie";

			[JSImport($"{JsType}.pause")]
			internal static partial void Pause(nint htmlId);

			[JSImport($"{JsType}.play")]
			internal static partial void Play(nint htmlId, double from, double to, bool loop);

			[JSImport($"{JsType}.resume")]
			internal static partial void Resume(nint htmlId);

			[JSImport($"{JsType}.setAnimationPropertiesNative")]
			internal static partial void SetAnimationProperties(nint htmlId, string? jsonPath, bool autoplay, string stretch, double playbackRate, string cacheKey);

			[JSImport($"{JsType}.setAnimationPropertiesNative")]
			internal static partial void SetAnimationProperties(nint htmlId, string? jsonPath, bool autoplay, string stretch, double playbackRate, string cacheKey, string data);

			[JSImport($"{JsType}.setProgress")]
			internal static partial void SetProgress(nint htmlId, double progress);

			[JSImport($"{JsType}.stop")]
			internal static partial void Stop(nint htmlId);
		}
#endif
	}
}
