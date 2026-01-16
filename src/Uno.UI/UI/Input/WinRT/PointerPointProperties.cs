// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
using System.Text;
using Windows.Foundation;
using Uno;

namespace Microsoft.UI.Input
{
	public partial class PointerPointProperties
	{
		private static class Mask
		{
			// Buttons // Note: Buttons must be first flags for the HasMultipleButtonsPressed to work
			public const int LeftButton = 1; // This has to be the first one, as it's the most common
			public const int MiddleButton = 1 << 1;
			public const int RightButton = 1 << 2;
			public const int XButton1 = 1 << 3;
			public const int XButton2 = 1 << 4;
			public const int BarrelButton = 1 << 5;

			public const int ButtonsCount = 6;
			public const int AllButtons = LeftButton | MiddleButton | RightButton | XButton1 | XButton2 | BarrelButton;

			// Pointer kind
			public const int HorizontalMouseWheel = 1 << 6;
			public const int Eraser = 1 << 7;
			public const int TouchPad = 1 << 8;

			// Misc core properties
			public const int Cancelled = 1 << 9;
			public const int Primary = 1 << 10;
			public const int InRange = 1 << 11;
			public const int TouchConfidence = 1 << 12;
		}

		internal int _flags;
		private bool GetFlag(int mask) => (_flags & mask) != 0;
		private void SetFlag(int mask, bool value) => _flags = value
			? _flags | mask
			: _flags & ~mask;


		internal PointerPointProperties()
		{
		}

		internal PointerPointProperties(global::Windows.UI.Input.PointerPointProperties properties)
		{
			if (properties is null)
			{
				return;
			}

			_flags = properties._flags;
			// Set by _flags: IsPrimary = properties.IsPrimary;
			// Set by _flags: IsInRange = properties.IsInRange;
			// Set by _flags: IsLeftButtonPressed = properties.IsLeftButtonPressed;
			// Set by _flags: IsMiddleButtonPressed = properties.IsMiddleButtonPressed;
			// Set by _flags: IsRightButtonPressed = properties.IsRightButtonPressed;
			// Set by _flags: IsHorizontalMouseWheel = properties.IsHorizontalMouseWheel;
			// Set by _flags: IsXButton1Pressed = properties.IsXButton1Pressed;
			// Set by _flags: IsXButton2Pressed = properties.IsXButton2Pressed;
			// Set by _flags: IsBarrelButtonPressed = properties.IsBarrelButtonPressed;
			// Set by _flags: IsEraser = properties.IsEraser;
			// Set by _flags: IsTouchPad = properties.IsTouchPad;
			Pressure = properties.Pressure;
			Orientation = properties.Orientation;
			ContactRect = properties.ContactRect;
			// Set by _flags: TouchConfidence = properties.TouchConfidence;
			// Set by _flags: IsCanceled = properties.IsCanceled;
			PointerUpdateKind = (PointerUpdateKind)properties.PointerUpdateKind;
			XTilt = properties.XTilt;
			YTilt = properties.YTilt;
			MouseWheelDelta = properties.MouseWheelDelta;
		}

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
		public static explicit operator global::Windows.UI.Input.PointerPointProperties(Microsoft.UI.Input.PointerPointProperties muxProps)
		{
			var props = new global::Windows.UI.Input.PointerPointProperties();

			props._flags = muxProps._flags;
			// Set by _flags : props.IsPrimary = muxProps.IsPrimary;
			// Set by _flags : props.IsInRange = muxProps.IsInRange;
			// Set by _flags : props.IsLeftButtonPressed = muxProps.IsLeftButtonPressed;
			// Set by _flags : props.IsMiddleButtonPressed = muxProps.IsMiddleButtonPressed;
			// Set by _flags : props.IsRightButtonPressed = muxProps.IsRightButtonPressed;
			// Set by _flags : props.IsHorizontalMouseWheel = muxProps.IsHorizontalMouseWheel;
			// Set by _flags : props.IsXButton1Pressed = muxProps.IsXButton1Pressed;
			// Set by _flags : props.IsXButton2Pressed = muxProps.IsXButton2Pressed;
			// Set by _flags : props.IsBarrelButtonPressed = muxProps.IsBarrelButtonPressed;
			// Set by _flags : props.IsEraser = muxProps.IsEraser;
			// Set by _flags : props.IsTouchPad = muxProps.IsTouchPad;
			props.Pressure = muxProps.Pressure;
			props.Orientation = muxProps.Orientation;
			props.ContactRect = muxProps.ContactRect;
			// Set by _flags: props.TouchConfidence = muxProps.TouchConfidence;
			// Set by _flags: props.IsCanceled = muxProps.IsCanceled;
			props.PointerUpdateKind = (global::Windows.UI.Input.PointerUpdateKind)muxProps.PointerUpdateKind;
			props.XTilt = muxProps.XTilt;
			props.YTilt = muxProps.YTilt;
			props.MouseWheelDelta = muxProps.MouseWheelDelta;

			return props;
		}
#endif

		/// <summary>
		/// This is actually equivalent to pointer.IsInContact
		/// </summary>
		internal bool HasPressedButton => (_flags & Mask.AllButtons) != 0;

		internal bool HasMultipleButtonsPressed
		{
			get
			{
				var buttons = _flags & Mask.AllButtons;
				if (buttons <= 1) // So we catch the common case where we only have the left button pressed
				{
					return false;
				}

				// Iterate flags until we find a button that is pressed
				for (var i = 0; i < Mask.ButtonsCount; i++)
				{
					var buttonMask = 1 << i;
					if ((_flags & buttonMask) != 0)
					{
						// Button is pressed, check if any other button is pressed
						return (buttons & ~buttonMask & Mask.AllButtons) != 0;
					}
				}

				return false;
			}
		}

		public bool IsPrimary
		{
			get => GetFlag(Mask.Primary);
			internal set => SetFlag(Mask.Primary, value);
		}

		public bool IsInRange
		{
			get => GetFlag(Mask.InRange);
			internal set => SetFlag(Mask.InRange, value);
		}

		public bool IsLeftButtonPressed
		{
			get => GetFlag(Mask.LeftButton);
			internal set => SetFlag(Mask.LeftButton, value);
		}

		public bool IsMiddleButtonPressed
		{
			get => GetFlag(Mask.MiddleButton);
			internal set => SetFlag(Mask.MiddleButton, value);
		}

		public bool IsRightButtonPressed
		{
			get => GetFlag(Mask.RightButton);
			internal set => SetFlag(Mask.RightButton, value);
		}

		public bool IsXButton1Pressed
		{
			get => GetFlag(Mask.XButton1);
			internal set => SetFlag(Mask.XButton1, value);
		}

		public bool IsXButton2Pressed
		{
			get => GetFlag(Mask.XButton2);
			internal set => SetFlag(Mask.XButton2, value);
		}

		public bool IsBarrelButtonPressed
		{
			get => GetFlag(Mask.BarrelButton);
			internal set => SetFlag(Mask.BarrelButton, value);
		}

		/// <summary>
		/// This is necessary for InteractionTracker, which behaves differently on mouse, touch and trackpad inputs.
		/// </summary>
		internal bool IsTouchPad
		{
			get => GetFlag(Mask.TouchPad);
			set => SetFlag(Mask.TouchPad, value);
		}

		public bool IsHorizontalMouseWheel
		{
			get => GetFlag(Mask.HorizontalMouseWheel);
			internal set => SetFlag(Mask.HorizontalMouseWheel, value);
		}

		public bool IsEraser
		{
			get => GetFlag(Mask.Eraser);
			internal set => SetFlag(Mask.Eraser, value);
		}

		public float Pressure { get; internal set; } = 0.5f; // According to the doc, the default value is .5

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public float Orientation { get; internal set; }

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public Rect ContactRect { get; internal set; }

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public bool TouchConfidence
		{
			get => GetFlag(Mask.TouchConfidence);
			internal set => SetFlag(Mask.TouchConfidence, value);
		}

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public bool IsCanceled
		{
			get => GetFlag(Mask.Cancelled);
			internal set => SetFlag(Mask.Cancelled, value);
		}

		public PointerUpdateKind PointerUpdateKind { get; internal set; }

		// Supported only on MacOS
		[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public float XTilt { get; internal set; }

		// Supported only on MacOS
		[NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public float YTilt { get; internal set; }

		[NotImplemented("__ANDROID__", "__APPLE_UIKIT__")]
		public int MouseWheelDelta { get; internal set; }

		/// <inheritdoc />
		public override string ToString()
		{
			var builder = new StringBuilder();

			// Common
			if (IsPrimary) builder.Append("primary ");
			if (IsInRange) builder.Append("in_range ");

			if (IsLeftButtonPressed) builder.Append("left ");
			if (IsMiddleButtonPressed) builder.Append("middle ");
			if (IsRightButtonPressed) builder.Append("right ");

			// Mouse
			if (IsXButton1Pressed) builder.Append("alt_butt_1 ");
			if (IsXButton2Pressed) builder.Append("alt_butt_2");
			if (MouseWheelDelta != 0)
			{
				builder.Append("scroll");
				builder.Append(IsHorizontalMouseWheel ? "X (" : "Y (");
				builder.Append(MouseWheelDelta);
				builder.Append("px) ");
			}

			// Pen
			if (IsBarrelButtonPressed) builder.Append("barrel ");
			if (IsEraser) builder.Append("eraser ");
			if (IsTouchPad) builder.Append("touchpad ");

			// Misc
			builder.Append('(');
			builder.Append(PointerUpdateKind);
			builder.Append(')');

			return builder.ToString();
		}
	}
}
#endif
