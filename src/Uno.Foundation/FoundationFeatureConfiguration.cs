using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Uno
{
	public static class FoundationFeatureConfiguration
	{
		/// <summary>
		/// Used by tests cleanup to restore the default configuration for other tests!
		/// </summary>
		internal static void RestoreDefaults()
		{
			GestureRecognizer.RestoreDefaults();
			Rect.RestoreDefaults();
		}

		public static class GestureRecognizer
		{
			internal static void RestoreDefaults()
			{
#if __ANDROID__
				InterpretMouseLeftLongPressAsRightTap = _defaultInterpretMouseLeftLongPressAsRightTap;
#elif __IOS__
				InterpretForceTouchAsRightTap = _defaultInterpretForceTouchAsRightTap;
#endif
			}

#if __ANDROID__
			private const bool _defaultInterpretMouseLeftLongPressAsRightTap = true;
			/// <summary>
			/// Determines if unlike UWP, long press on the left button of a mouse should be interpreted as a right tap.
			/// This is useful as the right button is commonly used by Android devices for back navigation.
			/// Using a long press with left button will be more intuitive for Android's users.
			/// Note that a long press on the right button is usually not used for back navigation, and will always be interpreted
			/// as a right tap no matter the value of this flag.
			/// </summary>
			[DefaultValue(_defaultInterpretMouseLeftLongPressAsRightTap)]
			public static bool InterpretMouseLeftLongPressAsRightTap { get; set; } = _defaultInterpretMouseLeftLongPressAsRightTap;
#endif
#if __IOS__
			private const bool _defaultInterpretForceTouchAsRightTap = true;
			/// <summary>
			/// Determines if force touch (a.k.a. 3D touch) should be interpreted as a right tap.
			/// Note that a long press will always be interpreted as a right tap no matter the value of this flag.
			/// </summary>
			[DefaultValue(_defaultInterpretForceTouchAsRightTap)]
			public static bool InterpretForceTouchAsRightTap { get; set; } = _defaultInterpretForceTouchAsRightTap;
#endif

		}

		public static class Rect
		{
			internal static void RestoreDefaults()
			{
				AllowNegativeWidthHeight = _defaultAllowNegativeWidthHeight;
			}

			private const bool _defaultAllowNegativeWidthHeight = true;
			/// <summary>
			/// If this flag is set to true, the <see cref="Windows.Foundation.Rect"/> won't throw an exception
			/// if it's been created with a negative width / height.
			/// This should be kept to `true` until https://github.com/unoplatform/uno/issues/606 get fixed.
			/// </summary>
			/// <remarks>This hides some errors from invalid measure/arrange which have to be fixed!</remarks>
			[DefaultValue(_defaultAllowNegativeWidthHeight)]
			public static bool AllowNegativeWidthHeight { get; set; } = _defaultAllowNegativeWidthHeight;
		}
	}
}
