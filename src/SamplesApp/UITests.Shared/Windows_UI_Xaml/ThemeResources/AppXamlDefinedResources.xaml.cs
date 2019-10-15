using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.ThemeResources
{
	[SampleControlInfo("XAML", controlName: nameof(AppXamlDefinedResources), description: "Validates the use of resources defined in App.xaml")]
	public sealed partial class AppXamlDefinedResources : UserControl
    {
        public AppXamlDefinedResources()
        {
            this.InitializeComponent();
        }
    }
}
