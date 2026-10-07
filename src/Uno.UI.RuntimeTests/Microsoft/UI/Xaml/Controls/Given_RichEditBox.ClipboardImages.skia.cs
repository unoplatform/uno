#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task When_BitmapOnly_Clipboard_Replaces_Selection_As_One_Undoable_Object()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				SUT.Document.SetText(TextSetOptions.None, "abcd");
				SUT.Document.Selection.SetRange(1, 3);
				SUT.Document.ClearUndoRedoHistory();
				SetClipboard(bitmap: CreatePng(Microsoft.UI.Colors.Orange));
				await WindowHelper.WaitForIdle();

				Assert.IsTrue(SUT.Document.CanPaste());
				SUT.PasteFromClipboard();
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(SUT.Document, out var value);
					return value == "a\ufffcd";
				});

				GetTextWithoutFinalEop(SUT.Document, TextGetOptions.UseObjectText, out var objectText);
				Assert.AreEqual("ad", objectText);
				Assert.AreEqual(2, SUT.Document.Selection.StartPosition);
				Assert.AreEqual(2, SUT.Document.Selection.EndPosition);
				SUT.Document.Undo();
				GetTextWithoutFinalEop(SUT.Document, out var restored);
				Assert.AreEqual("abcd", restored);
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task When_Clipboard_Has_Text_And_Bitmap_Text_Has_Priority()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var package = new DataPackage();
				package.SetText("text");
				package.SetBitmap(CreateBitmapReference(CreatePng(Microsoft.UI.Colors.Blue)));
				Clipboard.SetContent(package);
				await WindowHelper.WaitForIdle();

				SUT.PasteFromClipboard();
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(SUT.Document, out var value);
					return value == "text";
				});

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual("text", text);
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task When_Bitmap_Clipboard_Respects_MaxLength_ReadOnly_And_Protection()
		{
			var limited = new RichEditBox { MaxLength = 1 };
			var readOnly = new RichEditBox { IsReadOnly = true };
			var protectedBox = new RichEditBox();
			var panel = new StackPanel();
			panel.Children.Add(limited);
			panel.Children.Add(readOnly);
			panel.Children.Add(protectedBox);
			try
			{
				WindowHelper.WindowContent = panel;
				await WindowHelper.WaitForLoaded(panel);
				SetClipboard(bitmap: CreatePng(Microsoft.UI.Colors.Green));
				await WindowHelper.WaitForIdle();

				limited.PasteFromClipboard();
				await WindowHelper.WaitFor(() =>
				{
					GetTextWithoutFinalEop(limited.Document, out var value);
					return value == "\ufffc";
				});
				limited.PasteFromClipboard();
				await WindowHelper.WaitForIdle();
				GetTextWithoutFinalEop(limited.Document, out var limitedText);
				Assert.AreEqual("\ufffc", limitedText);

				readOnly.PasteFromClipboard();
				await WindowHelper.WaitForIdle();
				GetTextWithoutFinalEop(readOnly.Document, out var readOnlyText);
				Assert.AreEqual(string.Empty, readOnlyText);

				protectedBox.Document.SetText(TextSetOptions.None, "ab");
				protectedBox.Document.GetRange(0, 2).CharacterFormat.ProtectedText = FormatEffect.On;
				protectedBox.Document.Selection.SetRange(0, 2);
				protectedBox.PasteFromClipboard();
				await WindowHelper.WaitForIdle();
				GetTextWithoutFinalEop(protectedBox.Document, out var protectedText);
				Assert.AreEqual("ab", protectedText);
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
		public async Task When_Bitmap_Clipboard_Exceeds_Pixel_Budget_No_Object_Is_Inserted()
		{
			var SUT = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = SUT;
				await WindowHelper.WaitForLoaded(SUT);
				var encoded = Uno.UI.RuntimeTests.Helpers.TestPngEncoder.CreateSolidPng(2050, 2050, Microsoft.UI.Colors.Purple);
				SetClipboard(bitmap: encoded);
				await WindowHelper.WaitForIdle();

				SUT.PasteFromClipboard();
				await WindowHelper.WaitForIdle();

				GetTextWithoutFinalEop(SUT.Document, out var text);
				Assert.AreEqual(string.Empty, text);
			}
			finally
			{
				Clipboard.Clear();
				WindowHelper.WindowContent = null;
			}
		}

		private static byte[] CreatePng(Windows.UI.Color color)
			=> Uno.UI.RuntimeTests.Helpers.TestPngEncoder.CreateSolidPng(2, 2, color);

		private static void SetClipboard(byte[] bitmap)
		{
			var package = new DataPackage();
			package.SetBitmap(CreateBitmapReference(bitmap));
			Clipboard.SetContent(package);
		}

		private static RandomAccessStreamReference CreateBitmapReference(byte[] bitmap)
			=> RandomAccessStreamReference.CreateFromStream(new MemoryStream(bitmap).AsRandomAccessStream());
	}
}
