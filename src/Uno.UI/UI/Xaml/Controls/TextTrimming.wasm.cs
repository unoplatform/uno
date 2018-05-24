namespace Windows.UI.Xaml
{
	internal static class TextTrimmingExtensions
	{
		internal static string ToCssString(this TextTrimming value)
		{
			switch (value)
			{
				case TextTrimming.CharacterEllipsis:
					return "ellipsis";
				default:
					return "clip";
			}
		}
	}
}
