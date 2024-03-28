using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Markup
{
	public partial interface IMarkupExtensionOverrides
	{
		object ProvideValue();

		object ProvideValue(IXamlServiceProvider serviceProvider);
	}
}
