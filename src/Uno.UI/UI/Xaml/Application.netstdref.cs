using System;
using System.Collections.Generic;
using System.Text;
using Windows.Globalization;

namespace Windows.UI.Xaml;

public partial class Application
{
	public Application()
	{
		Current = this;
		ApplicationLanguages.ApplyCulture();
		InitializeSystemTheme();
	}
}
