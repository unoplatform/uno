using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Hosting;

internal class NavigationLosingFocusEventArgs : DesktopWindowXamlSourceTakeFocusRequestedEventArgs
{
	internal NavigationLosingFocusEventArgs(XamlSourceFocusNavigationRequest request) : base(request)
	{
	}
}

internal class NavigationGotFocusEventArgs : DesktopWindowXamlSourceGotFocusEventArgs
{
	internal NavigationGotFocusEventArgs(XamlSourceFocusNavigationRequest request) : base(request)
	{
	}
}
