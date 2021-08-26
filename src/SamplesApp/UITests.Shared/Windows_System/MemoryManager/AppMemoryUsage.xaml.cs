using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_System.MemoryManager
{
	[SampleControlInfo("Windows.System", "MemoryManager.AppMemoryUsage",
		description: "Shows the amount of memory used by the application")]
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
