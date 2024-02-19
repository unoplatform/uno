using System;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;

using NativeMethods = __Windows.Graphics.Display.DisplayInformation.NativeMethods;

namespace Windows.Graphics.Display
{
	public sealed partial class DisplayInformation
	{
		private static DisplayInformation _instance;

		private static DisplayInformation InternalGetForCurrentView() => _instance ??= new DisplayInformation();

		[JSExport]
		internal static int DispatchDpiChanged()
		{
			if (_instance is not null)
			{
				_instance.OnDisplayMetricsChanged();
			}
			return 0;
		}

		[JSExport]
		internal static int DispatchOrientationChanged()
		{
			if (_instance is not null)
			{
				_instance.OnDisplayMetricsChanged();
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

			NativeMethods.StartOrientationChanged();
		}

		partial void StopOrientationChanged()
		{
			NativeMethods.StopOrientationChanged();
		}

		partial void StartDpiChanged()
		{
			_lastKnownDpi = LogicalDpi;

			NativeMethods.StartDpiChanged();
		}

		partial void StopDpiChanged()
		{
			NativeMethods.StopDpiChanged();
		}

		static partial void SetOrientationPartial(DisplayOrientations orientations)
		{
			Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(
				() => SetOrientationAsync(orientations),
				Uno.UI.Dispatching.NativeDispatcherPriority.High);
		}

		private static bool TryReadDevicePixelRatio(out float value)
		{
			value = NativeMethods.GetDevicePixelRatio();

			return true;
		}

		private static bool TryReadScreenWidth(out float value)
		{
			value = NativeMethods.GetScreenWidth();

			return true;
		}

		private static bool TryReadScreenHeight(out float value)
		{
			value = NativeMethods.GetScreenHeight();

			return true;
		}

		private static bool TryReadOrientationAngle(out int value)
		{
			var angle = NativeMethods.GetScreenOrientationAngle();

			value = angle ?? default;

			return angle.HasValue;
		}

		private static string ReadOrientationType()
			=> NativeMethods.GetScreenOrientationType();

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

		private static Task SetOrientationAsync(DisplayOrientations orientations)
		{
			return NativeMethods.SetOrientationAsync((int)orientations);
		}
	}
}
