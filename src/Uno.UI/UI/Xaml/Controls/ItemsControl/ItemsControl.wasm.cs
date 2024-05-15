using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.UI;
using System.Linq;
using Uno.Extensions.Specialized;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void RequestLayoutPartial()
		{
			InvalidateMeasure();
		}
	}
}
