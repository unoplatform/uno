#if __WASM__
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.Graphics.Display.DisplayInformation.NativeMethods;
#endif

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private static readonly Lazy<DisplayInformation> _lazyInstance = new Lazy<DisplayInformation>(() => new DisplayInformation());

		private static DisplayInformation InternalGetForCurrentView() => _lazyInstance.Value;

#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Graphics.Display.DisplayInformation";
#endif

		public static int DispatchDpiChanged()
		{
			if (_lazyInstance.IsValueCreated)
			{
				_lazyInstance.Value.OnDisplayMetricsChanged();
			}
			return 0;
		}

		public static int DispatchOrientationChanged()
		{
			if (_lazyInstance.IsValueCreated)
			{
				_lazyInstance.Value.OnDisplayMetricsChanged();
			}
			return 0;
		}

		public DisplayOrientations CurrentOrientation
		{
			get
			{
				var jsOrientation = ReadOrientationType();
				return ParseJsOrientation(jsOrientation);
			}
		}

		/// <summary>
		/// Gets the native orientation of the display monitor, 
		/// which is typically the orientation where the buttons
		/// on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation
		{
			get
			{
				if (TryReadOrientationAngle(out var angle))
				{
					var jsOrientation = ReadOrientationType();

					var isCurrentlyPortrait = jsOrientation.StartsWith("portrait", StringComparison.Ordinal);
					var isCurrentlyLandscape = jsOrientation.StartsWith("landscape", StringComparison.Ordinal);

					if (!isCurrentlyLandscape && !isCurrentlyPortrait)
					{
						// JavaScript returned some unexpected string.
						return DisplayOrientations.None;
					}

					switch (angle)
					{
						case 0:
						case 180:
							return isCurrentlyPortrait ? DisplayOrientations.Portrait : DisplayOrientations.Landscape;
						case 90:
						case 270:
							return isCurrentlyPortrait ? DisplayOrientations.Landscape : DisplayOrientations.Portrait;
						default:
							return DisplayOrientations.None;
					}
				}

				return DisplayOrientations.None;
			}
		}

		public uint ScreenHeightInRawPixels
		{
			get
			{
				if (TryReadScreenHeight(out var height))
				{
					var scale = (double)LogicalDpi / BaseDpi;
					return (uint)(height * scale);
				}
				return 0;
			}
		}

		public uint ScreenWidthInRawPixels
		{
			get
			{
				if (TryReadScreenWidth(out var width))
				{
					var scale = (double)LogicalDpi / BaseDpi;
					return (uint)(width * scale);
				}
				return 0;
			}
		}

		public float LogicalDpi
		{
			get
			{
				if (TryReadDevicePixelRatio(out var devicePixelRatio))
				{
					return devicePixelRatio * BaseDpi;
				}
				else
				{
					return BaseDpi;
				}
			}
		}

		public double RawPixelsPerViewPixel
		{
			get
			{
				var scale = (double)LogicalDpi / BaseDpi;
				return scale;
			}
		}

		public ResolutionScale ResolutionScale
		{
			get
			{
				if (TryReadDevicePixelRatio(out var devicePixelRatio))
				{
					return (ResolutionScale)(int)(devicePixelRatio * 100);
				}
				else
				{
					return ResolutionScale.Scale100Percent;
				}
			}
		}

		partial void StartOrientationChanged()
		{
			_lastKnownOrientation = CurrentOrientation;

#if NET7_0_OR_GREATER
			NativeMethods.StartOrientationChanged();
#else
			var command = $"{JsType}.startOrientationChanged()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		partial void StopOrientationChanged()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StopOrientationChanged();
#else
			var command = $"{JsType}.stopOrientationChanged()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		partial void StartDpiChanged()
		{
			_lastKnownDpi = LogicalDpi;

#if NET7_0_OR_GREATER
			NativeMethods.StartDpiChanged();
#else
			var command = $"{JsType}.startDpiChanged()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		partial void StopDpiChanged()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StopDpiChanged();
#else
			var command = $"{JsType}.stopDpiChanged()";
			WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			_ = Uno.UI.Dispatching.CoreDispatcher.Main.RunAsync(
				Uno.UI.Dispatching.CoreDispatcherPriority.High,
				(ct) => SetOrientationAsync(orientations, ct));
		}

		private static bool TryReadDevicePixelRatio(out float value)
		{
#if NET7_0_OR_GREATER
			value = NativeMethods.GetDevicePixelRatio();

			return true;
#else
			return float.TryParse(WebAssemblyRuntime.InvokeJS("window.devicePixelRatio"), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
#endif
		}

		private static bool TryReadScreenWidth(out float value)
		{
#if NET7_0_OR_GREATER
			value = NativeMethods.GetScreenWidth();

			return true;
#else
			return float.TryParse(WebAssemblyRuntime.InvokeJS("window.screen.width"), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
#endif
		}

		private static bool TryReadScreenHeight(out float value)
		{
#if NET7_0_OR_GREATER
			value = NativeMethods.GetScreenHeight();

			return true;
#else
			return float.TryParse(WebAssemblyRuntime.InvokeJS("window.screen.height"), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
#endif
		}

		private static bool TryReadOrientationAngle(out int value)
		{
#if NET7_0_OR_GREATER
			var angle = NativeMethods.GetScreenOrientationAngle();

			value = angle ?? default;

			return angle.HasValue;
#else
			return int.TryParse(WebAssemblyRuntime.InvokeJS("window.screen.orientation.angle"), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
#endif
		}

		private static string ReadOrientationType()
			=>
#if NET7_0_OR_GREATER
				NativeMethods.GetScreenOrientationType();
#else
				WebAssemblyRuntime.InvokeJS("window.screen.orientation.type");
#endif

		private static DisplayOrientations ParseJsOrientation(string jsOrientation)
		{
			// orientation has four possible values
			// see https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type
			return (jsOrientation.ToLowerInvariant()) switch
			{
				"portrait-primary" => DisplayOrientations.Portrait,
				"portrait-secondary" => DisplayOrientations.PortraitFlipped,
				"landscape-primary" => DisplayOrientations.Landscape,
				"landscape-secondary" => DisplayOrientations.LandscapeFlipped,
				_ => DisplayOrientations.None,
			};
		}

		private static Task SetOrientationAsync(DisplayOrientations orientations, CancellationToken ct)
		{
#if NET7_0_OR_GREATER
			return NativeMethods.SetOrientationAsync((int)orientations);
#else
			return WebAssemblyRuntime.InvokeAsync($"{JsType}.setOrientationAsync({(int)orientations})", ct);
#endif
		}
	}
}
#endif
