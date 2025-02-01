namespace uno53net9blank;
using System;
using Microsoft.UI.Xaml.Controls;

public sealed
#if !DESKTOP
    // This control tests that ControlWithXamlEverywhereExceptDesktop.xaml is not included,
    // so if ControlWithXamlEverywhereExceptDesktop.xaml is included, it won't compile.
    partial
#endif
    class ControlWithXamlEverywhereExceptDesktop : Page
{
	public ControlWithXamlEverywhereExceptDesktop()
	{
		// no this.InitializeComponent();
    }
}
