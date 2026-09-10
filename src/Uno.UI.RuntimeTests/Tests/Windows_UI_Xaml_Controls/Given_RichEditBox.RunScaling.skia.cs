#nullable enable

using System;
using System.IO;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public void When_Run_Splicing_Edits_Boundaries_And_Middles()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "abcdefgh");
			document.GetRange(0, 2).CharacterFormat.Bold = FormatEffect.On;
			document.GetRange(2, 6).CharacterFormat.Italic = FormatEffect.On;
			document.GetRange(6, 8).CharacterFormat.Underline = UnderlineType.Single;

			document.GetRange(2, 2).Text = "X";
			document.GetRange(4, 5).Text = "YZ";
			document.GetRange(1, 7).Text = string.Empty;

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("afgh", text);
			Assert.AreEqual(FormatEffect.On, document.GetRange(0, 1).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, document.GetRange(1, 2).CharacterFormat.Italic);
			Assert.AreEqual(UnderlineType.Single, document.GetRange(2, 4).CharacterFormat.Underline);
			Assert.IsTrue(document.AreRunIndexesValid());
			Assert.AreEqual(3, document.CharacterRunCount);
		}

		[TestMethod]
		public void When_Paragraph_Run_Splicing_Normalizes_Only_Touched_Paragraphs()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "aa\rbb\rcc");
			document.GetRange(3, 5).ParagraphFormat.Alignment = ParagraphAlignment.Center;
			document.GetRange(6, 8).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			document.GetRange(3, 5).CharacterFormat.Italic = FormatEffect.On;
			document.GetRange(6, 8).CharacterFormat.Underline = UnderlineType.Single;

			document.GetRange(2, 3).Text = string.Empty;
			GetTextWithoutFinalEop(document, out var merged);
			Assert.AreEqual("aabb\rcc", merged);
			Assert.AreEqual(ParagraphAlignment.Left, document.GetRange(0, 4).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Right, document.GetRange(5, 7).ParagraphFormat.Alignment);
			Assert.AreEqual(FormatEffect.On, document.GetRange(2, 4).CharacterFormat.Italic);

			document.GetRange(2, 2).Text = "\r";
			Assert.AreEqual(ParagraphAlignment.Left, document.GetRange(0, 2).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Left, document.GetRange(3, 5).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Right, document.GetRange(6, 8).ParagraphFormat.Alignment);
			Assert.IsTrue(document.AreRunIndexesValid());
		}

		[TestMethod]
		public void When_Formatted_Fragment_Preserves_Links_Images_And_Hidden_Filtering()
		{
			var source = new RichEditBox();
			var target = new RichEditBox();
			source.Document.SetText(TextSetOptions.None, "linkHide");
			source.Document.GetRange(0, 4).Link = "\"https://contoso.example\"";
			source.Document.GetRange(4, 8).CharacterFormat.Hidden = FormatEffect.On;
			source.Document.GetRange(8, 8).InsertImage(
				8,
				8,
				6,
				VerticalCharacterAlignment.Baseline,
				"pic",
				CreateImageStream(SkiaSharp.SKColors.Blue));
			target.Document.SetText(TextSetOptions.None, "XXYY");

			target.Document.GetRange(1, 3).FormattedText = source.Document.GetRange(0, 9);

			GetTextWithoutFinalEop(target.Document, out var raw);
			GetTextWithoutFinalEop(target.Document, TextGetOptions.UseObjectText | TextGetOptions.NoHidden, out var filtered);
			Assert.AreEqual("XlinkHide\ufffcY", raw);
			Assert.AreEqual("XlinkpicY", filtered);
			Assert.AreEqual("\"https://contoso.example\"", target.Document.GetRange(2, 3).Link);
			Assert.AreEqual(FormatEffect.On, target.Document.GetRange(5, 9).CharacterFormat.Hidden);
			Assert.IsTrue(target.Document.AreRunIndexesValid());
		}

		[TestMethod]
		public void When_Collapsed_Format_Uses_Gravity_And_Pending_Format()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "ab");
			document.GetRange(1, 2).CharacterFormat.Bold = FormatEffect.On;
			var backward = document.GetRange(1, 1);
			var forward = document.GetRange(1, 1);
			backward.Gravity = RangeGravity.Backward;
			forward.Gravity = RangeGravity.Forward;

			Assert.AreEqual(FormatEffect.Off, backward.CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, forward.CharacterFormat.Bold);

			forward.CharacterFormat.Italic = FormatEffect.On;
			forward.Text = "X";

			Assert.AreEqual(FormatEffect.On, document.GetRange(1, 2).CharacterFormat.Bold);
			Assert.AreEqual(FormatEffect.On, document.GetRange(1, 2).CharacterFormat.Italic);
			Assert.IsTrue(document.AreRunIndexesValid());
		}

		[TestMethod]
		public void When_Undo_Redo_Preserves_Final_Eop_Adjacent_Run_State()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "a\r");
			document.ClearUndoRedoHistory();
			document.GetRange(2, 2).ParagraphFormat.Alignment = ParagraphAlignment.Right;
			document.ClearUndoRedoHistory();

			document.GetRange(2, 2).Text = "b";
			Assert.AreEqual(ParagraphAlignment.Right, document.GetRange(2, 3).ParagraphFormat.Alignment);
			Assert.AreEqual(ParagraphAlignment.Right, document.GetRange(3, 3).ParagraphFormat.Alignment);
			Assert.IsTrue(document.AreRunIndexesValid());

			document.Undo();
			Assert.AreEqual(2, document.TextLength);
			Assert.AreEqual(ParagraphAlignment.Right, document.GetRange(2, 2).ParagraphFormat.Alignment);
			Assert.IsTrue(document.AreRunIndexesValid());

			document.Redo();
			Assert.AreEqual(3, document.TextLength);
			Assert.AreEqual(ParagraphAlignment.Right, document.GetRange(2, 3).ParagraphFormat.Alignment);
			Assert.IsTrue(document.AreRunIndexesValid());
		}

		[TestMethod]
		public void When_Large_Document_Local_Queries_And_Edits_Keep_Runs_Bounded()
		{
			const int paragraphCount = 4096;
			const int paragraphLength = 64;
			var rtf = new StringBuilder(@"{\rtf1\ansi ");
			for (var i = 0; i < paragraphCount; i++)
			{
				rtf.Append(i % 2 == 0 ? @"\ql\b " : @"\qr\b0 ");
				rtf.Append('a', paragraphLength - 1);
				rtf.Append(@"\par ");
			}
			rtf.Append('}');

			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, rtf.ToString());
			document.ClearUndoRedoHistory();
			var initialCharacterRuns = document.CharacterRunCount;
			var initialParagraphRuns = document.ParagraphRunCount;

			Assert.AreEqual(paragraphCount * paragraphLength, document.TextLength);
			Assert.AreEqual(paragraphCount, initialCharacterRuns);
			Assert.AreEqual(paragraphCount, initialParagraphRuns);
			Assert.IsTrue(document.AreRunIndexesValid());

			var boldQueries = 0;
			for (var i = 0; i < 8192; i++)
			{
				var paragraph = i * 7919 % paragraphCount;
				var position = paragraph * paragraphLength + 10;
				if (document.GetRange(position, position + 1).CharacterFormat.Bold == FormatEffect.On)
				{
					boldQueries++;
				}
				var expectedAlignment = paragraph % 2 == 0 ? ParagraphAlignment.Left : ParagraphAlignment.Right;
				Assert.AreEqual(expectedAlignment, document.GetRange(position, position).ParagraphFormat.Alignment);
			}

			Assert.AreEqual(4096, boldQueries);
			for (var i = 0; i < 32; i++)
			{
				var paragraph = i * 127 % paragraphCount;
				var position = paragraph * paragraphLength + 20;
				document.GetRange(position, position + 1).Text = "z";
			}

			Assert.IsTrue(document.AreRunIndexesValid());
			Assert.IsTrue(document.CharacterRunCount <= initialCharacterRuns + 64);
			Assert.AreEqual(initialParagraphRuns, document.ParagraphRunCount);
		}

		[TestMethod]
		public void When_Rtf_Fragment_Transport_Allocates_Format_State_Per_Run()
		{
			const int paragraphCount = 4096;
			const int paragraphLength = 64;
			var rtf = CreateRunScalingRtf(paragraphCount, paragraphLength, includeHidden: true);

			var (fragment, parseClones) = TrackFormattingClones(() => RichTextRtfCodec.Read(rtf));

			Assert.AreEqual(paragraphCount * paragraphLength, fragment.Text.Length);
			Assert.AreEqual(paragraphCount, fragment.CharacterRuns.Count);
			Assert.AreEqual(paragraphCount, fragment.ParagraphRuns.Count);
			Assert.IsTrue(fragment.AreRunInvariantsValid());
			Assert.IsLessThan(paragraphCount + 16, parseClones.Character);
			Assert.IsLessThan(paragraphCount + 16, parseClones.Paragraph);

			var (exported, writeClones) = TrackFormattingClones(() => RichTextRtfCodec.Write(fragment));
			Assert.AreEqual(0, writeClones.Character);
			Assert.AreEqual(0, writeClones.Paragraph);

			var source = new RichEditBox();
			source.Document.SetText(TextSetOptions.FormatRtf, exported);
			var (captured, captureClones) = TrackFormattingClones(
				() => source.Document.CaptureFragment(0, source.Document.TextLength));
			Assert.AreEqual(paragraphCount, captured.CharacterRuns.Count);
			Assert.AreEqual(paragraphCount, captured.ParagraphRuns.Count);
			Assert.IsLessThan(paragraphCount + 1, captureClones.Character);
			Assert.IsLessThan(paragraphCount + 2, captureClones.Paragraph);

			var (visible, filteredClones) = TrackFormattingClones(
				() => source.Document.CaptureFragment(0, source.Document.TextLength, noHidden: true));
			Assert.AreEqual(paragraphCount / 2 * paragraphLength, visible.Text.Length);
			Assert.IsTrue(visible.AreRunInvariantsValid());
			Assert.IsLessThan(paragraphCount + 1, filteredClones.Character);
			Assert.IsLessThan(paragraphCount + 2, filteredClones.Paragraph);

			var target = new RichEditBox();
			var (_, pasteClones) = TrackFormattingClones(() =>
			{
				target.Document.GetRange(0, 0).FormattedText =
					source.Document.GetRange(0, source.Document.TextLength);
				return true;
			});
			Assert.AreEqual(source.Document.TextLength, target.Document.TextLength);
			Assert.AreEqual(paragraphCount, target.Document.CharacterRunCount);
			Assert.AreEqual(paragraphCount, target.Document.ParagraphRunCount);
			Assert.IsLessThan(paragraphCount * 24, pasteClones.Character);
			Assert.IsLessThan(paragraphCount * 24, pasteClones.Paragraph);
		}

		[TestMethod]
		public void When_Repeated_Rtf_Images_Remain_Distinct_Without_Weakening_Image_Budgets()
		{
			const int imageCount = 64;
			using var imageStream = CreateImageStream(SKColors.Orange);
			using var imageBytes = new MemoryStream();
			imageStream.AsStreamForRead().CopyTo(imageBytes);
			var imageHex = Convert.ToHexString(imageBytes.ToArray());
			var rtf = new StringBuilder(@"{\rtf1");
			for (var i = 0; i < imageCount; i++)
			{
				rtf.Append(@"{\pict\pngblip\picw2\pich2 ").Append(imageHex).Append('}');
			}
			rtf.Append('}');

			var (fragment, parseClones) = TrackFormattingClones(
				() => RichTextRtfCodec.Read(rtf.ToString()));
			Assert.AreEqual(imageCount, fragment.Text.Length);
			Assert.AreEqual(imageCount, fragment.CharacterRuns.Count);
			foreach (var run in fragment.CharacterRuns)
			{
				Assert.AreEqual(1, run.Length);
			}
			Assert.AreEqual(1, fragment.ParagraphRuns.Count);
			Assert.IsTrue(fragment.AreRunInvariantsValid());
			Assert.IsLessThan(imageCount * 3, parseClones.Character);
			Assert.IsLessThan(imageCount * 3, parseClones.Paragraph);

			var (exported, writeClones) = TrackFormattingClones(() => RichTextRtfCodec.Write(fragment));
			Assert.AreEqual(0, writeClones.Character);
			Assert.AreEqual(0, writeClones.Paragraph);

			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.FormatRtf, exported);
			Assert.AreEqual(imageCount, document.TextLength);
			Assert.AreEqual(imageCount, document.CharacterRunCount);

			var overBudget = exported.Insert(exported.Length - 1, exported[exported.IndexOf(@"{\pict", StringComparison.Ordinal)..^1]);
			Assert.ThrowsExactly<ArgumentException>(
				() => new RichEditBox().Document.SetText(TextSetOptions.FormatRtf, overBudget));
		}

		private static string CreateRunScalingRtf(int paragraphCount, int paragraphLength, bool includeHidden)
		{
			var rtf = new StringBuilder(@"{\rtf1\ansi ");
			for (var i = 0; i < paragraphCount; i++)
			{
				rtf.Append(i % 2 == 0 ? @"\ql\b " : @"\qr\b0 ");
				if (includeHidden)
				{
					rtf.Append(i % 2 == 0 ? @"\v0 " : @"\v ");
				}
				rtf.Append('a', paragraphLength - 1);
				rtf.Append(@"\par ");
			}
			rtf.Append('}');
			return rtf.ToString();
		}

		private static (T Result, FormattingStateCloneCounts Counts) TrackFormattingClones<T>(Func<T> action)
		{
			FormattingStateCloneDiagnostics.BeginTracking();
			FormattingStateCloneCounts counts = default;
			T result;
			try
			{
				result = action();
			}
			finally
			{
				counts = FormattingStateCloneDiagnostics.EndTracking();
			}

			return (result, counts);
		}
	}
}
