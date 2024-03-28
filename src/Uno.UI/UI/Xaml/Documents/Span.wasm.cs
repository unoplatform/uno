namespace Windows.UI.Xaml.Documents
{
	partial class Span
	{
		public Span() : this("span")
		{

		}

		internal Span(string htmlTag) : base(htmlTag)
		{
			Inlines = new InlineCollection(this);
		}
	}
}
