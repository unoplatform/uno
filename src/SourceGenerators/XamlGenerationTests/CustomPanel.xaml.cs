using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace XamlGenerationTests.Shared
{
	public sealed partial class CustomPanel : StackPanel
	{
#if NETFX_CORE
		public CustomPanel()
		{
			this.InitializeComponent();
		}
#endif
	}
}
