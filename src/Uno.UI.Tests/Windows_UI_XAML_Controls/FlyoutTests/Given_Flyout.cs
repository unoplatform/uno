using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Tests.Windows_UI_XAML_Controls.FlyoutTests.Controls;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
			var app = UnitTestsApp.App.EnsureApplication();

			var SUT = new Grid() { Name = "test" };

			var flyout = new Flyout()
			{
				LightDismissOverlayMode = LightDismissOverlayMode.On,
				Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
			};

			var button = new Button()
			{
				Width = 5,
				Flyout = flyout
			};

			SUT.AddChild(button);

			app.HostView.Children.Add(SUT);

			SUT.Measure(new Size(20, 20));
			SUT.Arrange(new Rect(0, 0, 20, 20));

			//button.Click;
			button.Focus(FocusState.Programmatic);
			flyout.ShowAt(button);

			Assert.AreEqual(button.LayoutSlot.X, flyout._popup.LayoutSlot.X);
		}

		[TestMethod]
		public void When_Focus_Properties_Set_On_Flyout_Propagate_To_Content()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var appContent = new Grid() { Name = "test" };
			var SUT = new Button();
			var flyoutContent = new Grid()
			{
				Children =
				{
					SUT
				}
			};

			var flyout = new Flyout()
			{
				AllowFocusOnInteraction = false,
				AllowFocusWhenDisabled = true,
				Content = flyoutContent
			};

			var flyoutOwner = new Button()
			{
				Flyout = flyout
			};

			appContent.AddChild(flyoutOwner);

			app.HostView.Children.Add(appContent);

			appContent.Measure(new Size(20, 20));
			appContent.Arrange(new Rect(0, 0, 20, 20));

			flyout.ShowAt(flyoutOwner);

			Assert.AreEqual(false, SUT.AllowFocusOnInteraction);
			Assert.AreEqual(true, SUT.AllowFocusWhenDisabled);

			// Change values
			flyout.AllowFocusOnInteraction = true;
			flyout.AllowFocusWhenDisabled = false;

			Assert.AreEqual(true, SUT.AllowFocusOnInteraction);
			Assert.AreEqual(false, SUT.AllowFocusWhenDisabled);
		}

		[TestMethod]
		public void When_Focus_Properties_Set_On_Flyout_Propagate_To_Popup()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var appContent = new Grid() { Name = "test" };
			var flyoutContent = new Grid()
			{
				Children =
				{
					new Button()
				}
			};

			var flyout = new Flyout()
			{
				AllowFocusOnInteraction = false,
				AllowFocusWhenDisabled = true,
				Content = flyoutContent
			};

			var flyoutOwner = new Button()
			{
				Flyout = flyout
			};

			appContent.AddChild(flyoutOwner);

			app.HostView.Children.Add(appContent);

			appContent.Measure(new Size(20, 20));
			appContent.Arrange(new Rect(0, 0, 20, 20));

			flyout.ShowAt(flyoutOwner);

			var popupPanel = flyout.GetPopupPanel();
			var SUT = popupPanel.Popup;

			Assert.AreEqual(false, SUT.AllowFocusOnInteraction);
			Assert.AreEqual(true, SUT.AllowFocusWhenDisabled);

			// Change values
			flyout.AllowFocusOnInteraction = true;
			flyout.AllowFocusWhenDisabled = false;

			Assert.AreEqual(true, SUT.AllowFocusOnInteraction);
			Assert.AreEqual(false, SUT.AllowFocusWhenDisabled);
		}

		[TestMethod]
		public void When_Placement_Full()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			var flyout = new Flyout()
			{
				Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Full,
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

			button.ForceLoaded();

			//button.Click;
			button.Focus(FocusState.Programmatic);
			flyout.ShowAt(button);

			var presenter = flyout.GetPresenter();
			var panel = flyout.GetPopupPanel();

			var visibleBounds = new Rect(0, 0, 410, 815);
			var applicationView = ApplicationView.GetForCurrentView();
			using (applicationView.SetTemporaryVisibleBounds(visibleBounds))
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
			var app = UnitTestsApp.App.EnsureApplication();

			var flyout = new Flyout()
			{
				Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Full,
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

			button.ForceLoaded();

			//button.Click;
			button.Focus(FocusState.Programmatic);
			flyout.ShowAt(button);

			var presenter = flyout.GetPresenter();
			var panel = flyout.GetPopupPanel();

			var visibleBounds = new Rect(0, 0, 410, 815);
			var applicationView = ApplicationView.GetForCurrentView();
			using (applicationView.SetTemporaryVisibleBounds(visibleBounds))
			{
				panel.Measure(visibleBounds.Size);
				panel.Arrange(visibleBounds);

				Assert.AreEqual(214d, presenter.ActualWidth);
				Assert.AreEqual(641d, presenter.ActualHeight);
			}
		}

		[TestMethod]
		public void When_Xaml_And_Conflicting_ClassName()
		{
			// This particular test is validating that the class is not using
			// another type with the same non-qualified name, which can cause
			// base type lookups to be invalid.

			var app = UnitTestsApp.App.EnsureApplication();

			var SUT = new Grid() { Name = "test" };

			var flyout = new Windows_UI_XAML_Controls.FlyoutTests.Controls.SettingsFlyout()
			{
				Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
			};

			var button = new Button()
			{
				Width = 5,
				Flyout = flyout
			};

			SUT.AddChild(button);

			app.HostView.Children.Add(SUT);
		}
	}
}
