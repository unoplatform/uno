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

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSourceBase
	{
		private static readonly string? UNO_BOOTSTRAP_APP_BASE = global::System.Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

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
			if(_initializedPlayer != player)
			{
				_initializedPlayer = player;
				player?.RegisterHtmlCustomEventHandler("lottie_state", OnStateChanged, isDetailJson: false);
				player?.RegisterHtmlCustomEventHandler("animation_dom_loaded", OnAnimationDomLoaded);
			}

			if (player == null || _isUpdating)
			{
				return;
			}

			string[] js;

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

						if (_playState != null && _domLoaded)
						{
							var (fromProgress, toProgress, looped) = _playState.Value;
							Play(fromProgress, toProgress, looped);
						}
					}
				}
				else
				{
					var documentPath = string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE)
						? UriSource?.PathAndQuery
						: UNO_BOOTSTRAP_APP_BASE + UriSource?.PathAndQuery;
					_domLoaded = false;

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

			void ExecuteJs(string[] js)
			{
				_isUpdating = true;

				InvokeJs(js);

				_isUpdating = false;
			}
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
		}

		void IAnimatedVisualSource.Stop()
		{
			_playState = null;

			if (_player == null)
			{
				return;
			}

			var js = new[]
			{
				"Uno.UI.Lottie.stop(",
				_player.HtmlId.ToString(),
				");"
			};

			InvokeJs(js);
		}

		void IAnimatedVisualSource.Pause()
		{
			if (_player == null)
			{
				return;
			}

			var js = new[]
			{
				"Uno.UI.Lottie.pause(",
				_player.HtmlId.ToString(),
				");"
			};

			InvokeJs(js);
		}

		void IAnimatedVisualSource.Resume()
		{
			if (_player == null)
			{
				return;
			}

			var js = new[]
			{
				"Uno.UI.Lottie.resume(",
				_player.HtmlId.ToString(),
				");"
			};

			InvokeJs(js);
		}

		public void SetProgress(double progress)
		{
			if (_player == null)
			{
				return;
			}

			var js = new[]
			{
				"Uno.UI.Lottie.setProgress(",
				_player.HtmlId.ToString(),
				",",
				progress.ToStringInvariant(),
				");"
			};

			InvokeJs(js);
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

			var js = new[]
			{
					"Uno.UI.Lottie.resume(",
					_player.HtmlId.ToString(),
					");"
				};

			InvokeJs(js);
		}

		private static string InvokeJs(string[] js) => WebAssemblyRuntime.InvokeJS(string.Concat(js));

		void IAnimatedVisualSource.Unload()
		{
			if (_player == null || _playState == null)
			{
				return;
			}

			var js = new[]
			{
					"Uno.UI.Lottie.pause(",
					_player.HtmlId.ToString(),
					");"
				};

			InvokeJs(js);
		}

		private Size CompositionSize => _compositionSize;
	}
}
