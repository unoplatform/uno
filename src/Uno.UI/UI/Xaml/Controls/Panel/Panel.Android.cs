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

public partial class Panel : IEnumerable
{
	protected override void OnChildViewAdded(View child)
	{
		if (child is IFrameworkElement element)
		{
			OnChildAdded(element);
		}

		base.OnChildViewAdded(child);
	}

	partial void UpdateBorder() => _borderRenderer.Update();

	protected override void OnDraw(Android.Graphics.Canvas canvas)
	{
		AdjustCornerRadius(canvas, CornerRadiusInternal);
	}

	protected virtual void OnChildrenChanged() => UpdateBorder();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();

	partial void OnBorderBrushChangedPartial(Brush oldValue, Brush newValue) => UpdateBorder();

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();

	partial void OnCornerRadiusChangedPartial(CornerRadius oldValue, CornerRadius newValue) => UpdateBorder();

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e) => UpdateBorder();

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

	/// <summary>        
	/// Support for the C# collection initializer style.
	/// Allows items to be added like this 
	/// new Panel 
	/// {
	///    new Border()
	/// }
	/// </summary>
	/// <param name="view"></param>
	public void Add(UIElement view)
	{
		Children.Add(view);
	}

	public IEnumerator GetEnumerator()
	{
		return this.GetChildren().GetEnumerator();
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadiusInternal != CornerRadius.None;
}
