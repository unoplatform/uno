#if __WASM__
using Uno;
using Uno.Foundation;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private const string JsType = "Windows.Graphics.Display.DisplayInformation";

		// devicePixelRatio of 1 = https://developer.mozilla.org/en-US/docs/Web/API/Window/devicePixelRatio
		private const float BaseDpi = 96.0f;

		[Preserve]
		public static int DispatchDpiChanged()
		{
			if (_lazyInstance.IsValueCreated)
			{
				_lazyInstance.Value.OnDpiChanged();
			}
			return 0;
		}

		[Preserve]
		public static int DispatchOrientationChanged()
		{
			if (_lazyInstance.IsValueCreated)
			{
				_lazyInstance.Value.OnOrientationChanged();
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

		/// <summary>
		//// Gets the native orientation of the display monitor, 
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation { get; private set; } = DisplayOrientations.None;

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
				return (ResolutionScale)LogicalDpi;
			}
		}

		partial void StartOrientationChanged()
		{
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
			var command = $"{JsType}.startDpiChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		partial void StopDpiChanged()
		{
			var command = $"{JsType}.stopDpiChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		private static bool TryReadJsFloat(string property, out float value) =>
			float.TryParse(WebAssemblyRuntime.InvokeJS(property), out value);

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
	}
}
#endif
