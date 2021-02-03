using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using System.Threading.Tasks;
using Uno.UI;
using Uno.Extensions;
using UIKit;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class SelectorItem
	{
		private WeakReference<UIView> _parentOverride;

		/// <summary>
		/// Set a view which should be used as the DP parent for this container. Used from <see cref="NativeListViewBase"/>, because <see cref="UICollectionView"/>
		/// wraps items in native UIViews which interrupts binding propagation.
		/// </summary>
		/// <param name="parentOverride">The logical parent of this container.</param>
		internal void SetParentOverride(UIView parentOverride)
		{
			_parentOverride = new WeakReference<UIView>(parentOverride);
			this.SetParent(parentOverride);
		}

		internal override void OnLoading()
		{
			if (_parentOverride?.GetTarget() is { } parentOverride && !(this.GetParent() is DependencyObject))
			{
				this.SetParent(parentOverride);
			}
			base.OnLoading();
		}
	}
}
