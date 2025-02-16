using Android.Views;
using Android.Widget;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Android.Graphics;
using Android.Graphics.Drawables;
using System.Drawing;
using System.Linq;
using Uno.UI;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

partial class ContentPresenter
{
	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;

	partial void InitializePlatform() => IFrameworkElementHelper.Initialize(this);

	partial void SetUpdateTemplatePartial() => RequestLayout();

	partial void RegisterContentTemplateRoot()
	{
		//This validation is present in order to remove the child from its parent if it already has a parent.
		//This prevents an exception for an InvalidState when we try to set a new template.
		if (ContentTemplateRoot.Parent != null)
		{
			(ContentTemplateRoot.Parent as ViewGroup)?.RemoveView(ContentTemplateRoot);
		}

		AddView(ContentTemplateRoot);
	}

	partial void UnregisterContentTemplateRoot() => this.RemoveViewAndDispose(ContentTemplateRoot);

	protected override void OnDraw(Android.Graphics.Canvas canvas)
	{
		AdjustCornerRadius(canvas, CornerRadius);
	}
}
