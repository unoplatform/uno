using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_Xaml_Markup.VisualStateManager_Lazy_Tests.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.VisualStateManager_Lazy_Tests
{
	[TestClass]
	public class VisualStateManager_Lazy
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
			Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Light);
		}

		[TestMethod]
		public async Task When_VisualStateManager_Lazy_ThemeChanges()
		{
			var SUT = new When_VisualStateManager_Lazy_ThemeChanges();
			var app = UnitTestsApp.App.EnsureApplication();
			app.HostView.Children.Add(SUT);

			await WaitForIdle();

			await SwapSystemTheme();

			await GoTo("State2");

			Assert.AreEqual(47.0, SUT.myControl.Tag);

			await GoTo("State1");

			await SwapSystemTheme();

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
		public async Task When_DefaultButton_Not_Set()
		{
			var app = UnitTestsApp.App.EnsureApplication();

			await SwapSystemTheme();

			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			Assert.AreEqual(ContentDialogButton.None, SUT.DefaultButton);

			await ShowDialog(SUT);

			Assert.IsNotNull(SUT.PrimaryButton);

			var fg = SUT.PrimaryButton.Foreground as SolidColorBrush;
			Assert.IsNotNull(fg);
			Assert.AreEqual(Colors.White, fg.Color);
		}

		private static async Task ShowDialog(MyContentDialog dialog)
		{
			dialog.ForceLoaded();
			dialog.ShowAsync();
		}

#if NETFX_CORE
		private static async Task WaitForIdle() => await Task.Delay(100);
#else
		private static Task WaitForIdle() => Task.CompletedTask;
#endif

		private static async Task<bool> SwapSystemTheme()
		{
			var currentTheme = Application.Current.RequestedTheme;
			var targetTheme = currentTheme == ApplicationTheme.Light ?
				ApplicationTheme.Dark :
				ApplicationTheme.Light;
#if NETFX_CORE
			if (!UnitTestsApp.App.EnableInteractiveTests || targetTheme == ApplicationTheme.Light)
			{
				return false;
			}

			_swapTask = _swapTask ?? GetSwapTask();

			await _swapTask;
#else
			Application.Current.SetExplicitRequestedTheme(targetTheme);
#endif
			Assert.AreEqual(targetTheme, Application.Current.RequestedTheme);
			return true;
		}
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
