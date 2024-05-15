using System.Text;
using Windows.Foundation;
using Uno;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial class PointerPointProperties
	{
		internal PointerPointProperties()
		{
		}

		internal PointerPointProperties(Windows.UI.Input.PointerPointProperties properties)
		{
			if (properties is null)
			{
				return;
			}

			IsPrimary = properties.IsPrimary;
			IsInRange = properties.IsInRange;
			IsLeftButtonPressed = properties.IsLeftButtonPressed;
			IsMiddleButtonPressed = properties.IsMiddleButtonPressed;
			IsRightButtonPressed = properties.IsRightButtonPressed;
			IsHorizontalMouseWheel = properties.IsHorizontalMouseWheel;
			IsXButton1Pressed = properties.IsXButton1Pressed;
			IsXButton2Pressed = properties.IsXButton2Pressed;
			IsBarrelButtonPressed = properties.IsBarrelButtonPressed;
			IsEraser = properties.IsEraser;
			Pressure = properties.Pressure;
			Orientation = properties.Orientation;
			ContactRect = properties.ContactRect;
			TouchConfidence = properties.TouchConfidence;
			IsCanceled = properties.IsCanceled;
			PointerUpdateKind = (PointerUpdateKind)properties.PointerUpdateKind;
			XTilt = properties.XTilt;
			YTilt = properties.YTilt;
			MouseWheelDelta = properties.MouseWheelDelta;
		}

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
		public static explicit operator Windows.UI.Input.PointerPointProperties(Microsoft.UI.Input.PointerPointProperties muxProps)
		{
			var props = new Windows.UI.Input.PointerPointProperties();

			props.IsPrimary = muxProps.IsPrimary;
			props.IsInRange = muxProps.IsInRange;
			props.IsLeftButtonPressed = muxProps.IsLeftButtonPressed;
			props.IsMiddleButtonPressed = muxProps.IsMiddleButtonPressed;
			props.IsRightButtonPressed = muxProps.IsRightButtonPressed;
			props.IsHorizontalMouseWheel = muxProps.IsHorizontalMouseWheel;
			props.IsXButton1Pressed = muxProps.IsXButton1Pressed;
			props.IsXButton2Pressed = muxProps.IsXButton2Pressed;
			props.IsBarrelButtonPressed = muxProps.IsBarrelButtonPressed;
			props.IsEraser = muxProps.IsEraser;
			props.Pressure = muxProps.Pressure;
			props.Orientation = muxProps.Orientation;
			props.ContactRect = muxProps.ContactRect;
			props.TouchConfidence = muxProps.TouchConfidence;
			props.IsCanceled = muxProps.IsCanceled;
			props.PointerUpdateKind = (Windows.UI.Input.PointerUpdateKind)muxProps.PointerUpdateKind;
			props.XTilt = muxProps.XTilt;
			props.YTilt = muxProps.YTilt;
			props.MouseWheelDelta = muxProps.MouseWheelDelta;

			return props;
		}
#endif

		/// <summary>
		/// This is actually equivalent to pointer.IsInContact
		/// </summary>
		internal bool HasPressedButton => IsLeftButtonPressed || IsMiddleButtonPressed || IsRightButtonPressed || IsXButton1Pressed || IsXButton2Pressed || IsBarrelButtonPressed;

		public bool IsPrimary { get; internal set; }

		public bool IsInRange { get; internal set; }

		public bool IsLeftButtonPressed { get; internal set; }

		public bool IsMiddleButtonPressed { get; internal set; }

		public bool IsRightButtonPressed { get; internal set; }

		public bool IsHorizontalMouseWheel { get; internal set; }

		public bool IsXButton1Pressed { get; internal set; }

		public bool IsXButton2Pressed { get; internal set; }

		public bool IsBarrelButtonPressed { get; internal set; }

		public bool IsEraser { get; internal set; }

		public float Pressure { get; internal set; } = 0.5f; // According to the doc, the default value is .5

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public float Orientation { get; internal set; }

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public Rect ContactRect { get; internal set; }

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public bool TouchConfidence { get; internal set; }

		[NotImplemented] // This is not implemented, it can only be set using injected inputs
		public bool IsCanceled { get; internal set; }

		public PointerUpdateKind PointerUpdateKind { get; internal set; }

		// Supported only on MacOS
		[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public float XTilt { get; internal set; }

		// Supported only on MacOS
		[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public float YTilt { get; internal set; }

		[NotImplemented("__ANDROID__", "__IOS__", "__MACOS__")]
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

			// Misc
			builder.Append('(');
			builder.Append(PointerUpdateKind);
			builder.Append(')');

			return builder.ToString();
		}
	}
}
