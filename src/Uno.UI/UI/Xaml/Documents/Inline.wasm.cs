using System.Collections.Generic;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents
{
	abstract partial class Inline
	{
		protected Inline(string htmlTag = "span") : base(htmlTag)
		{
		}
	}
}
