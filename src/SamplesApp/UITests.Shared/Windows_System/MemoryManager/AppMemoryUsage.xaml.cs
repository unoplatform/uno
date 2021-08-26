using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_System.MemoryManager
{
	[Sample("Windows.System", "MemoryManager.AppMemoryUsage")]
	public sealed partial class AppMemoryUsage : Page
	{
		public AppMemoryUsage()
		{
			this.InitializeComponent();
			SetValueAppMemoryUsage();
		}

		public void SetValueAppMemoryUsage()
		{
			ValueAppMemoryUsage.Text = Windows.System.MemoryManager.AppMemoryUsage.ToString();
		}

	}
}
