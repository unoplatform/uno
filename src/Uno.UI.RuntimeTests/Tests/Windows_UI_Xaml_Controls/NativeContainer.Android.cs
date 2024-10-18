using Android.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	partial class NativeContainer : ViewGroup
	{
		public NativeContainer() : base(ContextHelper.Current)
		{
		}

		private void OnContentChanged(UIElement oldValue, UIElement newValue)
		{
			if (oldValue != null)
			{
				RemoveViewAt(0);
			}
			if (newValue != null)
			{
				AddView(newValue);
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			Content?.Measure(widthMeasureSpec, heightMeasureSpec);
			var logicalSize = Content?.DesiredSize ?? default;
			SetMeasuredDimension(ViewHelper.LogicalToPhysicalPixels(logicalSize.Width), ViewHelper.LogicalToPhysicalPixels(logicalSize.Height));
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			if (Content != null)
			{
				var physicalDesiredSize = ViewHelper.LogicalToPhysicalPixels(Content.DesiredSize);
				Content.Layout(0, 0, (int)physicalDesiredSize.Width, (int)physicalDesiredSize.Height);
			}
		}
	}
}
