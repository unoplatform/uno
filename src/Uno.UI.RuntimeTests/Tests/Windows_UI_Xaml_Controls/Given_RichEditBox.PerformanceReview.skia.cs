#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Loaded_Million_Character_Local_Key_Edit_Stays_Range_Based()
	{
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(new FakeImeTextBoxExtension());
		const int length = 1_000_000;
		var editor = new RichEditBox
		{
			Width = 480,
			Height = 120,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.None, new string('a', length));
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Focus(FocusState.Programmatic);
			editor.Document.Selection.SetRange(length / 2, length / 2);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			var first = block.Inlines[0];
			var last = block.Inlines[^1];
			var fragmentCreations = editor.RenderFragmentCreationCount;
			var fullDiffs = editor.RenderFullDiffCount;
			editor.Document.ResetTextBufferDiagnosticsForTesting();

			RaiseKey(editor, VirtualKey.None, unicodeKey: 'Z');
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(length + 1, editor.Document.TextLength);
			Assert.AreEqual("aZa", editor.Document.GetTextInRange(length / 2 - 1, length / 2 + 2));
			Assert.AreEqual(0, editor.Document.TextBufferFullMaterializationCount);
			Assert.AreEqual(fullDiffs, editor.RenderFullDiffCount);
			Assert.IsTrue(editor.RenderFragmentCreationCount - fragmentCreations <= 4);
			Assert.AreSame(first, block.Inlines[0]);
			Assert.AreSame(last, block.Inlines[^1]);
			Assert.IsTrue(editor.AreRenderedFragmentsValid());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Large_List_Paragraphs_Are_Locally_Edited_Layout_Stays_Incremental()
	{
		const int paragraphCount = 1_000;
		const int runsPerParagraph = 10;
		var editor = new RichEditBox
		{
			Width = 480,
			Height = 120,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			var rtf = new StringBuilder(@"{\rtf1\ansi ");
			for (var paragraphIndex = 0; paragraphIndex < paragraphCount; paragraphIndex++)
			{
				for (var runIndex = 0; runIndex < runsPerParagraph; runIndex++)
				{
					rtf.Append(runIndex % 2 == 0 ? @"\b x" : @"\b0 x");
				}
				rtf.Append(@"\par ");
			}
			rtf.Append('}');
			editor.Document.SetText(TextSetOptions.FormatRtf, rtf.ToString());
			var list = editor.Document.GetRange(0, editor.Document.TextLength).ParagraphFormat;
			list.ListType = MarkerType.Arabic;
			list.ListStyle = MarkerStyle.Period;
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(editor.UsesBoundedRichLayout);
			Assert.AreEqual(paragraphCount, editor.BoundedRichLayoutCachedParagraphCount);
			var fullDiffs = editor.RenderFullDiffCount;
			var paragraphRebuilds = editor.BoundedRichLayoutParagraphRebuildCount;
			var runVisits = editor.BoundedRichLayoutRunVisitCount;
			var editPosition = (runsPerParagraph + 1) * (paragraphCount / 2) + runsPerParagraph / 2;

			editor.Document.GetRange(editPosition, editPosition + 1).Text = "X";
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(fullDiffs, editor.RenderFullDiffCount);
			Assert.AreEqual(paragraphCount, editor.BoundedRichLayoutCachedParagraphCount);
			Assert.IsLessThanOrEqualTo(
				2,
				editor.BoundedRichLayoutParagraphRebuildCount - paragraphRebuilds);
			Assert.IsLessThanOrEqualTo(20, editor.BoundedRichLayoutRunVisitCount - runVisits);
			Assert.AreEqual(MarkerType.Arabic, editor.Document.GetRange(0, 0).ParagraphFormat.ListType);
			Assert.AreEqual("xXx", editor.Document.GetTextInRange(editPosition - 1, editPosition + 2));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Bounded_Layout_Ends_Large_Story_Is_Released()
	{
		var editor = new RichEditBox
		{
			Width = 480,
			Height = 120,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.None, new string('x', 2 * 1024 * 1024 + 1));
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(editor.UsesBoundedRichLayout);
			Assert.AreNotEqual(0, editor.BoundedRichLayoutCachedParagraphCount);

			editor.Document.SetText(TextSetOptions.None, "small");
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(editor.UsesBoundedRichLayout);
			Assert.AreEqual(0, editor.BoundedRichLayoutCachedParagraphCount);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Thousands_Of_Indexed_Runs_Are_Locally_Edited_Trees_Stay_Bounded()
	{
		const int runCount = 32_768;
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(runCount, 1));
		var editRange = document.GetRange(0, 0);

		for (var i = 0; i < 512; i++)
		{
			var position = (i * 7919) % document.TextLength;
			editRange.SetRange(position, position + 1);
			editRange.Text = ((char)('A' + i % 26)).ToString();
		}

		Assert.IsTrue(document.AreRunIndexesValid());
		Assert.IsTrue(document.CharacterRunIndexTreeHeight <= 96);
		Assert.IsTrue(document.ParagraphRunIndexTreeHeight <= 96);
		Assert.IsGreaterThan(runCount / 2, document.CharacterRunCount);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Thousands_Of_Automation_Children_Refresh_Uses_One_Identity_Lookup_Each()
	{
		const int linkCount = 2048;
		var editor = new RichEditBox
		{
			Width = 480,
			Height = 120,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.None, string.Concat(Enumerable.Repeat("x ", linkCount)));
			editor.Document.BatchDisplayUpdates();
			try
			{
				for (var i = 0; i < linkCount; i++)
				{
					editor.Document.GetRange(i * 2, i * 2 + 1).Link = $"\"https://contoso.example/{i}\"";
				}
			}
			finally
			{
				editor.Document.ApplyDisplayUpdates();
			}

			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var peer = (RichEditBoxAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(editor)!;
			var textProvider = (ITextProvider)peer.GetPattern(PatternInterface.Text)!;
			var before = textProvider.DocumentRange.GetChildren();
			Assert.HasCount(linkCount, before);

			editor.Document.GetRange(0, 0).Text = "y";
			await WindowHelper.WaitForIdle();
			var after = textProvider.DocumentRange.GetChildren();

			Assert.HasCount(linkCount, after);
			Assert.AreEqual(linkCount, peer.TextObjectIdentityLookupCount);
			Assert.AreSame(before[0].AutomationPeer, after[0].AutomationPeer);
			Assert.AreSame(before[^1].AutomationPeer, after[^1].AutomationPeer);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Large_Rich_Clipboard_Payload_Defers_Bounded_Rtf_Generation()
	{
		const int length = 1_000_000;
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, new string('c', length));
		document.ResetClipboardDiagnosticsForTesting();
		document.ResetTextBufferDiagnosticsForTesting();

		var package = document.CreateClipboardDataPackage(0, document.TextLength);
		Assert.IsNotNull(package);
		Assert.AreEqual(0, document.TextBufferFullMaterializationCount);
		Assert.AreEqual(0, document.ClipboardRtfGenerationCount);

		var view = package.GetView();
		Assert.IsTrue(view.Contains(StandardDataFormats.Text));
		Assert.IsTrue(view.Contains(StandardDataFormats.Rtf));
		var rtf = await view.GetRtfAsync();

		Assert.AreEqual(1, document.ClipboardRtfGenerationCount);
		Assert.IsLessThanOrEqualTo(RichTextRtfCodec.MaxRtfOutputLength, rtf.Length);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Maximum_Math_Projection_Uses_One_Compact_Index_Array()
	{
		var projection = new string('m', MathDocument.MaxProjectionLength);
		var editor = new RichEditBox
		{
			Width = 480,
			Height = 120,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			editor.Document.SetMathMode(RichEditMathMode.MathOnly);
			editor.Document.SetMathML(
				$"<math xmlns=\"{MathDocument.NamespaceName}\"><mtext>{projection}</mtext></math>");
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(MathDocument.MaxProjectionLength, editor.Document.TextLength);
			Assert.AreEqual(
				checked((MathDocument.MaxProjectionLength + 1) * 24),
				editor.MathIndexStorageByteCount);
			Assert.IsLessThan(7 * 1024 * 1024, editor.MathIndexStorageByteCount);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	// Mono's conservative GC does not deterministically reclaim weakly referenced strings.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaUIKit)]
	public void When_Large_Initial_Story_Is_Replaced_Or_Deleted_Original_Source_Is_Collectible(bool delete)
	{
		var document = new RichEditBox().Document;
		var source = ReplaceLargeInitialStory(document, delete);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.IsFalse(source.TryGetTarget(out _));
		Assert.AreEqual(delete ? 0 : 11, document.TextLength);
		Assert.IsTrue(document.AreTextBufferInvariantsValid());
	}

	[TestMethod]
	public void When_Million_Grapheme_Cluster_Cache_Uses_Shared_Integer_Boundaries()
	{
		const int length = 1_000_000;
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, new string('g', length));
		document.ResetTextBufferDiagnosticsForTesting();

		var clusters = document.GetUnitBoundaries(TextRangeUnit.Cluster);
		Assert.IsNotNull(clusters);
		Assert.AreEqual(length + 1, clusters.Count);
		Assert.AreEqual(0, document.TextBufferFullMaterializationCount);
		Assert.IsLessThanOrEqualTo(checked((length + 1) * sizeof(int)), document.TextElementBoundaryStorageBytes);
		Assert.AreEqual(0, document.GetUnitBoundaryOwnedStorageBytes(TextRangeUnit.Cluster));
		Assert.AreSame(clusters, document.GetUnitBoundaries(TextRangeUnit.Cluster));

		document.GetRange(length / 2, length / 2 + 1).Text = "z";
		Assert.AreEqual(length / 2, document.GetTextElementStart(length / 2));
		Assert.AreEqual(0, document.TextBufferFullMaterializationCount);
	}

	[TestMethod]
	public void When_Many_Rtf_Control_Tokens_Parse_Without_Control_Word_Strings()
	{
		const int tokenCount = 100_000;
		var rtf = new StringBuilder(@"{\rtf1\ansi\ansicpg1252\deff0 ");
		for (var i = 0; i < tokenCount; i++)
		{
			rtf.Append(@"\b0 ").Append('x');
		}
		rtf.Append('}');

		RichTextRtfCodec.ResetParserDiagnosticsForTesting();
		var fragment = RichTextRtfCodec.Read(rtf.ToString());

		Assert.AreEqual(tokenCount, fragment.Text.Length);
		Assert.AreEqual(1, fragment.CharacterRuns.Count);
		Assert.AreEqual(0, RichTextRtfCodec.ControlWordStringAllocationCount);
	}

	[TestMethod]
	public void When_Formatting_Clone_Diagnostics_Are_Compiled_Out_Of_Release_Hot_Paths()
	{
		var character = typeof(FormattingStateCloneDiagnostics).GetMethod(
			"RecordCharacterClone",
			BindingFlags.Static | BindingFlags.NonPublic);
		var paragraph = typeof(FormattingStateCloneDiagnostics).GetMethod(
			"RecordParagraphClone",
			BindingFlags.Static | BindingFlags.NonPublic);

		Assert.AreEqual("DEBUG", character?.GetCustomAttribute<ConditionalAttribute>()?.ConditionString);
		Assert.AreEqual("DEBUG", paragraph?.GetCustomAttribute<ConditionalAttribute>()?.ConditionString);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference<string> ReplaceLargeInitialStory(RichEditTextDocument document, bool delete)
	{
		var text = new string('q', 1_000_000);
		var reference = new WeakReference<string>(text);
		document.SetText(TextSetOptions.None, text);
		document.GetRange(0, document.TextLength).Text = delete ? string.Empty : "replacement";
		document.ClearUndoRedoHistory();
		return reference;
	}
}
