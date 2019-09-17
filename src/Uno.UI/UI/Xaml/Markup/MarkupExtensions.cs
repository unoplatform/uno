using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Markup
{
	public partial class MarkupExtension : IMarkupExtensionOverrides
	{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		//protected virtual object ProvideValue()
		//{
		//	return null;
		//}

		object IMarkupExtensionOverrides.ProvideValue()
		{
			return ProvideValue();
		}
#endif
	}
}
