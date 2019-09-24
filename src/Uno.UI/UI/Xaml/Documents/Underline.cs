namespace Windows.UI.Xaml.Documents
{
	public partial class Underline : Span
	{
#if !__WASM__
		public Underline()
		{
			TextDecorations = Text.TextDecorations.Underline; // TODO
		}
#endif
	}
}
