using System.Text;

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

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
		public PointerPointProperties(Windows.UI.Input.PointerPointProperties properties)
		{
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
			PointerUpdateKind = (PointerUpdateKind)properties.PointerUpdateKind;
		}

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
			props.PointerUpdateKind = (Windows.UI.Input.PointerUpdateKind)muxProps.PointerUpdateKind;

			return props;
	}
#endif

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

		public PointerUpdateKind PointerUpdateKind { get; internal set; }

#if __MACOS__
		public float XTilt { get; internal set; } = 0f;

		public float YTilt { get; internal set; } = 0f;
#endif

#if __IOS__ || __MACOS__ || __ANDROID__
		[global::Uno.NotImplemented]
#endif
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
