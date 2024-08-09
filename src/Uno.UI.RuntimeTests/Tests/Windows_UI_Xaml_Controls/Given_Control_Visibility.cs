using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.WinRT.Extensions.UI.Popups;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_Control_Visibility
{
#if HAS_UNO_WINUI
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
					type == typeof(FlipView) ||
#if HAS_UNO
					type == typeof(CalendarViewItem) ||
#endif
#if HAS_UNO_WINUI
					type == typeof(ItemsView) ||
#endif
#if __ANDROID__ || __IOS__
					// OnApplyTemplate crashes with NRE.
					type == typeof(AutoSuggestBox) ||
					type == typeof(TwoPaneView) ||
					type == typeof(NativePivotPresenter) ||
					type == typeof(MessageDialogContentDialog) ||
#endif
#if __ANDROID__
					// RefreshContainer requires a control of type NativeRefreshControl in its hierarchy.
					type == typeof(RefreshContainer) ||
#endif
#if __IOS__
					// Native implementation not found. Make sure FlipView has a style which contains an ItemsPanel of type {nameof(PagedCollectionView)}.
					type == typeof(FlipView) ||
#endif
					type == typeof(MediaTransportControls) || // matches winui
					type == typeof(MenuBarItem) || // matches winui
					type == typeof(ColorPickerSlider)
					)
				{
					continue;
				}

				Console.WriteLine($"Running When_Visibility_Changes test for {type.FullName}");
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
					control.HorizontalAlignment = HorizontalAlignment.Stretch;
					control.VerticalAlignment = VerticalAlignment.Stretch;
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
				// When there is DPI scaling, the Widths can differ by 1px (rounding errors maybe?)
				Assert.AreEqual(bitmap.Width, bounds.Width, delta: 1, $"Checking width when visible for {type}");
				Assert.AreEqual(bitmap.Height, bounds.Height, delta: 1, $"Checking height when visible for {type}");

				control.Visibility = Visibility.Collapsed;
				await UITestHelper.WaitForIdle();

				bitmap = await UITestHelper.ScreenShot(border);
				bounds = ImageAssert.GetColorBounds(bitmap, Microsoft.UI.Colors.Red);
				Assert.AreEqual(bitmap.Width, bounds.Width, delta: 1, $"Checking width when collapsed for {type}");
				Assert.AreEqual(bitmap.Height, bounds.Height, delta: 1, $"Checking height when collapsed for {type}");
			}
		}
	}
#endif
}
