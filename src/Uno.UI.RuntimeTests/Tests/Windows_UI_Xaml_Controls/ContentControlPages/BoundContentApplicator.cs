using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ContentControlPages
{
	public partial class BoundContentApplicator : Grid
	{
		public object PseudoContent
		{
			get { return (object)GetValue(PseudoContentProperty); }
			set { SetValue(PseudoContentProperty, value); }
		}

		public static readonly DependencyProperty PseudoContentProperty =
			DependencyProperty.Register("PseudoContent", typeof(object), typeof(BoundContentApplicator), new PropertyMetadata(null));

		public void SpawnButton()
		{
			var button = new Button(); // Since we create the Button from code, its default style and its template will only be applied when it's loaded into the visual tree
			button.Content = PseudoContent;
			Children.Add(button);
		}
	}
}
