using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

internal interface ICleanableNativeWebView : INativeWebView
{
	void OnLoaded();

	void OnUnloaded();
}
