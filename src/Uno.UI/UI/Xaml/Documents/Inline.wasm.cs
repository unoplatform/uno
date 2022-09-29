#nullable disable

using System.Collections.Generic;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Documents
{
	abstract partial class Inline
	{
		protected Inline(string htmlTag = "span") : base(htmlTag)
		{
		}
	}
}
