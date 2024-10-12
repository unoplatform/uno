using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentControl
	{
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
	}
}
