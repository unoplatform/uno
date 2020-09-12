using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Uno.Foundation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Extensions;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	public partial class LottieVisualSource
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = global::System.Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

		private AnimatedVisualPlayer _initializedPlayer;
		private bool _isPlaying;
		private Size _compositionSize = new Size(0, 0);
		private Uri _loadedEmbeddedUri;

		private (double fromProgress, double toProgress, bool looped)? _playState;
		private bool _isUpdating;

		partial void InnerUpdate()
		{
			var player = _player;
			if(_initializedPlayer != player)
			{
				player.RegisterHtmlCustomEventHandler("lottie_state", OnStateChanged, isDetailJson: false);
				_initializedPlayer = player;
			}

			string[] js;

			var uri = UriSource;

			if (uri.Scheme == "embedded")
			{
				string jsonString;

				if (!uri.Equals(_loadedEmbeddedUri) && TryLoadEmbeddedJson(uri, out jsonString))
				{
					_loadedEmbeddedUri = uri;
				}
				else
				{
					jsonString = "null";
				}

				js = new[]
				{
					"Uno.UI.Lottie.setAnimationProperties({",
					"elementId:",
					player.HtmlId.ToString(),
					",jsonPath: null",
					",autoplay:",
					player.AutoPlay ? "true" : "false",
					",stretch:\"",
					player.Stretch.ToString(),
					"\",rate:",
					player.PlaybackRate.ToStringInvariant(),
					"},",
					jsonString,
					");"
				};
			}
			else
			{
				var documentPath = string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE)
					? UriSource?.PathAndQuery
					: UNO_BOOTSTRAP_APP_BASE + UriSource?.PathAndQuery;

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
					"});"
				};
			}

			_isUpdating = true;

			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = player.AutoPlay;

			_isUpdating = false;

			ApplyPlayState();
		}

		private void ApplyPlayState()
		{
			if (_playState != null)
			{
				var (fromProgress, toProgress, looped) = _playState.Value;
				Play(fromProgress, toProgress, looped);
			}
		}

		private void OnStateChanged(object sender, HtmlCustomEventArgs e) => ParseStateString(e.Detail);

		private void ParseStateString(string stateString)
		{
			var parts = stateString.Split('|');
			double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var w);
			double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var h);
			var loaded = parts[2].Equals("true", StringComparison.Ordinal);
			var paused = parts[3].Equals("true", StringComparison.Ordinal);

			if (paused && !_isUpdating)
			{
				_playState = null;
			}

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

			if (_player == null)
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
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = true;
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
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = false;
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
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = false;
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
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = true;
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
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = true;
		}

		void IAnimatedVisualSource.Load()
		{
			if (_player == null)
			{
				return;
			}

			ApplyPlayState();

			if (!_isPlaying)
			{
				return;
			}

			var js = new[]
			{
					"Uno.UI.Lottie.resume(",
					_player.HtmlId.ToString(),
					");"
				};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
		}

		void IAnimatedVisualSource.Unload()
		{
			if (_player == null)
			{
				return;
			}
			if (!_isPlaying)
			{
				return;
			}

			var js = new[]
			{
					"Uno.UI.Lottie.pause(",
					_player.HtmlId.ToString(),
					");"
				};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
		}

		private Size CompositionSize => _compositionSize;
	}
}
