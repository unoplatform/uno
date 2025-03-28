using Windows.UI.Text;

namespace Windows.UI.Xaml.Documents
{
	public partial class Underline : Span
	{
#if !__WASM__
		public Underline()
		{
			TextDecorations = TextDecorations.Underline; // TODO
		}
#endif
	}
}
