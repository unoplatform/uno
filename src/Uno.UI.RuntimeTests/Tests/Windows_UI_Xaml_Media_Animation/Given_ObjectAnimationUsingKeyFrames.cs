using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ObjectAnimationUsingKeyFrames
	{
#if !NETFX_CORE // Disabled on UWP for now because 17763 doesn't support WinUI 2.x
		[TestMethod]
		public async Task When_Theme_Changed_Animated_Value()
		{
			using (UseFluentStyles())
			{
				var checkBox = new MyCheckBox() { Content = "CheckBox", IsEnabled = false };
				TestServices.WindowHelper.WindowContent = checkBox;
				await TestServices.WindowHelper.WaitForIdle();

				Assert.IsNotNull(checkBox.ContentPresenter);

				Assert.AreEqual(Color.FromArgb(102, 0, 0, 0), (checkBox.ContentPresenter.Foreground as SolidColorBrush).Color);

				using (UseDarkTheme())
				{
					await TestServices.WindowHelper.WaitForIdle();

					Assert.AreEqual(Color.FromArgb(102, 255, 255, 255), (checkBox.ContentPresenter.Foreground as SolidColorBrush).Color);
				}
			}
		}
#endif

		/// <summary>
		/// Ensure dark theme is applied for the course of a single test.
		/// </summary>
		private IDisposable UseDarkTheme() => ThemeHelper.UseDarkTheme();

#if !NETFX_CORE // Disabled on UWP for now because 17763 doesn't support WinUI 2.x
		/// <summary>
		/// Ensure Fluent styles are available for the course of a single test.
		/// </summary>
		private IDisposable UseFluentStyles() => StyleHelper.UseFluentStyles();
#endif
	}

	public partial class MyCheckBox : CheckBox
	{
		public ContentPresenter ContentPresenter { get; set; }
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			ContentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter; // This is a ContentPresenter
		}
	}
}
