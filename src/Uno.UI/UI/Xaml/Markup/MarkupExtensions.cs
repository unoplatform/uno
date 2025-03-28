using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Markup
{
	public partial class MarkupExtension : IMarkupExtensionOverrides
	{
		object IMarkupExtensionOverrides.ProvideValue() => ProvideValue();

		object IMarkupExtensionOverrides.ProvideValue(IXamlServiceProvider serviceProvider) => ProvideValue(serviceProvider);

		protected virtual object ProvideValue() => null;

		protected virtual object ProvideValue(IXamlServiceProvider serviceProvider) => ProvideValue();
	}
}
