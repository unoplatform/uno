#if !WINAPPSDK // Disabled on UWP as tests use Uno-specific APIs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ObjectAnimationUsingKeyFrames
	{

		[TestMethod]
		public async Task When_Theme_Changed_Animated_Value()
		{
			var checkBox = new MyCheckBox() { Content = "CheckBox", IsEnabled = false };
			TestServices.WindowHelper.WindowContent = checkBox;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(checkBox.ContentPresenter);

			var lightThemeForeground = TestsColorHelper.ToColor("#5C000000");
			var darkThemeForeground = TestsColorHelper.ToColor("#5DFFFFFF");

			Assert.AreEqual(lightThemeForeground, (checkBox.ContentPresenter.Foreground as SolidColorBrush).Color);

			using (ThemeHelper.UseDarkTheme())
			{
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(darkThemeForeground, (checkBox.ContentPresenter.Foreground as SolidColorBrush).Color);
			}
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
}
#endif
