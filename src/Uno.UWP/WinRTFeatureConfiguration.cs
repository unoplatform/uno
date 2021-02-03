using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Uno
{
	public static class WinRTFeatureConfiguration
	{
		/// <summary>
		/// Used by tests cleanup to restore the default configuration for other tests!
		/// </summary>
		internal static void RestoreDefaults()
		{
			GestureRecognizer.RestoreDefaults();
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

		public static class Midi
		{
#if __WASM__
			/// <summary>
			/// Allows MIDI System eclusive access for WebAssembly.
			/// </summary>
			public static bool RequestSystemExclusiveAccess { get; set; }
#endif
		}
		
		public static class NetworkInformation
		{
			public static string ReachabilityHostname { get; set; } = "www.example.com";
		}
	}
}
