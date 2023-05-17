using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel;

namespace Windows.UI.Xaml;

public partial class Application
{
	public Application()
	{
		Current = this;
		Package.EntryAssembly = this.GetType().Assembly;
		InitializeSystemTheme();
	}
}
