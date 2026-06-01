using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class CommandBarFlyout_Leak : UserControl, ITrackingLeakTest
	{
		public CommandBarFlyout_Leak()
		{
			InitializeComponent();
		}

		public event EventHandler<DependencyObject> ObjectTrackingRequested;

		public async Task WaitForTestToComplete()
		{
			FlyoutBase.ShowAttachedFlyout(Btn);
			await Task.Yield();
			await Task.Yield();

			var flyout = FlyoutBase.GetAttachedFlyout(Btn);
			ObjectTrackingRequested?.Invoke(this, flyout);

			if (flyout is CommandBarFlyout commandBarFlyout)
			{
				foreach (var primaryCommand in commandBarFlyout.PrimaryCommands)
				{
					ObjectTrackingRequested?.Invoke(this, primaryCommand as DependencyObject);
				}
				foreach (var secondaryCommand in commandBarFlyout.SecondaryCommands)
				{
					ObjectTrackingRequested?.Invoke(this, secondaryCommand as DependencyObject);
				}
				commandBarFlyout.Hide();
			}
			else
			{
				throw new InvalidOperationException("Expected CommandBarFlyout, but got " + flyout?.GetType().Name);
			}
		}
	}
}
