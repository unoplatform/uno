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

		public static DisplayOrientations AutoRotationPreferences
		{
			get { return _autoRotationPreferences; }
			set
			{
				_autoRotationPreferences = value;
				SetOrientationPartial(_autoRotationPreferences);
			}
		}

		static partial void SetOrientationPartial(DisplayOrientations orientations);

		public DisplayOrientations CurrentOrientation { get; private set; }

		public DisplayOrientations NativeOrientation { get; private set; }

		public uint ScreenHeightInRawPixels { get; private set; }

		public uint ScreenWidthInRawPixels { get; private set; }

		public float LogicalDpi { get; private set; }


		public double? DiagonalSizeInInches { get; private set; }

        public double RawPixelsPerViewPixel { get; private set; }

		public float RawDpiX { get; private set; }

		public float RawDpiY { get; private set; }

        public ResolutionScale ResolutionScale { get; private set; }

		private DisplayInformation()
		{
			Initialize();
		}

		partial void Initialize();

		public static DisplayInformation GetForCurrentView()
		{
			if (_instance == null)
			{
				_instance = new DisplayInformation();
			}

			return _instance;
		}

#pragma warning disable CS0067
		public event Foundation.TypedEventHandler<DisplayInformation, object> OrientationChanged;
#pragma warning restore CS0067
	}
}
