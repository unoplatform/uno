using System.Collections.Generic;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents
{
	abstract partial class Inline
	{
		// This constructor is to keep API compatible with Skia and Reference.
		protected Inline() : this("span")
		{
		}

		private protected Inline(string htmlTag) : base(htmlTag)
		{
		}
	}
}
