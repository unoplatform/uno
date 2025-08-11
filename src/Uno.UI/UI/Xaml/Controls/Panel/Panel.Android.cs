using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using Uno.UI.Helpers;
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
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class Panel : IEnumerable
{
	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadiusInternal != CornerRadius.None;

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

	protected override void OnChildViewAdded(View child)
	{
		if (child is IFrameworkElement element)
		{
			OnChildAdded(element);
		}

		base.OnChildViewAdded(child);
	}

	protected override void OnDraw(ACanvas canvas)
	{
		AdjustCornerRadius(canvas, CornerRadiusInternal);
	}

	public IEnumerator GetEnumerator() => this.GetChildren().GetEnumerator();
}
