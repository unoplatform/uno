using System;
using System.Collections.Generic;
using System.Threading;
using Uno.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	public partial class LottieVisualSource
	{
		private AnimatedVisualPlayer _player;
		private bool _isPlaying;

		void IAnimatedVisualSource.Update(AnimatedVisualPlayer player)
		{
			_player = player;
			var js = new[]
			{
				"Uno.UI.Lottie.setAnimationProperties({",
				"elementId:\"",
				player.HtmlId.ToString(),
				"\", jsonPath:\"",
				UriSource?.PathAndQuery ?? "",
				"\", autoplay:",
				player.AutoPlay ? "true" : "false",
				", stretch:\"",
				player.Stretch.ToString(),
				"\", rate:",
				player.PlaybackRate.ToString(),
				"});"
			};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = player.AutoPlay;
		}

		void IAnimatedVisualSource.Play(bool looped)
		{
			var js = new[]
			{
				"Uno.UI.Lottie.play(\"",
				_player.HtmlId.ToString(),
				"\",",
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
				"Uno.UI.Lottie.stop(\"",
				_player.HtmlId.ToString(),
				"\");"
			};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = false;
		}

		void IAnimatedVisualSource.Pause()
		{
			var js = new[]
			{
				"Uno.UI.Lottie.pause(\"",
				_player.HtmlId.ToString(),
				"\");"
			};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = false;
		}

		void IAnimatedVisualSource.Resume()
		{
			var js = new[]
			{
				"Uno.UI.Lottie.resume(\"",
				_player.HtmlId.ToString(),
				"\");"
			};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
			_isPlaying = false;
		}

		void IAnimatedVisualSource.Load()
		{
			if (!_isPlaying)
			{
				return;
			}

			var js = new[]
			{
					"Uno.UI.Lottie.play(\"",
					_player.HtmlId.ToString(),
					"\");"
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
					"Uno.UI.Lottie.stop(\"",
					_player.HtmlId.ToString(),
					"\");"
				};
			WebAssemblyRuntime.InvokeJS(string.Concat(js));
		}
	}
}
