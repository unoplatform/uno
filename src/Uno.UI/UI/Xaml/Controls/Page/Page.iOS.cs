using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Views.Controls;
using Uno.UI.DataBinding;
using UIKit;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Page
	{
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			UpdateBorder();
		}
	}
}
