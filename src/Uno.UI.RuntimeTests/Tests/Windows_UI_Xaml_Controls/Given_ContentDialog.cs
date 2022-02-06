using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ContentDialog
	{
		[TestMethod]
		public async Task When_Not_FullSizeDesired()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			try
			{
				await ShowDialog(SUT);

				Assert.IsNotNull(SUT.BackgroundElement);

				var actualHeight = SUT.BackgroundElement.ActualHeight;
				Assert.IsTrue(actualHeight > 0); // Is displayed
				Assert.IsTrue(actualHeight < 300); // Is not stretched
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		public async Task When_FullSizeDesired()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SUT.FullSizeDesired = true;


			try
			{
				await ShowDialog(SUT);


				Assert.IsNotNull(SUT.BackgroundElement);

				var actualHeight = SUT.BackgroundElement.ActualHeight;
				Assert.IsTrue(actualHeight > 0); // Is displayed
				Assert.IsTrue(actualHeight > 400); // Is stretched to full size
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		public async Task When_DefaultButton_Not_Set()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			Assert.AreEqual(ContentDialogButton.None, SUT.DefaultButton);


			try
			{
				await ShowDialog(SUT);


				Assert.IsNotNull(SUT.PrimaryButton);

				var fg = SUT.PrimaryButton.Foreground as SolidColorBrush;
				Assert.IsNotNull(fg);
				Assert.AreEqual(Colors.Black, fg.Color);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		public async Task When_DefaultButton_Set()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SUT.DefaultButton = ContentDialogButton.Primary;


			try
			{
				await ShowDialog(SUT);


				Assert.IsNotNull(SUT.PrimaryButton);

				var fg = SUT.PrimaryButton.Foreground as SolidColorBrush;
				Assert.IsNotNull(fg);
				Assert.AreEqual(Colors.White, fg.Color);
			}
			finally
			{
				SUT.Hide();
			}
		}

		[TestMethod]
		public async Task When_CloseDeferred()
		{
			var SUT = new MyContentDialog
			{
				Title = "Dialog title",
				Content = "Dialog content",
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			bool triggered = false;
			bool hideSecondTime = false;

			async void SUT_Closing(object sender, ContentDialogClosingEventArgs args)
			{
				// Closing should only be invoked once.
				Assert.IsFalse(triggered);
				triggered = true;
				var deferral = args.GetDeferral();
				await WindowHelper.WaitFor(() => hideSecondTime);
				deferral.Complete();
				triggered = false;
			};

			SUT.Closing += SUT_Closing;

			try
			{
				await ShowDialog(SUT);

				SUT.Hide();
				await WindowHelper.WaitFor(() => triggered);
				SUT.Hide();
				hideSecondTime = true;
			}
			finally
			{
				SUT.Closing -= SUT_Closing;
				SUT.Hide();
			}
		}

		[TestMethod]
		[Ignore("Test applies to platforms using software keyboard https://github.com/unoplatform/uno/issues/7995")]
		public async Task When_Soft_Keyboard_And_VisibleBounds()
		{
			var nativeUnsafeArea = ScreenHelper.GetUnsafeArea();

			using (ScreenHelper.OverrideVisibleBounds(new Thickness(0, 38, 0, 72), skipIfHasNativeUnsafeArea: (nativeUnsafeArea.Top + nativeUnsafeArea.Bottom) > 50))
			{
				var tb = new TextBox
				{
					Height = 1200
				};


				var SUT = new MyContentDialog
				{
					Title = "Dialog title",
					Content = new ScrollViewer
					{
						Content = tb
					},
					PrimaryButtonText = "Accept",
					SecondaryButtonText = "Nope"
				};

				try
				{
					await ShowDialog(SUT);

					var originalButtonBounds = SUT.PrimaryButton.GetOnScreenBounds();
					var originalBackgroundBounds = SUT.BackgroundElement.GetOnScreenBounds();
					var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
					RectAssert.Contains(visibleBounds, originalButtonBounds);

					var inputPane = InputPane.GetForCurrentView();
					RectAssert.AreEqual(default, inputPane.OccludedRect); // Soft keyboard should not already be visible

					await FocusTextBoxWithSoftKeyboard(tb);

					var occludedRect = inputPane.OccludedRect;
					var shiftedButtonBounds = SUT.PrimaryButton.GetOnScreenBounds();
					var shiftedBackgroundBounds = SUT.BackgroundElement.GetOnScreenBounds();

					NumberAssert.Greater(originalButtonBounds.Bottom, occludedRect.Top); // Button's original position should be occluded, otherwise test is pointless
					NumberAssert.Greater(originalBackgroundBounds.Bottom, occludedRect.Top); // ditto background
					NumberAssert.Less(shiftedButtonBounds.Bottom, occludedRect.Top); // Button should be shifted to be visible (along with rest of dialog) while keyboard is open
					NumberAssert.Less(shiftedBackgroundBounds.Bottom, occludedRect.Top); // ditto background
					;
				}
				finally
				{
					SUT.Hide();
				}
			}
		}

		private async Task FocusTextBoxWithSoftKeyboard(TextBox textBox)
		{
			var tcs = new TaskCompletionSource<bool>();
			var inputPane = InputPane.GetForCurrentView();
			void OnShowing(InputPane sender, InputPaneVisibilityEventArgs args)
			{
				tcs.SetResult(true);
			}
			try
			{
				inputPane.Showing += OnShowing;
				textBox.Focus(FocusState.Programmatic);
				await tcs.Task;
			}
			finally
			{
				inputPane.Showing -= OnShowing;
			}
			await WindowHelper.WaitForIdle();
		}

		private static async Task ShowDialog(MyContentDialog dialog)
		{
			dialog.ShowAsync();
			await WindowHelper.WaitFor(() => dialog.BackgroundElement != null);
#if !NETFX_CORE
			await WindowHelper.WaitFor(() => dialog.BackgroundElement.ActualHeight > 0); // This is necessary on current version of Uno because the template is materialized too early  
#endif
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
