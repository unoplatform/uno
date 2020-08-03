using System.Text;

namespace Windows.UI.Input
{
	public partial class PointerPointProperties 
	{
		internal PointerPointProperties()
		{
		}

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
