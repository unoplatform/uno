#nullable disable

#if __WASM__
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private static readonly Lazy<DisplayInformation> _lazyInstance = new Lazy<DisplayInformation>(() => new DisplayInformation());

		private static DisplayInformation InternalGetForCurrentView() => _lazyInstance.Value;

		private const string JsType = "Windows.Graphics.Display.DisplayInformation";

		[Preserve]
		public static int DispatchDpiChanged()
		{
			if (_lazyInstance.IsValueCreated)
			{
				_lazyInstance.Value.OnDisplayMetricsChanged();
			}
			return 0;
		}

		[Preserve]
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
				var jsOrientation = ReadJsString("window.screen.orientation.type");
				return ParseJsOrientation(jsOrientation);
			}
		}

		public uint ScreenHeightInRawPixels
		{
			get
			{
				if (TryReadJsFloat("window.screen.height", out var height))
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
				if (TryReadJsFloat("window.screen.width", out var width))
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
				if (TryReadJsFloat("window.devicePixelRatio", out var devicePixelRatio))
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
				if (TryReadJsFloat("window.devicePixelRatio", out var devicePixelRatio))
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
			var command = $"{JsType}.startOrientationChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		partial void StopOrientationChanged()
		{
			var command = $"{JsType}.stopOrientationChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		partial void StartDpiChanged()
		{
			_lastKnownDpi = LogicalDpi;
			var command = $"{JsType}.startDpiChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		partial void StopDpiChanged()
		{
			var command = $"{JsType}.stopDpiChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			Uno.UI.Dispatching.CoreDispatcher.Main.RunAsync(
				Uno.UI.Dispatching.CoreDispatcherPriority.High,
				(ct) => SetOrientationAsync(orientations, ct));
		}

		private static bool TryReadJsFloat(string property, out float value) =>
			float.TryParse(WebAssemblyRuntime.InvokeJS(property), NumberStyles.Any, CultureInfo.InvariantCulture, out value);

		private static string ReadJsString(string property) => WebAssemblyRuntime.InvokeJS(property);

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
			return WebAssemblyRuntime.InvokeAsync($"{JsType}.setOrientationAsync({(int)orientations})", ct);
		}
	}
}
#endif
