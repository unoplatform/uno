using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class UserControl : ContentControl
	{
		public UserControl()
		{
		}

		// This mimics UWP
		private protected override Type GetDefaultStyleKey() => null;

		private protected override bool IsTabStopDefaultValue => false;
	}
}
