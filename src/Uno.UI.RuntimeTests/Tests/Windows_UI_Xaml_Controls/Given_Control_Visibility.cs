using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;

using SwipeControl = Microsoft/* UWP don't rename */.UI.Xaml.Controls.SwipeControl;
using TeachingTip = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TeachingTip;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Control_Visibility
{
	[TestMethod]
	[UnoWorkItem("https://github.com/unoplatform/uno/issues/16369")]
	public async Task When_Visibility_Changes()
	{
		foreach (var type in typeof(Control).Assembly.GetTypes())
		{
			if (!type.IsAbstract && type.IsClass && type.IsAssignableTo(typeof(Control)))
			{
				// TODO: Understand why these fail.
				if (type == typeof(CalendarViewDayItem) ||
					type == typeof(ContentDialog) ||
					type == typeof(GridViewHeaderItem) ||
					type == typeof(GridViewItem) ||
					type == typeof(ListViewHeaderItem) ||
					type == typeof(MediaPlayerElement) ||
					type == typeof(PivotItem) ||
					type == typeof(Slider) ||
					type == typeof(SwipeControl) ||
					type == typeof(TeachingTip) ||
#if HAS_UNO
					type == typeof(CalendarViewItem) ||
#endif
					type == typeof(ColorPickerSlider))
				{
					continue;
				}

				Control control;
				try
				{
					control = (Control)Activator.CreateInstance(type);
					control.Width = 100;
					control.Height = 100;
					control.MinHeight = 100;
					control.MinWidth = 100;
					control.MaxWidth = 100;
					control.MaxHeight = 100;
				}
				catch (MissingMethodException)
				{
					// No parameterless constructor. Skip this control.
					continue;
				}

				control.Template = (ControlTemplate)XamlReader.Load("""
					<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
										xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
										xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml">
						<Border Width="100" Height="100" Background="Yellow" />
					</ControlTemplate>
					""");

				var border = new Border()
				{
					Width = 100,
					Height = 100,
					Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
					Child = control,
				};

				await UITestHelper.Load(border);
				var bitmap = await UITestHelper.ScreenShot(border);
				var bounds = ImageAssert.GetColorBounds(bitmap, Microsoft.UI.Colors.Yellow);
				Assert.AreEqual(99, bounds.Width, $"Checking width when visible for {type}");
				Assert.AreEqual(99, bounds.Height, $"Checking height when visible for {type}");

				control.Visibility = Visibility.Collapsed;
				await UITestHelper.WaitForIdle();

				bitmap = await UITestHelper.ScreenShot(border);
				bounds = ImageAssert.GetColorBounds(bitmap, Microsoft.UI.Colors.Red);
				Assert.AreEqual(99, bounds.Width, $"Checking width when collapsed for {type}");
				Assert.AreEqual(99, bounds.Height, $"Checking height when collapsed for {type}");
			}
		}
	}
}
