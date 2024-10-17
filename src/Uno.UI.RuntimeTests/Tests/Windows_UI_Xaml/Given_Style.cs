using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;
using SamplesApp.UITests;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	// The attribute is required when running WinUI. See:
	// https://github.com/microsoft/microsoft-ui-xaml/issues/4723#issuecomment-812753123
	[Bindable]
	public sealed partial class ThrowingElement : FrameworkElement
	{
		public ThrowingElement() => throw new Exception("Inner exception");
	}

	[TestClass]
	public class Given_Style
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_StyleFailsToApply()
		{
			var controlTemplate = (ControlTemplate)XamlReader.Load("""
				<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
								 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
								 xmlns:local="using:Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml">
					<local:ThrowingElement />
				</ControlTemplate>
				""");

			var style = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.TemplateProperty, controlTemplate)
				}
			};

			// This shouldn't throw.
			_ = new ContentControl() { Style = style };
		}

		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15460")]
#if __ANDROID__
		[Ignore("Doesn't pass in CI on Android")]
#endif
		public async Task When_ImplicitStyle()
		{
			var implicitStyle = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch)
				},
				TargetType = typeof(ContentControl),
			};

			var explicitStyle = new Style()
			{
				TargetType = typeof(ContentControl),
			};

			var cc = new ContentControl() { Width = 100, Height = 100 };

			// On Android and iOS, ContentControl fails to load if it doesn't have content.
			cc.Content = new Border() { Width = 100, Height = 100 };

			Assert.AreEqual(HorizontalAlignment.Center, cc.HorizontalContentAlignment);

			cc.Resources.Add(typeof(ContentControl), implicitStyle);
			await UITestHelper.Load(cc);

			Assert.AreEqual(HorizontalAlignment.Stretch, cc.HorizontalContentAlignment);

			cc.Style = explicitStyle;

			Assert.AreEqual(HorizontalAlignment.Left, cc.HorizontalContentAlignment);
		}
	}
}
