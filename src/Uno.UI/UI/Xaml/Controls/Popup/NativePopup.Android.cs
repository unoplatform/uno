using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Controls.Primitives;
using Android.Views;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls.Primitives;

[ContentProperty(Name = "Child")]
public partial class NativePopup : NativePopupBase
{
	private Android.Widget.PopupWindow _popupWindow;

	public NativePopup()
	{
		_popupWindow = new Android.Widget.PopupWindow();
		_popupWindow.Focusable = true;
		_popupWindow.Touchable = true;
	}

	private protected override void OnLoaded()
	{
		base.OnLoaded();
		_popupWindow.DismissEvent += OnDismissEvent;
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();
		_popupWindow.DismissEvent -= OnDismissEvent;
	}

	private void OnDismissEvent(object sender, EventArgs e)
	{
		IsOpen = false;
	}

	protected override void OnChildChanged(UIElement oldChild, UIElement newChild)
	{
		base.OnChildChanged(oldChild, newChild);
		_popupWindow.ContentView = newChild;
	}

	protected override void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
	{
		base.OnIsOpenChanged(oldIsOpen, newIsOpen);

		if (newIsOpen)
		{
			if (Child is FrameworkElement child)
			{
				// TODO: Adjust for multiwindow #13827
				child.Measure(Window.CurrentSafe?.Bounds.Size ?? default);
				_popupWindow.Width = ViewHelper.LogicalToPhysicalPixels(child.DesiredSize.Width);
				_popupWindow.Height = ViewHelper.LogicalToPhysicalPixels(child.DesiredSize.Height);
			}

			_popupWindow.ShowAsDropDown(Anchor ?? this);
		}
		else
		{
			_popupWindow.Dismiss();
		}
	}

	public View Anchor
	{
		get { return (View)this.GetValue(AnchorProperty); }
		set { this.SetValue(AnchorProperty, value); }
	}

	public static DependencyProperty AnchorProperty { get; } =
		DependencyProperty.Register(
			"Anchor",
			typeof(View),
			typeof(NativePopup),
			new FrameworkPropertyMetadata(
				defaultValue: (View)null,
				options: FrameworkPropertyMetadataOptions.None
			)
		);
}
