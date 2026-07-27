#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public async Task When_Vertical_Selection_Movement_Preserves_Desired_X()
	{
		var sut = new RichEditBox { Width = 150, Height = 84, TextWrapping = TextWrapping.Wrap };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(
				TextSetOptions.None,
				"one two three four five six seven eight\rsecond paragraph with words\rthird");
			await WindowHelper.WaitForIdle();

			var selection = sut.Document.Selection;
			selection.SetRange(18, 18);
			var original = selection.StartPosition;
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Line, 1, false));
			Assert.AreEqual(1, selection.MoveDown(TextRangeUnit.Line, 1, false));
			Assert.AreEqual(2, selection.MoveUp(TextRangeUnit.Line, 2, false));
			Assert.AreEqual(original, selection.StartPosition);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_AtEndOfLine_Selects_The_Preceding_Wrapped_Line()
	{
		var sut = new RichEditBox { Width = 120, TextWrapping = TextWrapping.Wrap };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "one two three four five six");
			await WindowHelper.WaitForIdle();

			var selection = sut.Document.Selection;
			selection.SetRange(1, 1);
			Assert.IsTrue(selection.EndKey(TextRangeUnit.Line, false) > 0);
			var lineEnd = selection.StartPosition;
			Assert.IsTrue(selection.Options.HasFlag(SelectionOptions.AtEndOfLine));
			Assert.AreEqual(0, selection.MoveUp(TextRangeUnit.Line, 1, false));
			Assert.AreEqual(lineEnd, selection.StartPosition);

			selection.Options &= ~SelectionOptions.AtEndOfLine;
			Assert.AreEqual(1, selection.MoveUp(TextRangeUnit.Line, 1, false));
			Assert.IsTrue(selection.StartPosition < lineEnd);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Rtl_Vertical_Selection_RoundTrips()
	{
		var sut = new RichEditBox
		{
			Width = 150,
			Height = 84,
			TextWrapping = TextWrapping.Wrap,
			FlowDirection = FlowDirection.RightToLeft,
		};
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "אבג דהו זחט יכל מנס עףצ קרש ת\rפסקה שניה ארוכה\rשלישית");
			await WindowHelper.WaitForIdle();

			var selection = sut.Document.Selection;
			selection.SetRange(8, 8);
			selection.GetRect(PointOptions.ClientCoordinates, out var originalRect, out _);
			Assert.AreEqual(2, selection.MoveDown(TextRangeUnit.Line, 2, false));
			Assert.AreEqual(2, selection.MoveUp(TextRangeUnit.Line, 2, false));
			selection.GetRect(PointOptions.ClientCoordinates, out var finalRect, out _);
			Assert.AreEqual(originalRect.X, finalRect.X, 2);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Inline_Image_Selection_Has_Object_Geometry()
	{
		var sut = new RichEditBox { Width = 200 };
		try
		{
			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			sut.Document.SetText(TextSetOptions.None, "ab");
			using var stream = CreateImageStream(SKColors.Red);
			sut.Document.GetRange(1, 1).InsertImage(
				20,
				12,
				8,
				VerticalCharacterAlignment.Baseline,
				"image",
				stream);
			await WindowHelper.WaitForIdle();

			var selection = sut.Document.Selection;
			selection.SetRange(1, 2);
			selection.GetRect(PointOptions.ClientCoordinates, out var rect, out _);

			Assert.AreEqual(SelectionType.InlineShape, selection.Type);
			Assert.IsTrue(rect.Width > 0);
			Assert.IsTrue(rect.Height > 0);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
