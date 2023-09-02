using System;
using System.Collections.Generic;
using System.Text;
using Uno.Foundation;

using NativeMethods = __Windows.UI.ViewManagement.ApplicationViewTitleBar.NativeMethods;

namespace Windows.UI.ViewManagement
{
	public partial class ApplicationViewTitleBar
	{
		private Color? _backgroundColor;

		public Color? BackgroundColor
		{
			get => _backgroundColor;
			set
			{
				if (_backgroundColor != value)
				{
					_backgroundColor = value;
					UpdateBackgroundColor();
				}
			}
		}

		private void UpdateBackgroundColor()
		{
			NativeMethods.SetBackgroundColor(_backgroundColor?.ToHexString());
		}
	}
}
