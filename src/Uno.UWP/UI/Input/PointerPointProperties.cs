using System.Text;

namespace Windows.UI.Input
{
	public partial class PointerPointProperties 
	{
		internal PointerPointProperties()
		{
		}

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

		public PointerUpdateKind PointerUpdateKind { get; internal set; }

		[global::Uno.NotImplemented]
		public int MouseWheelDelta => 0;

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
			if (IsHorizontalMouseWheel) builder.Append("scroll_Y ");
			if (IsXButton1Pressed) builder.Append("alt_butt_1 ");
			if (IsXButton2Pressed) builder.Append("alt_butt_2");

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
