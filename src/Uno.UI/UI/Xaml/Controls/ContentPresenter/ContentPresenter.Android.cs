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
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentPresenter
	{
		private BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();

		public ContentPresenter()
		{
			InitializeContentPresenter();

			IFrameworkElementHelper.Initialize(this);
		}

		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom, bool localIsLayoutRequested)
		{
			base.OnLayoutCore(changed, left, top, right, bottom, localIsLayoutRequested);

			UpdateBorder();
		}

		private void SetUpdateTemplate()
		{
			UpdateContentTemplateRoot();
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

		partial void OnBackgroundSizingChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			UpdateBorder();
		}

		protected override void OnDraw(Android.Graphics.Canvas canvas)
		{
			AdjustCornerRadius(canvas, CornerRadius);
		}

		private void UpdateCornerRadius(CornerRadius radius) => UpdateBorder(false);

		private void UpdateBorder()
		{
			UpdateBorder(false);
		}

		private void UpdateBorder(bool willUpdateMeasures)
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					this,
					Background,
					BackgroundSizing,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					Padding,
					willUpdateMeasures
				);
			}
		}

		private void ClearBorder()
		{
			_borderRenderer.Clear();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder(true);
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
	}
}
