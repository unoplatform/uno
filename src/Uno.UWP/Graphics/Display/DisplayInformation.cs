#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno;

namespace Windows.Graphics.Display
{
    public sealed partial class DisplayInformation
    {
		private static DisplayOrientations _autoRotationPreferences;

		private static DisplayInformation _instance;

		private DisplayInformation()
		{
			Initialize();
		}

		public static DisplayOrientations AutoRotationPreferences
		{
			get { return _autoRotationPreferences; }
			set
			{
				_autoRotationPreferences = value;
				SetOrientationPartial(_autoRotationPreferences);
			}
		}

		public DisplayOrientations CurrentOrientation { get; private set; }

		/// <summary>
		//// Gets the native orientation of the display monitor, 
		///  which is typically the orientation where the buttons
		///  on the device match the orientation of the monitor.
		/// </summary>
		public DisplayOrientations NativeOrientation { get; private set; } = DisplayOrientations.None;

		public uint ScreenHeightInRawPixels { get; private set; }

		public uint ScreenWidthInRawPixels { get; private set; }

		public float LogicalDpi { get; private set; }

		/// <summary>
		/// Diagonal size of the display in inches.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.diagonalsizeininches#property-value">Docs</see> 
		/// defaults to null if not set
		/// </remarks>
		public double? DiagonalSizeInInches { get; private set; }

        public double RawPixelsPerViewPixel { get; private set; }
		
		/// <summary>
		/// Gets the raw dots per inch (DPI) along the x axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpix#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		public float RawDpiX { get; private set; } = 0;

		/// <summary>
		/// Gets the raw dots per inch (DPI) along the y axis of the display monitor.
		/// </summary>
		/// <remarks>
		/// As per <see href="https://docs.microsoft.com/en-us/uwp/api/windows.graphics.display.displayinformation.rawdpiy#remarks">Docs</see> 
		/// defaults to 0 if not set
		/// </remarks>
		public float RawDpiY { get; private set; } = 0;

		public bool StereoEnabled { get; private set; } = false;

        public ResolutionScale ResolutionScale { get; private set; }
		
		public static DisplayInformation GetForCurrentView()
		{
			return _instance ?? (_instance = new DisplayInformation());
		}

		static partial void SetOrientationPartial(DisplayOrientations orientations);

		partial void Initialize();

#pragma warning disable CS0067
		public event Foundation.TypedEventHandler<DisplayInformation, object> OrientationChanged;
#pragma warning restore CS0067
	}
}
