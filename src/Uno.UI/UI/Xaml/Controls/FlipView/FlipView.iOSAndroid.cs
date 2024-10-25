#if __IOS__ || __ANDROID__
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);

			if (HasItems)
			{
				this.SelectedIndex = 0;
			}
		}

		protected override void OnItemsChanged(object e)
		{
			base.OnItemsChanged(e);

			if (HasItems && SelectedIndex < 0)
			{
				SelectedIndex = 0;
			}
		}

		internal override void OnSelectedIndexChanged(int oldValue, int newValue)
		{
			base.OnSelectedIndexChanged(oldValue, newValue);

			// Never animate for changes greater than next/previous item
			var smallChange = Math.Abs(newValue - oldValue) <= 1;
			OnSelectedIndexChangedPartial(oldValue, newValue, smallChange && UseTouchAnimationsForAllNavigation);
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new FlipViewItem() { IsGeneratedContainer = true };
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is FlipViewItem;
		}
	}
}
#endif
