using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.UI;
using System.Linq;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Data;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void RequestLayoutPartial()
		{
			InvalidateMeasure();
		}
	}
}
