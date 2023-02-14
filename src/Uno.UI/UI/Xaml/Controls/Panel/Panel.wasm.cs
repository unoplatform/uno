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

		protected virtual void OnChildrenChanged()
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

		public new IEnumerator GetEnumerator()
		{
			return this.GetChildren().GetEnumerator();
		}

		partial void OnBackgroundChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			UpdateHitTest();
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadiusInternal != CornerRadius.None;
	}
}
