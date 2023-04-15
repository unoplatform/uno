using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Android.Views;
using Uno.UI.DataBinding;
using Uno.Disposables;
using Microsoft.UI.Xaml.Data;
using System.Runtime.CompilerServices;
using Android.Graphics;
using Android.Graphics.Drawables;
using Uno.UI;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Panel
	{
		protected override void OnChildViewAdded(View child)
		{
			if (child is IFrameworkElement element)
			{
				OnChildAdded(element);
			}

			base.OnChildViewAdded(child);
		}

		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom, bool localIsLayoutRequested)
		{
			base.OnLayoutCore(changed, left, top, right, bottom, localIsLayoutRequested);

			ShouldUpdateMeasures = changed;
			UpdateBorder();
		}

		protected override void OnDraw(Android.Graphics.Canvas canvas)
		{
			AdjustCornerRadius(canvas, CornerRadiusInternal);
		}

		protected virtual void OnChildrenChanged() => UpdateBorder();

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			ShouldUpdateMeasures = true;
		}

		protected override void OnBeforeArrange()
		{
			base.OnBeforeArrange();

			//We set childrens position for the animations before the arrange
			_transitionHelper?.SetInitialChildrenPositions();
		}

		protected override void OnAfterArrange()
		{
			base.OnAfterArrange();

			//We trigger all layoutUpdated animations
			_transitionHelper?.LayoutUpdatedTransition();
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadiusInternal != CornerRadius.None;
	}
}
