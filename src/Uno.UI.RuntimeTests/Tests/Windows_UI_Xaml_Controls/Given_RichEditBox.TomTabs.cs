#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		private const int TomTabHere = -1;
		private const int TomTabNext = -2;
		private const int TomTabBack = -3;
		private const int EInvalidArg = unchecked((int)0x80070057);

		[TestMethod]
		public async Task When_Paragraph_GetTab_Special_Selectors_Match_WinUI()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "0123456789\rabcdefghij");

				var first = editor.Document.GetRange(0, 10).ParagraphFormat;
				first.ClearAllTabs();
				first.AddTab(36, TabAlignment.Left, TabLeader.Spaces);
				first.AddTab(72, TabAlignment.Center, TabLeader.Dots);
				first.AddTab(108, TabAlignment.Right, TabLeader.Dashes);

				foreach (var defaultTabStop in new[] { 36f, 48f, 72f })
				{
					editor.Document.DefaultTabStop = defaultTabStop;
					AssertSpecialTabSelectors(editor.Document.GetRange(0, 0).ParagraphFormat);
					AssertSpecialTabSelectors(editor.Document.GetRange(5, 5).ParagraphFormat);
					AssertSpecialTabSelectors(editor.Document.GetRange(0, 5).ParagraphFormat);

					editor.Document.Selection.SetRange(5, 5);
					AssertSpecialTabSelectors(editor.Document.Selection.ParagraphFormat);
				}

				first.GetTab(0, out var firstPosition, out var firstAlignment, out var firstLeader);
				first.GetTab(1, out var secondPosition, out var secondAlignment, out var secondLeader);
				first.GetTab(2, out var thirdPosition, out var thirdAlignment, out var thirdLeader);
				Assert.AreEqual(36f, firstPosition);
				Assert.AreEqual(TabAlignment.Left, firstAlignment);
				Assert.AreEqual(TabLeader.Spaces, firstLeader);
				Assert.AreEqual(72f, secondPosition);
				Assert.AreEqual(TabAlignment.Center, secondAlignment);
				Assert.AreEqual(TabLeader.Dots, secondLeader);
				Assert.AreEqual(108f, thirdPosition);
				Assert.AreEqual(TabAlignment.Right, thirdAlignment);
				Assert.AreEqual(TabLeader.Dashes, thirdLeader);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Paragraph_GetTab_Empty_And_Invalid_Indices_Match_WinUI()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "text");

				var format = editor.Document.GetRange(0, 0).ParagraphFormat;
				format.ClearAllTabs();
				Assert.AreEqual(0, format.TabCount);
				AssertInvalidTabIndex(format, TomTabBack);
				AssertInvalidTabIndex(format, TomTabNext);
				AssertInvalidTabIndex(format, TomTabHere);
				AssertInvalidTabIndex(format, 0);

				format.AddTab(36, TabAlignment.Left, TabLeader.Spaces);
				format.AddTab(72, TabAlignment.Center, TabLeader.Dots);
				format.AddTab(108, TabAlignment.Right, TabLeader.Dashes);
				AssertInvalidTabIndex(format, -4);
				AssertInvalidTabIndex(format, TomTabBack);
				AssertInvalidTabIndex(format, format.TabCount);
				AssertInvalidTabIndex(format, 99);
				AssertInvalidTabIndex(format, int.MinValue);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		public async Task When_Paragraph_GetTab_Mixed_Tabs_Returns_Undefined()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "first\rsecond");

				var first = editor.Document.GetRange(0, 5).ParagraphFormat;
				first.ClearAllTabs();
				first.AddTab(36, TabAlignment.Left, TabLeader.Spaces);
				first.AddTab(72, TabAlignment.Center, TabLeader.Dots);
				first.AddTab(108, TabAlignment.Right, TabLeader.Dashes);
				editor.Document.GetRange(6, 12).ParagraphFormat.ClearAllTabs();

				var mixed = editor.Document.GetRange(0, 12).ParagraphFormat;
				Assert.AreEqual(TextConstants.UndefinedInt32Value, mixed.TabCount);
				foreach (var index in new[] { -4, TomTabBack, TomTabNext, TomTabHere, 0, 1, 3, 99 })
				{
					mixed.GetTab(index, out var position, out var alignment, out var leader);
					Assert.AreEqual(TextConstants.UndefinedFloatValue, position);
					Assert.AreEqual(TabAlignment.Left, alignment);
					Assert.AreEqual(TabLeader.Spaces, leader);
				}
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		private static void AssertSpecialTabSelectors(ITextParagraphFormat format)
		{
			Assert.AreEqual(3, format.TabCount);

			format.GetTab(TomTabHere, out var herePosition, out var hereAlignment, out var hereLeader);
			Assert.AreEqual(0f, herePosition);
			Assert.AreEqual(TabAlignment.Left, hereAlignment);
			Assert.AreEqual(TabLeader.Spaces, hereLeader);

			format.GetTab(TomTabNext, out var nextPosition, out var nextAlignment, out var nextLeader);
			Assert.AreEqual(36f, nextPosition);
			Assert.AreEqual(TabAlignment.Left, nextAlignment);
			Assert.AreEqual(TabLeader.Spaces, nextLeader);

			AssertInvalidTabIndex(format, TomTabBack);
		}

		private static void AssertInvalidTabIndex(ITextParagraphFormat format, int index)
		{
			var exception = Assert.ThrowsExactly<ArgumentException>(
				() => format.GetTab(index, out _, out _, out _));
			Assert.AreEqual(EInvalidArg, exception.HResult);
		}
	}
}
