using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_Xaml_Markup.VisualStateManager_Lazy_Tests.Controls;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Tests.Helpers;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.VisualStateManager_Lazy_Tests
{
	[TestClass]
	public class VisualStateManager_Lazy
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();

			// Any unit test in any class that sets the theme explicitly
			// should make sure to reset that operation.
			// It's not this test responsibility to fix what other tests break.
			Assert.IsFalse(Application.Current.IsThemeSetExplicitly);
		}

		[TestMethod]
		public async Task When_VisualStateManager_Lazy()
		{
			var SUT = new When_VisualStateManager_Lazy();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(SUT);

			await WaitForIdle();

			Assert.IsNotNull(SUT.testTransition.LazyBuilder);
			Assert.IsNotNull(SUT.Normal.LazyBuilder);
			Assert.IsNotNull(SUT.PointerOver.LazyBuilder);
			Assert.IsNotNull(SUT.Pressed.LazyBuilder);
			Assert.IsNotNull(SUT.Disabled.LazyBuilder);

			await GoTo(nameof(SUT.Normal));

			Assert.IsNotNull(SUT.testTransition.LazyBuilder);
			Assert.IsNull(SUT.Normal.LazyBuilder);
			Assert.IsNotNull(SUT.PointerOver.LazyBuilder);
			Assert.IsNotNull(SUT.Pressed.LazyBuilder);
			Assert.IsNotNull(SUT.Disabled.LazyBuilder);

			await GoTo(nameof(SUT.PointerOver));

			Assert.IsNull(SUT.testTransition.LazyBuilder);
			Assert.IsNull(SUT.Normal.LazyBuilder);
			Assert.IsNull(SUT.PointerOver.LazyBuilder);
			Assert.IsNotNull(SUT.Pressed.LazyBuilder);
			Assert.IsNotNull(SUT.Disabled.LazyBuilder);

			await GoTo(nameof(SUT.Pressed));

			Assert.IsNull(SUT.testTransition.LazyBuilder);
			Assert.IsNull(SUT.Normal.LazyBuilder);
			Assert.IsNull(SUT.PointerOver.LazyBuilder);
			Assert.IsNull(SUT.Pressed.LazyBuilder);
			Assert.IsNotNull(SUT.Disabled.LazyBuilder);

			await GoTo(nameof(SUT.Disabled));

			Assert.IsNull(SUT.testTransition.LazyBuilder);
			Assert.IsNull(SUT.Normal.LazyBuilder);
			Assert.IsNull(SUT.PointerOver.LazyBuilder);
			Assert.IsNull(SUT.Pressed.LazyBuilder);
			Assert.IsNull(SUT.Disabled.LazyBuilder);

			async Task GoTo(string stateName)
			{
				var goToResult = VisualStateManager.GoToState(SUT, stateName, useTransitions: false);
				Assert.IsTrue(goToResult);
				await WaitForIdle();
			}
		}

		[TestMethod]
		public async Task When_VisualStateManager_Lazy_ThemeChanges()
		{
			var SUT = new When_VisualStateManager_Lazy_ThemeChanges();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(SUT);

			await WaitForIdle();

			using var _1 = ThemeHelper.SwapSystemTheme();

			await GoTo("State2");

			Assert.AreEqual(47.0, SUT.myControl.Tag);

			await GoTo("State1");

			_ = ThemeHelper.SwapSystemTheme();

			await GoTo("State2");

			Assert.AreEqual(29.0, SUT.myControl.Tag);

			async Task GoTo(string stateName)
			{
				var goToResult = VisualStateManager.GoToState(SUT, stateName, useTransitions: false);
				Assert.IsTrue(goToResult);
				await WaitForIdle();
			}
		}

		[TestMethod]
		public void When_DefaultButton_Not_Set()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			using var _ = ThemeHelper.SwapSystemTheme();

			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			Assert.AreEqual(ContentDialogButton.None, SUT.DefaultButton);

			ShowDialog(SUT);

			Assert.IsNotNull(SUT.PrimaryButton);

			var fg = SUT.PrimaryButton.Foreground as SolidColorBrush;
			Assert.IsNotNull(fg);
			Assert.AreEqual(Colors.White, fg.Color);
		}

		[TestMethod]
		public async Task When_xName()
		{
			var SUT = new When_VisualStateManager_xName();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(SUT);

			await WaitForIdle();

			Assert.IsNotNull(SUT.State3.LazyBuilder);
			Assert.IsNull(SUT.State2.LazyBuilder);

			Assert.IsNotNull(SUT.testAnimation);
			Assert.IsNotNull(SUT.testKeyFrame);
			Assert.AreEqual("0", SUT.testKeyFrame.Value);
		}

		private static void ShowDialog(MyContentDialog dialog)
		{
			dialog.ForceLoaded();
			_ = dialog.ShowAsync();
		}

#if NETFX_CORE
		private static async Task WaitForIdle() => await Task.Delay(100);
#else
		private static Task WaitForIdle() => Task.CompletedTask;
#endif
	}

	public partial class MyContentDialog : ContentDialog
	{
		public Button PrimaryButton { get; private set; }
		public Border BackgroundElement { get; private set; }

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			PrimaryButton = GetTemplateChild("PrimaryButton") as Button;
			BackgroundElement = GetTemplateChild("BackgroundElement") as Border;
		}
	}
}
