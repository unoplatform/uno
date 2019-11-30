using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Android.Views;
using Uno.UI.DataBinding;
using Uno.Disposables;
using Windows.UI.Xaml.Data;
using System.Runtime.CompilerServices;
using Android.Graphics;
using Android.Graphics.Drawables;
using Uno.UI;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class Panel : IEnumerable
	{
		private SerialDisposable _brushChanged = new SerialDisposable();
		private BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();

		public Panel()
		{
			Initialize();

			this.RegisterLoadActions(UpdateBorder, () => _borderRenderer.Clear());

		}

		protected override void OnChildViewAdded(View child)
		{
			var element = child as IFrameworkElement;

			if (element != null)
			{
				OnChildAdded(element);
			}
		}

		partial void Initialize();

		private void UpdateBorder()
		{
			UpdateBorder(false);
		}

		private void UpdateBorder(bool willUpdateMeasures)
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayers(
					this,
					Background,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					Padding,
					willUpdateMeasures
				);
			}
		}

		protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayoutCore(changed, left, top, right, bottom);

			UpdateBorder(changed);
		}

		protected override void OnDraw(Android.Graphics.Canvas canvas)
		{
			AdjustCornerRadius(canvas, CornerRadius);
		}

		protected virtual void OnChildrenChanged()
		{
			UpdateBorder();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder(true);
		}

		partial void OnBorderBrushChangedPartial(Brush oldValue, Brush newValue)
		{
			UpdateBorder();
		}

		partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}

		partial void OnCornerRadiusChangedPartial(CornerRadius oldValue, CornerRadius newValue)
		{
			UpdateBorder();
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, just update the filling color.
			_brushChanged.Disposable = Brush.AssignAndObserveBrush(e.NewValue as Brush, _ => UpdateBorder(), UpdateBorder);
			UpdateBorder();
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

		/// <summary>        
		/// Support for the C# collection initializer style.
		/// Allows items to be added like this 
		/// new Panel 
		/// {
		///    new Border()
		/// }
		/// </summary>
		/// <param name="view"></param>
		public void Add(View view)
		{
			Children.Add(view);
		}

		public IEnumerator GetEnumerator()
		{
			return this.GetChildren().GetEnumerator();
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
	}
}
