using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		public Application()
		{
			Current = this;
        }

		private ApplicationTheme GetDefaultSystemTheme() => ApplicationTheme.Light;
	}
}
