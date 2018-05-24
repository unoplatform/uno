namespace Windows.UI.Xaml.Documents
{
	public  partial class Bold : Span
	{
		public Bold()
		{
			FontWeight = Text.FontWeights.Bold;
		}

		protected override void OnStyleChanged()
		{
			if (Style == null)
			{
				base.Style = Style.DefaultStyleForType(typeof(Bold));
				base.Style.ApplyTo(this);
			}
		}
	}
}
