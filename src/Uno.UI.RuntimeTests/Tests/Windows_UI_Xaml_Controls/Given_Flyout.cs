using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Extensions;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Flyout
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Unloaded_Before_Shown()
		{
			var button = new Button()
			{
				Flyout = new Flyout
				{
					Content = new Border { Width = 50, Height = 30 }
				}
			};

			TestServices.WindowHelper.WindowContent = button;

			await TestServices.WindowHelper.WaitForIdle();

			TestServices.WindowHelper.WindowContent = null;

			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task When_Attached_To_Border_Check_Placement()
		{
			var (flyout, content) = CreateFlyout();

			const double MarginValue = 105;
			const int TargetWidth = 88;
			var target = new Border
			{
				Margin = new Thickness(MarginValue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Width = TargetWidth,
				Height = 23,
				Background = new SolidColorBrush(Colors.Red)
			};

			TestServices.WindowHelper.WindowContent = target;

			await TestServices.WindowHelper.WaitForLoaded(target);

			await TestServices.WindowHelper.WaitFor(() => target.ActualWidth == TargetWidth); // For some reason target is initially stretched on iOS

			try
			{
				FlyoutBase.SetAttachedFlyout(target, flyout);
				FlyoutBase.ShowAttachedFlyout(target);

				await TestServices.WindowHelper.WaitForLoaded(content);

				var contentCenter = content.GetOnScreenBounds().GetCenter();
				var targetCenter = target.GetOnScreenBounds().GetCenter();

				Assert.IsTrue(targetCenter.X > MarginValue);
				Assert.AreEqual(targetCenter.X, contentCenter.X, delta: 2);
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		public async Task When_Attached_To_TextBlock_Check_Placement()
		{
			var (flyout, content) = CreateFlyout();

			const double MarginValue = 105;
			var target = new TextBlock
			{
				Margin = new Thickness(MarginValue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Text = "Tweetle beetle battle"
			};

			TestServices.WindowHelper.WindowContent = target;

			await TestServices.WindowHelper.WaitForLoaded(target);

			try
			{
				FlyoutBase.SetAttachedFlyout(target, flyout);
				FlyoutBase.ShowAttachedFlyout(target);

				await TestServices.WindowHelper.WaitForLoaded(content);

				var contentCenter = content.GetOnScreenBounds().GetCenter();
				var targetCenter = target.GetOnScreenBounds().GetCenter();

				Assert.IsTrue(targetCenter.X > MarginValue);
				Assert.AreEqual(targetCenter.X, contentCenter.X, delta: 2);
			}
			finally
			{
				flyout.Hide();
			}
		}

		private (Flyout Flyout, FrameworkElement Content) CreateFlyout()
		{
			var content = new Grid { Height = 64, Width = 64, Background = new SolidColorBrush(Colors.Green) };
			var flyout = new Flyout
			{
				Content = content,
				FlyoutPresenterStyle = GetSimpleFlyoutPresenterStyle()
			};
			return (flyout, content);
		}

		private static Style GetSimpleFlyoutPresenterStyle() => new Style
		{
			TargetType = typeof(FlyoutPresenter),
			Setters =
					{
						new Setter(FlyoutPresenter.PaddingProperty, new Thickness(0)),
						new Setter(FlyoutPresenter.MinWidthProperty, 0d),
						new Setter(FlyoutPresenter.MinHeightProperty, 0d),
					}
		};
	}
}
