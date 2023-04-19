using Android.Views;
using Android.Widget;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Microsoft.UI.Xaml.Data;
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

namespace Windows.UI.Xaml.Controls;

public partial class ContentPresenter
{
	partial void InitializePlatform()
	{
		IFrameworkElementHelper.Initialize(this);
	}

	protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom, bool localIsLayoutRequested)
	{
		base.OnLayoutCore(changed, left, top, right, bottom, localIsLayoutRequested);

		UpdateBorder();
	}

	partial void SetUpdateTemplatePartial()
	{
		RequestLayout();
	}

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

	partial void UnregisterContentTemplateRoot()
	{
		this.RemoveViewAndDispose(ContentTemplateRoot);
	}

	partial void OnPaddingChangedPartial()
	{
		ShouldUpdateMeasures = true;
	}

	protected override void OnDraw(Android.Graphics.Canvas canvas)
	{
		AdjustCornerRadius(canvas, CornerRadius);
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
}
