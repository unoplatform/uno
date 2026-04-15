using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RichEditBox : Control
	{
		internal override string GetPlainText()
		{
			if (Header is not null)
			{
				var plainText = FrameworkElement.GetStringFromObject(Header);
				if (!string.IsNullOrEmpty(plainText))
				{
					return plainText;
				}
			}

			return PlaceholderText ?? string.Empty;
		}
	}
}
