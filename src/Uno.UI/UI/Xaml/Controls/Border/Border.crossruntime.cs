using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using System.Linq;
using System.Drawing;
using Uno.Disposables;
using Microsoft.UI.Xaml.Media;
using Uno.UI;

using View = Microsoft.UI.Xaml.UIElement;
using Color = System.Drawing.Color;
using Microsoft.UI.Composition;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class Border
{
	private readonly BorderLayerRenderer _borderRenderer;

	public Border()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}

	partial void OnChildChangedPartial(View previousValue, View newValue)
	{
		if (previousValue != null)
		{
			RemoveChild(previousValue);
		}

		AddChild(newValue);
	}

	private void UpdateBorder() => _borderRenderer.Update();

	// TODO:MZ: Needed on Skia?
	// internal override void OnArrangeVisual(Rect rect, Rect? clip)
	//{
	//	UpdateBorder();

	//	base.OnArrangeVisual(rect, clip);
	//}

	partial void OnBorderBrushChangedPartial()
	{
		UpdateBorder();
	}

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
	{
		UpdateBorder();
	}

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
	{
		UpdateBorder();
	}

	partial void OnCornerRadiusUpdatedPartial(CornerRadius oldValue, CornerRadius newValue)
	{
		UpdateBorder();
	}

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args)
	{
		base.OnBackgroundChanged(args);

		UpdateBorder();
		UpdateHitTest();
	}
}
