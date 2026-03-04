using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ImplicitXamlNamespaces
	{
		[TestMethod]
		public async Task When_Page_Has_No_Xmlns_Declarations()
		{
			// T011: Verify a XAML-defined UserControl without explicit xmlns
			// declarations loads and renders standard WinUI controls correctly.
			var control = new ImplicitXamlNamespaces_NoXmlns();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.TestButton, "Button should be created from implicit namespace XAML");
			Assert.IsInstanceOfType(control.TestButton, typeof(Button));
			Assert.AreEqual("Hello", control.TestButton.Content);

			Assert.IsNotNull(control.TestTextBlock, "TextBlock should be created from implicit namespace XAML");
			Assert.IsInstanceOfType(control.TestTextBlock, typeof(TextBlock));
			Assert.AreEqual("World", control.TestTextBlock.Text);
		}

		[TestMethod]
		public async Task When_XName_Works_Without_XmlnsX()
		{
			// T012: Verify x:Name works on elements in XAML without xmlns:x declaration.
			var control = new ImplicitXamlNamespaces_XName();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.NamedButton, "x:Name should resolve Button without explicit xmlns:x");
			Assert.AreEqual("Named", control.NamedButton.Content);

			Assert.IsNotNull(control.NamedTextBlock, "x:Name should resolve TextBlock without explicit xmlns:x");
			Assert.AreEqual("Named Text", control.NamedTextBlock.Text);
		}

		[TestMethod]
		public async Task When_XBind_Works_Without_XmlnsX()
		{
			// T013: Verify x:Bind expressions resolve correctly in XAML
			// without xmlns:x declaration.
			var control = new ImplicitXamlNamespaces_XBind();
			control.TestText = "Bound Value";
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.BoundTextBlock, "TextBlock with x:Bind should exist");
			Assert.AreEqual("Bound Value", control.BoundTextBlock.Text,
				"x:Bind should resolve property without explicit xmlns:x");
		}

		[TestMethod]
		public async Task When_Explicit_Xmlns_Still_Works()
		{
			// T014: Verify XAML with explicit xmlns declarations continues
			// to compile and render identically (backward compatibility).
			var control = new ImplicitXamlNamespaces_ExplicitXmlns();
			TestServices.WindowHelper.WindowContent = control;
			await TestServices.WindowHelper.WaitForLoaded(control);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(control.ExplicitButton, "Button with explicit xmlns should work");
			Assert.IsInstanceOfType(control.ExplicitButton, typeof(Button));
			Assert.AreEqual("Explicit", control.ExplicitButton.Content);

			Assert.IsNotNull(control.ExplicitTextBlock, "TextBlock with explicit xmlns should work");
			Assert.IsInstanceOfType(control.ExplicitTextBlock, typeof(TextBlock));
			Assert.AreEqual("Explicit Xmlns", control.ExplicitTextBlock.Text);
		}

		// T015: Feature disable test - when UnoEnableImplicitXamlNamespaces=false,
		// XAML without xmlns should fail to parse. This is a build-time behavior
		// that cannot be validated at runtime since XAML compilation happens during
		// the build. A separate test project with the flag set to false would be
		// needed to verify opt-out behavior.
	}
}
