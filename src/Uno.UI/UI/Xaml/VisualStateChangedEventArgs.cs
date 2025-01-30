using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml
{
	public partial class VisualStateChangedEventArgs
	{
		public Control Control { get; set; }

		public VisualState NewState { get; set; }

		public VisualState OldState { get; set; }
	}
}
