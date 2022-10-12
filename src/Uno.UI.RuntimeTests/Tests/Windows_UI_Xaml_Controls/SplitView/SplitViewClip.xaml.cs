using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml_Controls
{
	public sealed partial class SplitViewClip : Page
    {
        public SplitViewClip()
        {
            this.InitializeComponent();
        }

		public void PanOpen()
		{
			Split.IsPaneOpen = true;
		}

		public void PanClose()
		{
			Split.IsPaneOpen = false;
		}
    }
}
