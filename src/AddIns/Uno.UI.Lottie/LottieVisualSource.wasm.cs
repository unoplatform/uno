using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Uno.Foundation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	public partial class LottieVisualSource
	{
		private AnimatedVisualPlayer _player;
		private bool _isPlaying;
		private Size _compositionSize = new Size(0, 0);

		private void Update()
		{
			if (_player != null)
			{
				Update(_player);
			}
		}

		public void Update(AnimatedVisualPlayer player)
		{
			if(player != _player)
			{
				player.RegisterHtmlEventHandler("lottie_state", (EventHandler)OnStateChanged);
			}

			_player = player;

			var js = new[]
			{
				"Uno.UI.Lottie.setAnimationProperties({",
				"elementId:",
				player.HtmlId.ToString(),
				",jsonPath:\"",
				UriSource?.PathAndQuery ?? "",
				"\",autoplay:",
				player.AutoPlay ? "true" : "false",
				",stretch:\"",
				player.Stretch.ToString(),
				"\",rate:",
				player.PlaybackRate.ToString(),
				"});"
			};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = player.AutoPlay;
		}

		private void OnStateChanged(object sender, EventArgs e)
		{
			var r = WebAssemblyRuntime.InvokeJS("Uno.UI.Lottie.getAnimationState(" + _player.HtmlId + ");");

			var parts = r.Split('|');
			var w = double.Parse(parts[0]);
			var h = double.Parse(parts[1]);

			_compositionSize = new Size(w, h);
		}

		void IAnimatedVisualSource.Play(bool looped)
		{
			var js = new[]
			{
				"Uno.UI.Lottie.play(",
				_player.HtmlId.ToString(),
				",",
				looped ? "true" : "false",
				");"
			};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = true;
		}

		void IAnimatedVisualSource.Stop()
		{
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
			var js = new[]
			{
				"Uno.UI.Lottie.setProgress(",
				_player.HtmlId.ToString(),
				",",
				progress.ToString(CultureInfo.InvariantCulture),
				");"
			};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = true;
		}

		void IAnimatedVisualSource.Load()
		{
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

		Size IAnimatedVisualSource.Measure(Size availableSize)
		{
			var availableWidth = availableSize.Width;
			var availableHeight = availableSize.Height;
			if (double.IsInfinity(availableWidth))
			{
				if(double.IsInfinity(availableHeight))
				{
					return _compositionSize;
				}

				return new Size(availableHeight * _compositionSize.Width / _compositionSize.Height, availableHeight);
			}

			if (double.IsInfinity(availableHeight))
			{
				return new Size(availableWidth, availableWidth * _compositionSize.Height / _compositionSize.Width);

			}

			return availableSize;
		}
	}
}
