namespace Microsoft.UI.Xaml.Documents
{
	partial class Span
	{
		public Span() : this("span")
		{

		}

		public Span(string htmlTag) : base(htmlTag)
		{
			Inlines = new InlineCollection(this);
		}
	}
}
