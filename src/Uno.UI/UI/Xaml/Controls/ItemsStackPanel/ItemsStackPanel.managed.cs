#if !IS_UNIT_TESTS
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;
using Windows.Foundation;

#if __IOS__ || __ANDROID__
namespace Uno.UI.Controls
{
	public partial class ManagedItemsStackPanel
#else
namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsStackPanel
#endif
	{
		protected override Size MeasureOverride(Size availableSize) => _layout.MeasureOverride(availableSize);

		protected override Size ArrangeOverride(Size finalSize) => _layout.ArrangeOverride(finalSize);
	}
}

#endif
