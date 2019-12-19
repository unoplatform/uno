using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.FlyoutTests
{
	[TestClass]
	public class Given_Flyout
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_ChildIsBigger_PlacementBottom()
		{
			var SUT = new Grid() { Name = "test" };

			var flyout = new Flyout()
			{
				LightDismissOverlayMode = LightDismissOverlayMode.On,
				Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
			};

			var button = new Button()
			{
				Width = 5,
				Flyout = flyout
			};



			SUT.AddChild(button);

			SUT.Measure(new Size(20, 20));
			SUT.Arrange(new Rect(0, 0, 20, 20));

			//button.Click;
			flyout.ShowAt(button);

			Assert.AreEqual(button.LayoutSlot.X, flyout._popup.LayoutSlot.X);
		}

		[TestMethod]
		public void When_Placement_Full()
		{
			var SUT = new Grid() { Name = "test" };

			var flyout = new Flyout()
			{
				Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Full,
				FlyoutPresenterStyle = new Style
				{
					Setters =
					{
						new Setter(FrameworkElement.MaxWidthProperty, double.PositiveInfinity),
						new Setter(FrameworkElement.MaxHeightProperty, double.PositiveInfinity)
					}
				}
			};

			var button = new Button()
			{
				Flyout = flyout
			};
			
			//button.Click;
			flyout.ShowAt(button);

			var presenter = flyout.GetPresenter();
			var panel = flyout.GetPopupPanel();

			var visibleBounds = new Rect(0, 0, 410, 815);
			var applicationView = ApplicationView.GetForCurrentView();
			using (applicationView.SetVisibleBounds(visibleBounds))
			{
				panel.Measure(visibleBounds.Size);
				panel.Arrange(visibleBounds);

				Assert.AreEqual(410d, presenter.ActualWidth);
				Assert.AreEqual(815d, presenter.ActualHeight);
			}
		}

		[TestMethod]
		public void When_Placement_Full_Max_Dims()
		{
			var SUT = new Grid() { Name = "test" };

			var flyout = new Flyout()
			{
				Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Full,
				FlyoutPresenterStyle = new Style
				{
					Setters =
					{
						new Setter(FrameworkElement.MaxWidthProperty, 214d),
						new Setter(FrameworkElement.MaxHeightProperty, 641d)
					}
				}
			};

			var button = new Button()
			{
				Flyout = flyout
			};
			
			//button.Click;
			flyout.ShowAt(button);

			var presenter = flyout.GetPresenter();
			var panel = flyout.GetPopupPanel();

			var visibleBounds = new Rect(0, 0, 410, 815);
			var applicationView = ApplicationView.GetForCurrentView();
			using (applicationView.SetVisibleBounds(visibleBounds))
			{
				panel.Measure(visibleBounds.Size);
				panel.Arrange(visibleBounds);

				Assert.AreEqual(214d, presenter.ActualWidth);
				Assert.AreEqual(641d, presenter.ActualHeight);
			}
		}
	}
}
