using Windows.UI.Text;

namespace Windows.UI.Xaml.Documents
{
	public  partial class Underline : Span
	{
#if !__WASM__
		public Underline()
		{
			TextDecorations = TextDecorations.Underline; // TODO
		}
#endif

		protected override void OnStyleChanged()
		{
			if (Style == null)
			{
				base.Style = Style.DefaultStyleForType(typeof(Underline));
				base.Style.ApplyTo(this);
			}
		}
	}
}
