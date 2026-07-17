using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ThemeAnimation
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
		public void When_FadeInThemeAnimation_Reparented_To_Timeline()
		{
			// Reparent (7.0 breaking change): the *ThemeAnimation family derives directly
			// from Timeline, matching WinUI, instead of Uno's legacy DoubleAnimation base.
			Assert.AreEqual(typeof(Timeline), typeof(FadeInThemeAnimation).BaseType);
			Assert.AreNotEqual(typeof(DoubleAnimation), typeof(FadeInThemeAnimation).BaseType);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
		public void When_FadeOutThemeAnimation_Reparented_To_Timeline()
		{
			Assert.AreEqual(typeof(Timeline), typeof(FadeOutThemeAnimation).BaseType);
			Assert.AreNotEqual(typeof(DoubleAnimation), typeof(FadeOutThemeAnimation).BaseType);
		}

		[TestMethod]
		public async Task When_FadeInThemeAnimation_Animates_Opacity()
		{
			var target = new Border { Width = 100, Height = 100, Opacity = 0 };
			await UITestHelper.Load(target);

			var animation = new FadeInThemeAnimation().BindTo(target, nameof(UIElement.Opacity));
			await animation.ToStoryboard().RunAsync(TimeSpan.FromSeconds(5), throwsException: true);

			Assert.AreEqual(1.0, target.Opacity, 0.05);
		}

		[TestMethod]
		public async Task When_FadeOutThemeAnimation_Animates_Opacity()
		{
			var target = new Border { Width = 100, Height = 100, Opacity = 1 };
			await UITestHelper.Load(target);

			var animation = new FadeOutThemeAnimation().BindTo(target, nameof(UIElement.Opacity));
			await animation.ToStoryboard().RunAsync(TimeSpan.FromSeconds(5), throwsException: true);

			Assert.AreEqual(0.0, target.Opacity, 0.05);
		}
	}
}
