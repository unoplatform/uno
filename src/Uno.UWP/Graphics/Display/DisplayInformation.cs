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

#pragma warning disable 67 // unused member
		[NotImplemented]
		public event Foundation.TypedEventHandler<DisplayInformation, object> OrientationChanged;
#pragma warning restore 67 // unused member
	}
}