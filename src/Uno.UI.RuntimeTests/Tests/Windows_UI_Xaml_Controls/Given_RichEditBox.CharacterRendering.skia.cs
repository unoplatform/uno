#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_AllCaps_Projects_Without_Changing_Tom_Indices()
		{
			const string source = "iMix";
			var editor = new RichEditBox { Width = 240, TextWrapping = TextWrapping.NoWrap };
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, source);
				var format = editor.Document.GetRange(0, source.Length).CharacterFormat;
				format.AllCaps = FormatEffect.On;
				format.LanguageTag = "tr-TR";
				format.Kerning = 1;
				format.TextScript = TextScript.Turkish;
				await WindowHelper.WaitForIdle();

				var block = GetDisplayBlock(editor);
				var run = block.Inlines.OfType<Run>().Single();
				Assert.AreEqual("İMİX", run.Text);
				Assert.AreEqual("tr-TR", run.RichEditLanguageTag);
				Assert.AreEqual(TextScript.Turkish, run.RichEditTextScript);
				Assert.AreEqual(1f, run.RichEditKerningThreshold);

				GetTextWithoutFinalEop(editor.Document, out var documentText);
				Assert.AreEqual(source, documentText);
				Assert.AreEqual(source.Length, editor.Document.TextLength);
				var end = block.ParsedText.GetRectForIndex(source.Length);
				Assert.AreEqual(
					source.Length,
					block.ParsedText.GetIndexAt(new Point(end.X - 0.25, end.Y + end.Height / 2), false, true));
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SmallCaps_Outline_And_Position_Update_Existing_Run()
		{
			var editor = new RichEditBox { Width = 240 };
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "Mixed");
				var format = editor.Document.GetRange(0, 5).CharacterFormat;
				format.SmallCaps = FormatEffect.On;
				format.Outline = FormatEffect.On;
				format.Position = 3;
				await WindowHelper.WaitForIdle();

				var run = GetDisplayBlock(editor).Inlines.OfType<Run>().Single();
				Assert.AreEqual("Mixed", run.Text);
				Assert.IsTrue(run.RichEditSmallCaps);
				Assert.IsTrue(run.RichEditOutline);
				Assert.AreEqual(4f, run.RichEditBaselineOffset, 0.01f);

				format.SmallCaps = FormatEffect.Off;
				format.Outline = FormatEffect.Off;
				format.Position = -2;
				await WindowHelper.WaitForIdle();

				run = GetDisplayBlock(editor).Inlines.OfType<Run>().Single();
				Assert.IsFalse(run.RichEditSmallCaps);
				Assert.IsFalse(run.RichEditOutline);
				Assert.AreEqual(-8f / 3f, run.RichEditBaselineOffset, 0.01f);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Subscript_And_Superscript_Project_Size_And_Baseline()
		{
			var editor = new RichEditBox { Width = 320, FontSize = 24 };
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				editor.Document.SetText(TextSetOptions.None, "baseSUPsub");
				editor.Document.GetRange(4, 7).CharacterFormat.Superscript = FormatEffect.On;
				editor.Document.GetRange(7, 10).CharacterFormat.Subscript = FormatEffect.On;
				await WindowHelper.WaitForIdle();

				var runs = GetDisplayBlock(editor).Inlines.OfType<Run>().ToArray();
				Assert.HasCount(3, runs);
				Assert.AreEqual("base", runs[0].Text);
				Assert.AreEqual("SUP", runs[1].Text);
				Assert.AreEqual("sub", runs[2].Text);
				Assert.IsTrue(runs[1].FontSize < runs[0].FontSize);
				Assert.IsTrue(runs[2].FontSize < runs[0].FontSize);
				Assert.IsGreaterThan(0, runs[1].RichEditBaselineOffset);
				Assert.IsLessThan(0, runs[2].RichEditBaselineOffset);

				var parsed = GetDisplayBlock(editor).ParsedText;
				var superscript = parsed.GetRectForIndex(4);
				var subscript = parsed.GetRectForIndex(7);
				Assert.IsGreaterThan(0, superscript.Height);
				Assert.AreEqual(superscript.Height, subscript.Height, 0.1);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ColorFont_Setting_Continues_To_Project_To_DisplayBlock()
		{
			var editor = new RichEditBox();
			try
			{
				WindowHelper.WindowContent = editor;
				await WindowHelper.WaitForLoaded(editor);
				var block = GetDisplayBlock(editor);

				Assert.IsTrue(block.IsColorFontEnabled);
				editor.IsColorFontEnabled = false;
				await WindowHelper.WaitForIdle();
				Assert.IsFalse(block.IsColorFontEnabled);
				editor.IsColorFontEnabled = true;
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(block.IsColorFontEnabled);
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
