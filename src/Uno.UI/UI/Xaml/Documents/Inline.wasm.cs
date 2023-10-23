using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Documents
{
	abstract partial class Inline
	{
		// This constructor is to keep API compatible with Skia and Reference.
		protected Inline() : this("span")
		{
		}

		protected Inline(string htmlTag = "span") : base(htmlTag)
		{
		}
	}
}
