using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_StartScreen
{
	[SampleControlInfo("Windows.UI.StartScreen", "JumpList", typeof(JumpListTestsViewModel))]
	public sealed partial class JumpListTests : UserControl
	{
		public JumpListTests()
		{
			this.InitializeComponent();
			DataContextChanged += JumpListTests_DataContextChanged;
		}

		internal JumpListTestsViewModel Model { get; private set; }

		private void JumpListTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = args.NewValue as JumpListTestsViewModel;
		}
	}
}
