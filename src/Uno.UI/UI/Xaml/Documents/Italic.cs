namespace Windows.UI.Xaml.Documents
{
	public  partial class Italic : Span
	{
		public Italic()
		{
			FontStyle = Windows.UI.Text.FontStyle.Italic;
		}

		protected override void OnStyleChanged()
		{
			if (Style == null)
			{
				base.Style = Style.DefaultStyleForType(typeof(Italic));
				base.Style.ApplyTo(this);
			}
		}
	}
}
