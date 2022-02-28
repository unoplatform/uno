using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Uno.UI.DataBinding;
using Uno.Disposables;
using Microsoft.UI.Xaml.Data;
using System.Runtime.CompilerServices;
using System.Drawing;
using Uno.UI;
using Microsoft.UI.Xaml.Media;

using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Panel
	{
		partial void UpdateBorder()
		{
			// Checking for Window avoids re-creating the layer until it is actually used.
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					Background,
					InternalBackgroundSizing,
					BorderThicknessInternal,
					BorderBrushInternal,
					CornerRadiusInternal,
					null
				);
			}
		}

		protected virtual void OnChildrenChanged()
		{
			UpdateBorder();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
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

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnBackgroundChanged(e);
			UpdateHitTest();
			UpdateBorder();
		}
	}
}
