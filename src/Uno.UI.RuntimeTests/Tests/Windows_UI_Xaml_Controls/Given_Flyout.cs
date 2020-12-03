using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
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

		[TestMethod]
		[DataRow(FlyoutPlacementMode.Top, HorizontalPosition.Center, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.Bottom, HorizontalPosition.Center, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.Left, HorizontalPosition.BeyondLeft, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.Right, HorizontalPosition.BeyondRight, VerticalPosition.Center)]
#if NETFX_CORE // Not implemented on Uno https://github.com/unoplatform/uno/issues/4629
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedTop, HorizontalPosition.BeyondLeft, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedBottom, HorizontalPosition.BeyondLeft, VerticalPosition.BottomFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedTop, HorizontalPosition.BeyondRight, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedBottom, HorizontalPosition.BeyondRight, VerticalPosition.BottomFlush)]
#endif
		public async Task Check_Placement_All(
			FlyoutPlacementMode placementMode,
			HorizontalPosition horizontalPosition,
			VerticalPosition verticalPosition)
		{
			var (flyout, content) = CreateFlyout();

			flyout.Placement = placementMode;

			const double MarginValue = 97;
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

				VerifyRelativeContentPosition(horizontalPosition, verticalPosition, content, MarginValue, target);
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		[DataRow(FlyoutPlacementMode.Top, HorizontalPosition.Center, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.Bottom, HorizontalPosition.Center, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.Left, HorizontalPosition.BeyondLeft, VerticalPosition.Center)]
		[DataRow(FlyoutPlacementMode.Right, HorizontalPosition.BeyondRight, VerticalPosition.Center)]
#if NETFX_CORE // Not implemented on Uno https://github.com/unoplatform/uno/issues/4629
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.TopEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondTop)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedLeft, HorizontalPosition.LeftFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.BottomEdgeAlignedRight, HorizontalPosition.RightFlush, VerticalPosition.BeyondBottom)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedTop, HorizontalPosition.BeyondLeft, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.LeftEdgeAlignedBottom, HorizontalPosition.BeyondLeft, VerticalPosition.BottomFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedTop, HorizontalPosition.BeyondRight, VerticalPosition.TopFlush)]
		[DataRow(FlyoutPlacementMode.RightEdgeAlignedBottom, HorizontalPosition.BeyondRight, VerticalPosition.BottomFlush)]
#endif
		public async Task Check_Placement_All_MenuFlyout(
			FlyoutPlacementMode placementMode,
			HorizontalPosition horizontalPosition,
			VerticalPosition verticalPosition)
		{
			var flyout = CreateBasicMenuFlyout();

			flyout.Placement = placementMode;

			const double MarginValue = 97;
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

				var presenter = flyout.Presenter;

				await TestServices.WindowHelper.WaitForLoaded(presenter);

				var content = presenter.FindFirstChild<ScrollViewer>();

				VerifyRelativeContentPosition(horizontalPosition, verticalPosition, content, MarginValue, target);
			}
			finally
			{
				flyout.Hide();
			}
		}

		private static void VerifyRelativeContentPosition(HorizontalPosition horizontalPosition, VerticalPosition verticalPosition, FrameworkElement content, double minimumTargetOffset, Border target)
		{
			var contentScreenBounds = content.GetOnScreenBounds();
			var contentCenter = contentScreenBounds.GetCenter();
			var targetScreenBounds = target.GetOnScreenBounds();
			var targetCenter = targetScreenBounds.GetCenter();

			Assert.IsTrue(targetCenter.X > minimumTargetOffset);
			Assert.IsTrue(targetCenter.Y > minimumTargetOffset);
			switch (horizontalPosition)
			{
				case HorizontalPosition.BeyondLeft:
					NumberAssert.Less(contentScreenBounds.Right, targetScreenBounds.Left);
					break;
				case HorizontalPosition.LeftFlush:
					Assert.AreEqual(targetScreenBounds.Left, contentScreenBounds.Left, delta: 2);
					break;
				case HorizontalPosition.Center:
					Assert.AreEqual(targetCenter.X, contentCenter.X, delta: 2);
					break;
				case HorizontalPosition.RightFlush:
					Assert.AreEqual(targetScreenBounds.Right, contentScreenBounds.Right, delta: 2);
					break;
				case HorizontalPosition.BeyondRight:
					NumberAssert.Greater(contentScreenBounds.Left, targetScreenBounds.Right);
					break;
			}

			switch (verticalPosition)
			{
				case VerticalPosition.BeyondTop:
					NumberAssert.Less(contentScreenBounds.Bottom, targetScreenBounds.Top);
					break;
				case VerticalPosition.TopFlush:
					Assert.AreEqual(targetScreenBounds.Top, contentScreenBounds.Top, delta: 2);
					break;
				case VerticalPosition.Center:
					Assert.AreEqual(targetCenter.Y, contentCenter.Y, delta: 2);
					break;
				case VerticalPosition.BottomFlush:
					Assert.AreEqual(targetScreenBounds.Bottom, contentScreenBounds.Bottom, delta: 2);
					break;
				case VerticalPosition.BeyondBottom:
					NumberAssert.Greater(contentScreenBounds.Top, targetScreenBounds.Bottom);
					break;
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

		private MyMenuFlyout CreateBasicMenuFlyout()
		{
			var flyout = new MyMenuFlyout
			{
				Items =
				{
					new MenuFlyoutItem { Text = "Red" },
					new MenuFlyoutItem { Text = "Blue" },
					new MenuFlyoutItem { Text = "Green" },
				}
			};

			return flyout;
		}

		public enum HorizontalPosition
		{
			BeyondLeft,
			LeftFlush,
			Center,
			RightFlush,
			BeyondRight
		}

		public enum VerticalPosition
		{
			BeyondTop,
			TopFlush,
			Center,
			BottomFlush,
			BeyondBottom
		}
	}

	public partial class MyMenuFlyout : MenuFlyout
	{
		public MenuFlyoutPresenter Presenter { get; private set; }

		protected override Control CreatePresenter()
		{
			var presenter = base.CreatePresenter();
			Presenter = presenter as MenuFlyoutPresenter;
			return presenter;
		}
	}
}
