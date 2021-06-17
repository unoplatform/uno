using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		public FlipView()
		{
			DefaultStyleKey = typeof(FlipView);

			InitializePartial();
		}

		partial void InitializePartial();
	}
}
