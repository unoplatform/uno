using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents
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
