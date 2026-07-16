#nullable enable

namespace Microsoft.UI.Text
{
	public static partial class TextConstants
	{
		public static global::Windows.UI.Color AutoColor
			=> global::Windows.UI.Color.FromArgb(0, 0, 0, 1);

		public static int MaxUnitCount => 0x3FFFFFFF;

		public static int MinUnitCount => unchecked((int)0xC0000001);

		public static global::Windows.UI.Color UndefinedColor
			=> global::Windows.UI.Color.FromArgb(0, 0, 0, 2);

		public static float UndefinedFloatValue => -9_999_999f;

		public static global::Windows.UI.Text.FontStretch UndefinedFontStretch
			=> (global::Windows.UI.Text.FontStretch)UndefinedInt32Value;

		public static global::Windows.UI.Text.FontStyle UndefinedFontStyle
			=> (global::Windows.UI.Text.FontStyle)UndefinedInt32Value;

		public static int UndefinedInt32Value => -9_999_999;
	}
}
