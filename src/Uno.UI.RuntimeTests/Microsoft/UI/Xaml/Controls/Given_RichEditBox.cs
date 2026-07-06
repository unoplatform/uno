using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#if __SKIA__
using Uno.UI.Extensions;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_RichEditBox
	{
#if __SKIA__
		[TestMethod]
		public async Task When_Document_SetText_GetText_RoundTrips()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Hello RichEditBox");
			SUT.Document.GetText(TextGetOptions.None, out var text);

			Assert.AreEqual("Hello RichEditBox", text);
		}

		[TestMethod]
		public async Task When_Document_And_TextDocument_Are_Same_Instance()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(SUT.Document);
			Assert.AreSame(SUT.Document, SUT.TextDocument);
		}

		[TestMethod]
		public async Task When_SetText_Renders_In_DisplayBlock()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			SUT.Document.SetText(TextSetOptions.None, "Rendered content");
			await WindowHelper.WaitForIdle();

			var contentElement = SUT.FindFirstChild<ScrollViewer>(sv => sv.Name == "ContentElement");
			Assert.IsNotNull(contentElement);

			var displayBlock = contentElement.Content as TextBlock;
			Assert.IsNotNull(displayBlock);
			Assert.AreEqual("Rendered content", displayBlock.Text);
		}

		[TestMethod]
		public async Task When_Placeholder_Reflects_Emptiness()
		{
			var SUT = new RichEditBox { PlaceholderText = "Type here" };
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var placeholder = SUT.FindFirstChild<TextBlock>(tb => tb.Name == "PlaceholderTextContentPresenter");
			Assert.IsNotNull(placeholder);
			Assert.AreEqual(Visibility.Visible, placeholder.Visibility);

			SUT.Document.SetText(TextSetOptions.None, "Not empty anymore");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility);
		}

		[TestMethod]
		public async Task When_Header_Presenter_Visibility_Follows_Header()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			var header = SUT.FindFirstChild<ContentPresenter>(cp => cp.Name == "HeaderContentPresenter");
			Assert.IsNotNull(header);
			Assert.AreEqual(Visibility.Collapsed, header.Visibility);

			SUT.Header = "A header";
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Visible, header.Visibility);
		}

		[TestMethod]
		public async Task When_IsReadOnly_Is_Reflected()
		{
			var SUT = new RichEditBox();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsReadOnly);
			SUT.IsReadOnly = true;
			Assert.IsTrue(SUT.IsReadOnly);
		}
#endif
	}
}
