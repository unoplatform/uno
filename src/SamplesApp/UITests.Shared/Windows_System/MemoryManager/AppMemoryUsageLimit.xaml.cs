using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_System.MemoryManager
{
	[SampleControlInfo("Windows.System", "MemoryManager.AppMemoryUsageLimit",
		description: "Shows the limit amount of memory that can be used by the application")]
	public sealed partial class AppMemoryUsageLimit : Page
	{
		public AppMemoryUsageLimit()
		{
			this.InitializeComponent();
			SetValueAppMemoryUsageLimit();
		}

		public void SetValueAppMemoryUsageLimit()
		{
			ValueAppMemoryUsageLimit.Text = Windows.System.MemoryManager.AppMemoryUsageLimit.ToString();
		}

	}
}
