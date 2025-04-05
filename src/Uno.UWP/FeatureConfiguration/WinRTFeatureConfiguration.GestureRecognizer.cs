using System;
using System.ComponentModel;
using Uno.UI.Input;

namespace Uno
{
	partial class WinRTFeatureConfiguration
	{

		public static class GestureRecognizer
		{
			internal static void RestoreDefaults()
			{
#if __ANDROID__
				InterpretMouseLeftLongPressAsRightTap = _defaultInterpretMouseLeftLongPressAsRightTap;
#elif __IOS__ || __TVOS__
				InterpretForceTouchAsRightTap = _defaultInterpretForceTouchAsRightTap;
#endif
				ShouldProvideHapticFeedback = _defaultShouldProvideHapticFeedback;
				PatchCasesForDirectManipulation = _defaultPatchSuspiciousCases;
				PatchCasesForUiElement = _defaultPatchSuspiciousCases;
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
#if __IOS__ || __TVOS__
			private const bool _defaultInterpretForceTouchAsRightTap = true;
			/// <summary>
			/// Determines if force touch (a.k.a. 3D touch) should be interpreted as a right tap.
			/// Note that a long press will always be interpreted as a right tap no matter the value of this flag.
			/// </summary>
			[DefaultValue(_defaultInterpretForceTouchAsRightTap)]
			public static bool InterpretForceTouchAsRightTap { get; set; } = _defaultInterpretForceTouchAsRightTap;
#endif

			private const bool _defaultShouldProvideHapticFeedback = true;
			/// <summary>
			/// Sets whether haptic feedback is provided when a touch-initiated drag is ready to begin. The default is true.
			/// </summary>
			[DefaultValue(_defaultShouldProvideHapticFeedback)]
			public static bool ShouldProvideHapticFeedback { get; set; } = _defaultShouldProvideHapticFeedback;

			internal const GestureRecognizerSuspiciousCases _defaultPatchSuspiciousCases = GestureRecognizerSuspiciousCases.All;
			/// <summary>
			/// Patch suspicious cases in the gesture recognizer for direct manipulations.
			/// </summary>
			[DefaultValue(_defaultPatchSuspiciousCases)]
			public static GestureRecognizerSuspiciousCases PatchCasesForDirectManipulation { get; set; } = _defaultPatchSuspiciousCases;

			/// <summary>
			/// Patch suspicious cases in the gesture recognizer for manipulation events on UI elements.
			/// </summary>
			[DefaultValue(_defaultPatchSuspiciousCases)]
			public static GestureRecognizerSuspiciousCases PatchCasesForUiElement { get; set; } = _defaultPatchSuspiciousCases;
		}
	}
}

namespace Uno.UI.Input
{
	[Flags]
	public enum GestureRecognizerSuspiciousCases
	{
		/// <summary>
		/// Disable all suspicious cases patching.
		/// </summary>
		None,

		/// <summary>
		/// On Android we have detected cases where the motion up event is incoherent with the gesture and the previous events, especially on flick (scroll) gesture eg.: <br />
		///   OnNativeMotionEvent: ts = 10396892 | cnt = 1 | x = 506.55872 | y = 1108.6874 | act = Move | actBtn = 0 | idx = 0 | btn = 0 | down = 10396844 | keys = None | dst = 0 | wheel = 0 | pressure = 1 | or = -0.5410267 | tilt = 0 | size = 145.33928x143.76465 / 145.33928x143.76465| buttons = <br />
		///   OnNativeMotionEvent: ts = 10396909 | cnt = 1 | x = 538.3761 | y = 1272.1499 | act = Move | actBtn = 0 | idx = 0 | btn = 0 | down = 10396844 | keys = None | dst = 0 | wheel = 0 | pressure = 1 | or = 1.4137113 | tilt = 0 | size = 148.96095x147.70125 / 148.96095x147.70125| buttons = <br />
		///   OnNativeMotionEvent: ts = 10396926 | cnt = 1 | x = 609.22656 | y = 1455.9674 | act = Move | actBtn = 0 | idx = 0 | btn = 0 | down = 10396844 | keys = None | dst = 0 | wheel = 0 | pressure = 1 | or = -1.2042861 | tilt = 0 | size = 150.37813x146.599 / 150.37813x146.599| buttons = <br />
		///   OnNativeMotionEvent: ts = 10396934 | cnt = 1 | x = 600.1593 | y = 1434.5426 | act = Up | actBtn = 0 | idx = 0 | btn = 0 | down = 10396844 | keys = None | dst = 0 | wheel = 0 | pressure = 1 | or = -1.2042861 | tilt = 0 | size = 150.37813x146.599 / 150.37813x146.599| buttons = <br />
		/// On all moves the Y is increasing, but the motion up the Y is decreasing (compared to last move). <br />
		/// This drives the last velocities (used to start inertia) to be incoherent, but also causes some weird flickers while scrolling. <br />
		/// Enabling this case allows gesture recognizer to patch the position of the pointer release event to be at the location where it would have been if pointer continued to move at the same speed as for the last move event.
		/// </summary>
		AndroidMotionUpAtInvalidLocation = 1 << 1,

		/// <summary>
		/// Enable all suspicious cases patching.
		/// </summary>
		All = 65535
	}
}
