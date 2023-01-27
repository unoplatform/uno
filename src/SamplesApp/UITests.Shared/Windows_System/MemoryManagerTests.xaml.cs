using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// O modelo de item de Página em Branco está documentado em https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_System
{
	[Sample("Windows.System")]
	public sealed partial class MemoryManagerTests : Page
	{
		public MemoryManagerTests()
		{
			this.InitializeComponent();
		}

		public void SetValueMemoryManager()
		{
			ValueMemoryManager.Text = "AppMemoryUsage: " + Windows.System.MemoryManager.AppMemoryUsage.ToString() + " \n AppMemoryUsageLimit: " + Windows.System.MemoryManager.AppMemoryUsageLimit.ToString();
		}
	}
}
