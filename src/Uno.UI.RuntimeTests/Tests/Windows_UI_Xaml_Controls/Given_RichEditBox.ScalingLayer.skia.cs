#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Thousands_Of_Runs_Are_Edited_Only_Local_Inlines_Are_Replaced()
	{
		const int runCount = 2048;
		const int runLength = 4;
		var editor = new RichEditBox
		{
			Width = 480,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(runCount, runLength));
			editor.Document.GetRange(8, 12).Link = "\"https://contoso.example\"";
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			Assert.IsGreaterThanOrEqualTo(runCount, block.Inlines.Count);
			var first = block.Inlines[0];
			var link = block.Inlines.OfType<Hyperlink>().Single();
			var last = block.Inlines[^1];
			var createdBefore = editor.RenderFragmentCreationCount;
			var specifiedBefore = editor.RenderFragmentSpecificationCount;
			var splicesBefore = editor.RenderSpliceCount;

			for (var i = 0; i < 64; i++)
			{
				var run = 256 + i * 17;
				var position = run * runLength + 1;
				editor.Document.GetRange(position, position + 1).Text = ((char)('A' + i % 26)).ToString();
			}
			await WindowHelper.WaitForIdle();

			Assert.AreSame(first, block.Inlines[0]);
			Assert.AreSame(last, block.Inlines[^1]);
			Assert.AreSame(link, block.Inlines.OfType<Hyperlink>().Single());
			Assert.AreEqual(1, block.Inlines.Count(inline => ReferenceEquals(inline, link)));
			Assert.IsTrue(
				editor.RenderFragmentCreationCount - createdBefore <= 128,
				$"Created {editor.RenderFragmentCreationCount - createdBefore} fragments for 64 local edits.");
			Assert.IsTrue(
				editor.RenderFragmentSpecificationCount - specifiedBefore <= 256,
				$"Specified {editor.RenderFragmentSpecificationCount - specifiedBefore} fragments for 64 local edits.");
			Assert.AreEqual(64, editor.RenderSpliceCount - splicesBefore);
			Assert.IsTrue(editor.AreRenderedFragmentsValid());

			var insertionPoint = editor.Document.TextLength / 2;
			editor.Document.GetRange(insertionPoint, insertionPoint).Text = "ZZ";
			editor.Document.GetRange(insertionPoint + 2, insertionPoint + 6).CharacterFormat.Italic = FormatEffect.On;
			await WindowHelper.WaitForIdle();

			Assert.AreSame(first, block.Inlines[0]);
			Assert.AreSame(last, block.Inlines[^1]);
			Assert.AreSame(link, block.Inlines.OfType<Hyperlink>().Single());
			Assert.IsTrue(editor.AreRenderedFragmentsValid());
			Assert.IsTrue(editor.Document.AreRenderProfilesValid());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Rich_Inline_Cap_Uses_Bounded_Custom_Layout()
	{
		var editor = new RichEditBox
		{
			Width = 480,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(8200, 1));
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			Assert.IsTrue(editor.UsesBoundedRichLayout);
			Assert.HasCount(0, block.Inlines);
			Assert.AreEqual(1, editor.BoundedRichLayoutRetainedInlineCount);
			Assert.IsLessThanOrEqualTo(128, editor.BoundedRichLayoutRetainedResourceCount);
			Assert.AreEqual(0, editor.RenderFragmentCreationCount);
			Assert.IsGreaterThan(0, editor.BoundedRichLayoutCreateCount);
			Assert.IsGreaterThan(0, block.ParsedText.GetRectForIndex(editor.Document.TextLength - 1).Height);
			Assert.IsTrue(editor.AreRenderedFragmentsValid());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Twenty_Thousand_Rich_Fragments_Render_Without_Unbounded_Inlines()
	{
		const int runCount = 20_000;
		var editor = new RichEditBox
		{
			Width = 360,
			Height = 100,
			TextWrapping = TextWrapping.Wrap,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(runCount, 1));
			editor.Document.BatchDisplayUpdates();
			try
			{
				var imagePositions = new[] { 19_000, 10_000, 100 };
				var imageNames = new[] { "late stress image", "middle stress image", "early stress image" };
				var imageColors = new[] { SKColors.Cyan, SKColors.Purple, SKColors.Orange };
				for (var i = 0; i < imagePositions.Length; i++)
				{
					using var stream = CreateImageStream(imageColors[i]);
					editor.Document.GetRange(imagePositions[i], imagePositions[i])
						.InsertImage(18, 14, 10, VerticalCharacterAlignment.Baseline, imageNames[i], stream);
				}

				var linkPositions = new[] { 250, 10_050, 19_500 };
				var linkColors = new[] { Microsoft.UI.Colors.Red, Microsoft.UI.Colors.Lime, Microsoft.UI.Colors.Blue };
				for (var i = 0; i < linkPositions.Length; i++)
				{
					var linkRange = editor.Document.GetRange(linkPositions[i], linkPositions[i] + 8);
					linkRange.Link = $"\"https://contoso.example/{linkPositions[i]}\"";
					linkRange.CharacterFormat.ForegroundColor = linkColors[i];
				}

				SetBackground(editor.Document, 20, Microsoft.UI.Colors.Red);
				SetBackground(editor.Document, 11_000, Microsoft.UI.Colors.Lime);
				SetBackground(editor.Document, 19_950, Microsoft.UI.Colors.Blue);
			}
			finally
			{
				editor.Document.ApplyDisplayUpdates();
			}

			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			Assert.IsTrue(editor.UsesBoundedRichLayout);
			Assert.HasCount(0, block.Inlines);
			Assert.AreEqual(1, editor.BoundedRichLayoutRetainedInlineCount);
			Assert.IsLessThanOrEqualTo(128, editor.BoundedRichLayoutRetainedResourceCount);
			Assert.AreEqual(0, editor.RenderFragmentCreationCount);
			Assert.IsGreaterThanOrEqualTo(runCount, editor.Document.CharacterRunCount);
			Assert.IsTrue(editor.AreRenderedFragmentsValid());
			Assert.IsTrue(editor.Document.AreRenderProfilesValid());

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
			var provider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(provider);
			var children = provider.DocumentRange.GetChildren();
			var links = children.Where(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Hyperlink).ToArray();
			Assert.HasCount(3, links);
			foreach (var link in links)
			{
				Assert.IsInstanceOfType<IInvokeProvider>(link.AutomationPeer?.GetPattern(PatternInterface.Invoke));
				Assert.AreEqual(
					true,
					provider.RangeFromChild(link).GetAttributeValue((int)AutomationTextAttributesEnum.LinkAttribute));
			}
			var images = children.Where(child =>
				child.AutomationPeer?.GetAutomationControlType() == AutomationControlType.Image).ToArray();
			Assert.HasCount(3, images);
			CollectionAssert.AreEquivalent(
				new[] { "early stress image", "middle stress image", "late stress image" },
				images.Select(image => image.AutomationPeer?.GetName()).ToArray());
			foreach (var image in images)
			{
				Assert.AreEqual(image.AutomationPeer?.GetName(), provider.RangeFromChild(image).GetText(-1));
			}

			var finalImagePositions = new[] { 100, 10_001, 19_002 };
			foreach (var imagePosition in finalImagePositions)
			{
				editor.Document.GetRange(imagePosition, imagePosition + 1).ScrollIntoView(PointOptions.Start);
				await WindowHelper.WaitForIdle();
				editor.Document.GetRange(imagePosition, imagePosition + 1)
					.GetRect(PointOptions.ClientCoordinates, out var rect, out _);
				Assert.IsGreaterThan(10, rect.Width);
				Assert.IsGreaterThan(8, rect.Height);
			}

			await AssertImageVisible(editor, 100, Microsoft.UI.Colors.Orange);
			await AssertImageVisible(editor, 10_001, Microsoft.UI.Colors.Purple);
			await AssertImageVisible(editor, 19_002, Microsoft.UI.Colors.Cyan);
			await AssertForegroundVisible(editor, 250, Microsoft.UI.Colors.Red);
			await AssertForegroundVisible(editor, 10_050, Microsoft.UI.Colors.Lime);
			await AssertForegroundVisible(editor, 19_500, Microsoft.UI.Colors.Blue);
			await AssertBackgroundVisible(editor, block, 20, Microsoft.UI.Colors.Red);
			await AssertBackgroundVisible(editor, block, 11_000, Microsoft.UI.Colors.Lime);
			await AssertBackgroundVisible(editor, block, 19_950, Microsoft.UI.Colors.Blue);

			editor.Document.Selection.SetRange(19_950, 19_951);
			Assert.AreEqual(19_950, editor.Document.Selection.StartPosition);
			Assert.AreEqual(19_951, editor.Document.Selection.EndPosition);

			var layoutCreates = editor.BoundedRichLayoutCreateCount;
			var runVisits = editor.BoundedRichLayoutRunVisitCount;
			var fragmentCreations = editor.RenderFragmentCreationCount;
			editor.Document.ResetTextBufferDiagnosticsForTesting();
			editor.Document.GetRange(11_000, 11_001).Text = "Z";
			SetBackground(editor.Document, 11_000, Microsoft.UI.Colors.Yellow);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(editor.UsesBoundedRichLayout);
			Assert.HasCount(0, block.Inlines);
			Assert.AreEqual(fragmentCreations, editor.RenderFragmentCreationCount);
			Assert.IsLessThanOrEqualTo(4, editor.BoundedRichLayoutCreateCount - layoutCreates);
			Assert.IsLessThanOrEqualTo(
				runCount * 4L + 64,
				editor.BoundedRichLayoutRunVisitCount - runVisits);
			if (OperatingSystem.IsMacOS() || OperatingSystem.IsBrowser())
			{
				// Native accessibility bridges request the complete value after a RichEditBox state change.
				Assert.IsLessThanOrEqualTo(1, editor.Document.TextBufferFullMaterializationCount);
			}
			else
			{
				Assert.AreEqual(0, editor.Document.TextBufferFullMaterializationCount);
			}
			Assert.IsTrue(editor.AreRenderedFragmentsValid());
			Assert.AreEqual("Z", editor.Document.GetTextInRange(11_000, 11_001));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}

		static void SetBackground(RichEditTextDocument document, int position, global::Windows.UI.Color color)
			=> document.GetRange(position, position + 1).CharacterFormat.BackgroundColor = color;

		static async Task AssertBackgroundVisible(
			RichEditBox editor,
			TextBlock block,
			int position,
			global::Windows.UI.Color color)
		{
			var rect = block.ParsedText.GetRectForIndex(position);
			var hit = block.ParsedText.GetIndexAt(
				new global::Windows.Foundation.Point(rect.X + Math.Max(0.5, rect.Width / 2), rect.Y + rect.Height / 2),
				ignoreEndingNewLine: false,
				extendedSelection: true);
			Assert.IsTrue(hit >= position && hit <= position + 1);

			var range = editor.Document.GetRange(position, position + 1);
			range.ScrollIntoView(PointOptions.Start);
			var bounds = await WaitForRenderedColor(editor, color, tolerance: 10);
			Assert.IsTrue(
				bounds is { Width: > 2, Height: > 4 },
				$"Expected {color} formatting at {position}, bounds were {bounds}.");
		}

		static async Task AssertImageVisible(
			RichEditBox editor,
			int position,
			global::Windows.UI.Color color)
		{
			var range = editor.Document.GetRange(position, position + 1);
			range.ScrollIntoView(PointOptions.Start);
			var bounds = await WaitForRenderedColor(editor, color, tolerance: 20);
			Assert.IsTrue(
				bounds is { Width: > 8, Height: > 6 },
				$"Expected inline image {color} at {position}, bounds were {bounds}.");
		}

		static async Task AssertForegroundVisible(
			RichEditBox editor,
			int position,
			global::Windows.UI.Color color)
		{
			var range = editor.Document.GetRange(position, position + 1);
			range.ScrollIntoView(PointOptions.Start);
			var bounds = await WaitForRenderedColor(editor, color, tolerance: 20);
			Assert.IsTrue(
				bounds is { Width: > 1, Height: > 2 },
				$"Expected link foreground {color} at {position}, bounds were {bounds}.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Seeded_Randomized_Rich_Visual_Stress_Remains_Consistent()
	{
		const int seed = 0x524542;
		const int runCount = 12_000;
		const int randomizedFormatCount = 256;
		const int markerLength = 8;
		const int editCount = 24;
		var random = new Random(seed);
		var editor = new RichEditBox
		{
			Width = 360,
			Height = 100,
			TextWrapping = TextWrapping.Wrap,
		};
		try
		{
			var document = editor.Document;
			document.SetText(TextSetOptions.FormatRtf, BuildAlternatingRunRtf(runCount, 1));

			var zoneWidth = runCount / 9;
			var positions = new int[9];
			for (var i = 0; i < positions.Length; i++)
			{
				var zoneStart = i * zoneWidth;
				positions[i] = random.Next(zoneStart + 64, zoneStart + zoneWidth - 64);
			}

			var palette = new[]
			{
				global::Windows.UI.Color.FromArgb(255, 255, 73, 29),
				global::Windows.UI.Color.FromArgb(255, 127, 43, 223),
				global::Windows.UI.Color.FromArgb(255, 17, 181, 109),
				global::Windows.UI.Color.FromArgb(255, 229, 31, 91),
				global::Windows.UI.Color.FromArgb(255, 31, 211, 71),
				global::Windows.UI.Color.FromArgb(255, 37, 83, 229),
				global::Windows.UI.Color.FromArgb(255, 211, 41, 211),
				global::Windows.UI.Color.FromArgb(255, 31, 199, 211),
				global::Windows.UI.Color.FromArgb(255, 229, 199, 31),
			};
			Shuffle(palette, random);
			var markers = new List<RandomRichVisualMarker>(9);

			document.BatchDisplayUpdates();
			try
			{
				var neutralColors = new[]
				{
					global::Windows.UI.Color.FromArgb(255, 48, 48, 48),
					global::Windows.UI.Color.FromArgb(255, 96, 96, 96),
					global::Windows.UI.Color.FromArgb(255, 144, 144, 144),
					global::Windows.UI.Color.FromArgb(255, 208, 208, 208),
				};
				for (var i = 0; i < randomizedFormatCount; i++)
				{
					var start = random.Next(16, runCount - 24);
					var range = document.GetRange(start, start + random.Next(1, 7));
					switch (random.Next(5))
					{
						case 0:
							range.CharacterFormat.Bold = random.Next(2) == 0 ? FormatEffect.On : FormatEffect.Off;
							break;
						case 1:
							range.CharacterFormat.Italic = random.Next(2) == 0 ? FormatEffect.On : FormatEffect.Off;
							break;
						case 2:
							range.CharacterFormat.Underline = random.Next(2) == 0
								? UnderlineType.Single
								: UnderlineType.None;
							break;
						case 3:
							range.CharacterFormat.ForegroundColor = neutralColors[random.Next(neutralColors.Length)];
							break;
						default:
							range.CharacterFormat.BackgroundColor = neutralColors[random.Next(neutralColors.Length)];
							break;
					}
				}

				for (var i = 0; i < 3; i++)
				{
					var color = palette[i];
					var name = $"seeded-image-{i}";
					using var stream = CreateImageStream(new SKColor(color.R, color.G, color.B, color.A));
					document.GetRange(positions[i], positions[i]).InsertImage(
						18,
						14,
						10,
						VerticalCharacterAlignment.Baseline,
						name,
						stream);
					markers.Add(new RandomRichVisualMarker(
						positions[i],
						1,
						color,
						RandomRichVisualMarkerKind.Image,
						name));
				}

				for (var i = 0; i < 3; i++)
				{
					var position = positions[i + 3];
					var color = palette[i + 3];
					var link = $"\"https://contoso.example/seeded/{i}\"";
					var range = document.GetRange(position, position + markerLength);
					range.Link = link;
					range.CharacterFormat.ForegroundColor = color;
					markers.Add(new RandomRichVisualMarker(
						position,
						markerLength,
						color,
						RandomRichVisualMarkerKind.ForegroundLink,
						link));
				}

				for (var i = 0; i < 3; i++)
				{
					var position = positions[i + 6];
					var color = palette[i + 6];
					document.GetRange(position, position + markerLength).CharacterFormat.BackgroundColor = color;
					markers.Add(new RandomRichVisualMarker(
						position,
						markerLength,
						color,
						RandomRichVisualMarkerKind.Background,
						null));
				}
			}
			finally
			{
				document.ApplyDisplayUpdates();
			}

			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			Assert.IsTrue(editor.UsesBoundedRichLayout);
			Assert.HasCount(0, block.Inlines);
			Assert.AreEqual(1, editor.BoundedRichLayoutRetainedInlineCount);
			Assert.IsLessThanOrEqualTo(128, editor.BoundedRichLayoutRetainedResourceCount);
			Assert.AreEqual(0, editor.RenderFragmentCreationCount);
			Assert.IsGreaterThanOrEqualTo(
				runCount - randomizedFormatCount * 2,
				document.CharacterRunCount);

			foreach (var marker in markers)
			{
				var range = document.GetRange(marker.Position, marker.Position + marker.Length);
				switch (marker.Kind)
				{
					case RandomRichVisualMarkerKind.Image:
						range.GetText(TextGetOptions.UseObjectText, out var objectText);
						Assert.AreEqual(marker.Metadata, objectText);
						break;
					case RandomRichVisualMarkerKind.ForegroundLink:
						Assert.AreEqual(marker.Metadata, range.Link);
						Assert.AreEqual(marker.Color, range.CharacterFormat.ForegroundColor);
						break;
					case RandomRichVisualMarkerKind.Background:
						Assert.AreEqual(marker.Color, range.CharacterFormat.BackgroundColor);
						break;
				}

				var rect = block.ParsedText.GetRectForIndex(marker.Position);
				var hit = block.ParsedText.GetIndexAt(
					new global::Windows.Foundation.Point(
						rect.X + Math.Max(0.5, rect.Width / 2),
						rect.Y + rect.Height / 2),
					ignoreEndingNewLine: false,
					extendedSelection: true);
				Assert.IsTrue(
					hit >= marker.Position && hit <= marker.Position + marker.Length,
					$"Seed {seed}: hit {hit} missed {marker.Kind} marker at {marker.Position}.");
				if (marker.Kind == RandomRichVisualMarkerKind.Image)
				{
					Assert.IsGreaterThan(10, rect.Width);
					Assert.IsGreaterThan(8, rect.Height);
				}
			}

			var edits = new List<RandomRichTextEdit>(editCount);
			var usedEditPositions = new HashSet<int>();
			while (edits.Count < editCount)
			{
				var position = random.Next(32, document.TextLength - 32);
				if (!usedEditPositions.Add(position)
					|| markers.Any(marker =>
						position >= marker.Position - 4
						&& position < marker.Position + marker.Length + 4))
				{
					continue;
				}

				var before = document.GetTextInRange(position, position + 1);
				if (before.Length != 1 || before[0] == '\ufffc')
				{
					continue;
				}

				edits.Add(new RandomRichTextEdit(
					position,
					before,
					((char)('A' + edits.Count % 26)).ToString()));
			}

			document.ClearUndoRedoHistory();
			document.ResetTextBufferDiagnosticsForTesting();
			var layoutCreates = editor.BoundedRichLayoutCreateCount;
			var runVisits = editor.BoundedRichLayoutRunVisitCount;
			document.BatchDisplayUpdates();
			try
			{
				document.BeginUndoGroup();
				try
				{
					foreach (var edit in edits)
					{
						document.GetRange(edit.Position, edit.Position + 1).Text = edit.After;
					}
				}
				finally
				{
					document.EndUndoGroup();
				}
			}
			finally
			{
				document.ApplyDisplayUpdates();
			}
			await WindowHelper.WaitForIdle();
			foreach (var edit in edits)
			{
				Assert.AreEqual(edit.After, document.GetTextInRange(edit.Position, edit.Position + 1));
			}

			document.Undo();
			await WindowHelper.WaitForIdle();
			foreach (var edit in edits)
			{
				Assert.AreEqual(edit.Before, document.GetTextInRange(edit.Position, edit.Position + 1));
			}

			document.Redo();
			await WindowHelper.WaitForIdle();
			foreach (var edit in edits)
			{
				Assert.AreEqual(edit.After, document.GetTextInRange(edit.Position, edit.Position + 1));
			}

			Assert.IsLessThanOrEqualTo(
				3,
				document.TextBufferFullMaterializationCount,
				"Grouped history may materialize one snapshot for the edit, undo, and redo transitions.");
			Assert.AreEqual(0, editor.RenderFragmentCreationCount);
			Assert.IsLessThanOrEqualTo(12, editor.BoundedRichLayoutCreateCount - layoutCreates);
			Assert.IsLessThanOrEqualTo(
				runCount * 12L + 1024,
				editor.BoundedRichLayoutRunVisitCount - runVisits);
			Assert.IsTrue(document.CharacterRunIndexTreeHeight <= 96);
			Assert.IsTrue(document.ParagraphRunIndexTreeHeight <= 96);
			Assert.IsTrue(document.AreTextBufferInvariantsValid());
			Assert.IsTrue(document.AreRunIndexesValid());
			Assert.IsTrue(document.AreRangeEditLogInvariantsValid());
			Assert.IsTrue(document.AreRenderProfilesValid());
			Assert.IsTrue(editor.AreRenderedFragmentsValid());

			var randomizedOrder = markers.ToArray();
			Shuffle(randomizedOrder, random);
			for (var i = 0; i < randomizedOrder.Length; i++)
			{
				var marker = randomizedOrder[i];
				var decoy = randomizedOrder[(i + 1) % randomizedOrder.Length];
				document.GetRange(decoy.Position, decoy.Position + decoy.Length).ScrollIntoView(PointOptions.Start);
				document.GetRange(marker.Position, marker.Position + marker.Length).ScrollIntoView(PointOptions.Start);

				var bounds = await WaitForRenderedColor(editor, marker.Color, tolerance: 20);
				var visible = marker.Kind switch
				{
					RandomRichVisualMarkerKind.Image => bounds is { Width: > 8, Height: > 6 },
					RandomRichVisualMarkerKind.ForegroundLink => bounds is { Width: > 4, Height: > 2 },
					_ => bounds is { Width: > 4, Height: > 4 },
				};
				Assert.IsTrue(
					visible,
					$"Seed {seed}: expected {marker.Kind} color {marker.Color} at {marker.Position}, bounds were {bounds}.");
			}

			Assert.IsTrue(document.AreTextBufferInvariantsValid());
			Assert.IsTrue(document.AreRunIndexesValid());
			Assert.IsTrue(document.AreRangeEditLogInvariantsValid());
			Assert.IsTrue(document.AreRenderProfilesValid());
			Assert.IsTrue(editor.AreRenderedFragmentsValid());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Local_Paragraph_Format_Changes_Retain_Distant_Paragraph_Inlines()
	{
		const int paragraphCount = 1024;
		const int paragraphLength = 8;
		var editor = new RichEditBox
		{
			Width = 420,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			editor.Document.SetText(TextSetOptions.FormatRtf, BuildParagraphRunRtf(paragraphCount, paragraphLength));
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var block = GetDisplayBlock(editor);
			var first = block.Inlines[0];
			var last = block.Inlines[^1];
			var createdBefore = editor.RenderFragmentCreationCount;
			var specifiedBefore = editor.RenderFragmentSpecificationCount;
			var paragraphStart = paragraphCount / 2 * paragraphLength;

			var format = editor.Document.GetRange(paragraphStart, paragraphStart + 1).ParagraphFormat;
			format.Alignment = ParagraphAlignment.Center;
			format.SetIndents(0, 24, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreSame(first, block.Inlines[0]);
			Assert.AreSame(last, block.Inlines[^1]);
			Assert.IsTrue(editor.RenderFragmentCreationCount - createdBefore <= 4);
			Assert.IsTrue(editor.RenderFragmentSpecificationCount - specifiedBefore <= 12);
			Assert.IsTrue(editor.AreRenderedFragmentsValid());
			Assert.IsTrue(editor.Document.AreRenderProfilesValid());
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Unit_Boundaries_Are_Reused_And_Minimally_Invalidated()
	{
		var editor = new RichEditBox();
		var document = editor.Document;
		document.SetText(
			TextSetOptions.None,
			string.Join("\r", Enumerable.Range(0, 512).Select(static i => $"Paragraph {i}. One two three!")));

		var word = document.GetUnitBoundaries(TextRangeUnit.Word);
		var sentence = document.GetUnitBoundaries(TextRangeUnit.Sentence);
		var paragraph = document.GetUnitBoundaries(TextRangeUnit.Paragraph);
		var characterFormat = document.GetUnitBoundaries(TextRangeUnit.CharacterFormat);
		var paragraphFormat = document.GetUnitBoundaries(TextRangeUnit.ParagraphFormat);
		var objects = document.GetUnitBoundaries(TextRangeUnit.Object);
		var bold = document.GetUnitBoundaries(TextRangeUnit.Bold);
		var wordBuilds = document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Word);
		var sentenceBuilds = document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Sentence);
		var paragraphBuilds = document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Paragraph);

		for (var i = 0; i < 1000; i++)
		{
			Assert.AreSame(word, document.GetUnitBoundaries(TextRangeUnit.Word));
			Assert.AreSame(sentence, document.GetUnitBoundaries(TextRangeUnit.Sentence));
			Assert.AreSame(paragraph, document.GetUnitBoundaries(TextRangeUnit.Paragraph));
		}

		document.GetRange(10, 20).CharacterFormat.Bold = FormatEffect.On;
		Assert.AreSame(word, document.GetUnitBoundaries(TextRangeUnit.Word));
		Assert.AreSame(sentence, document.GetUnitBoundaries(TextRangeUnit.Sentence));
		Assert.AreSame(paragraph, document.GetUnitBoundaries(TextRangeUnit.Paragraph));
		Assert.AreNotSame(characterFormat, document.GetUnitBoundaries(TextRangeUnit.CharacterFormat));
		Assert.AreNotSame(objects, document.GetUnitBoundaries(TextRangeUnit.Object));
		Assert.AreNotSame(bold, document.GetUnitBoundaries(TextRangeUnit.Bold));
		Assert.AreSame(paragraphFormat, document.GetUnitBoundaries(TextRangeUnit.ParagraphFormat));
		Assert.AreEqual(wordBuilds, document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Word));
		Assert.AreEqual(sentenceBuilds, document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Sentence));
		Assert.AreEqual(paragraphBuilds, document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Paragraph));

		var range = document.GetRange(0, 0);
		for (var i = 0; i < 2000; i++)
		{
			range.Move(TextRangeUnit.Word, 1);
			if (range.StartPosition == document.TextLength)
			{
				range.SetRange(0, 0);
			}
		}
		Assert.AreEqual(wordBuilds, document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Word));

		document.GetRange(0, 1).Text = "X";
		Assert.AreNotSame(word, document.GetUnitBoundaries(TextRangeUnit.Word));
		Assert.AreEqual(wordBuilds + 1, document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Word));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Line_Navigation_Reuses_Layout_Index_Until_Layout_Changes()
	{
		var editor = new RichEditBox
		{
			Width = 240,
			TextWrapping = TextWrapping.Wrap,
		};
		try
		{
			editor.Document.SetText(
				TextSetOptions.None,
				string.Join(" ", Enumerable.Range(0, 800).Select(static i => $"word{i}")));
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			await WindowHelper.WaitForIdle();

			var first = editor.Document.GetUnitBoundaries(TextRangeUnit.Line);
			var lineBuilds = editor.VisualLineIndexRebuildCount;
			var boundaryBuilds = editor.Document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Line);
			for (var i = 0; i < 2000; i++)
			{
				Assert.AreSame(first, editor.Document.GetUnitBoundaries(TextRangeUnit.Line));
				Assert.IsTrue(editor.TryGetLineBounds(i % editor.Document.TextLength, out _, out _, out _, out _));
			}
			Assert.AreEqual(lineBuilds, editor.VisualLineIndexRebuildCount);
			Assert.AreEqual(boundaryBuilds, editor.Document.GetUnitBoundaryCacheRebuildCount(TextRangeUnit.Line));

			editor.Document.GetRange(20, 40).CharacterFormat.Bold = FormatEffect.On;
			await WindowHelper.WaitForIdle();
			var afterFormat = editor.Document.GetUnitBoundaries(TextRangeUnit.Line);
			Assert.AreNotSame(first, afterFormat);
			Assert.AreEqual(lineBuilds + 1, editor.VisualLineIndexRebuildCount);

			editor.Width = 160;
			await WindowHelper.WaitForIdle();
			var afterResize = editor.Document.GetUnitBoundaries(TextRangeUnit.Line);
			Assert.AreNotSame(afterFormat, afterResize);
			Assert.AreEqual(lineBuilds + 2, editor.VisualLineIndexRebuildCount);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Distinct_Paragraph_Tab_Metadata_Remains_Isolated_After_Local_Update()
	{
		var editor = new RichEditBox
		{
			Width = 360,
			TextWrapping = TextWrapping.NoWrap,
		};
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "R\t123\rD\t12.5");
			editor.Document.GetRange(0, 0).ParagraphFormat.AddTab(90, TabAlignment.Right, TabLeader.Dashes);
			editor.Document.GetRange(6, 6).ParagraphFormat.AddTab(90, TabAlignment.Decimal, TabLeader.Equals);
			await WindowHelper.WaitForIdle();

			var paragraphRuns = editor.Document.ParagraphRuns;
			Assert.AreEqual(TabAlignment.Right, paragraphRuns[0].Format.Tabs[0].Alignment);
			Assert.AreEqual(TabAlignment.Decimal, paragraphRuns[^1].Format.Tabs[0].Alignment);
			var runs = GetDisplayBlock(editor).Inlines.OfType<Run>().ToArray();
			Assert.AreEqual(TabAlignment.Right, runs[0].ParagraphLayout!.Tabs[0].Alignment);
			Assert.AreEqual(TabAlignment.Decimal, runs[^1].ParagraphLayout!.Tabs[0].Alignment);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Many_Tracked_Ranges_Rebase_Lazily_And_Compact_Bounded_Log()
	{
		const int liveRangeCount = 2048;
		const int deadRangeCount = 4096;
		const int editCount = 600;
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, new string('a', 8192));
		var liveRanges = new ITextRange[liveRangeCount];
		for (var i = 0; i < liveRanges.Length; i++)
		{
			var position = 512 + i;
			liveRanges[i] = document.GetRange(position, position + 1);
		}
		var finalEop = document.GetRange(document.TextLength, document.StoryLength);
		CreateDeadRanges(document, deadRangeCount);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		document.ResetRangeTrackingCountersForTesting();
		var retainedBefore = document.RetainedRangeEditCount;
		var pendingBefore = document.PendingRangeEditCount;

		var insertionRange = document.GetRange(0, 0);
		for (var i = 0; i < editCount; i++)
		{
			insertionRange.SetRange(0, 0);
			insertionRange.Text = "x";
		}

		Assert.IsGreaterThanOrEqualTo(editCount, document.RetainedRangeEditCount);
		Assert.IsLessThanOrEqualTo(retainedBefore + editCount, document.RetainedRangeEditCount);
		Assert.AreEqual((pendingBefore + editCount) % 128, document.PendingRangeEditCount);
		Assert.IsTrue(document.RangeEditLogSegmentCount <= 4);
		Assert.AreEqual((pendingBefore + editCount) / 128, document.RangeEditLogCompactionCount);
		Assert.IsTrue(
			document.RangeRebaseApplicationCount <= editCount * 2,
			$"Eagerly applied {document.RangeRebaseApplicationCount} range deltas.");
		for (var i = 0; i < 16; i++)
		{
			var rangeIndex = i * 127 % liveRanges.Length;
			Assert.AreEqual(512 + rangeIndex + editCount, liveRanges[rangeIndex].StartPosition);
		}
		Assert.IsTrue(document.RangeRebaseApplicationCount < liveRangeCount * editCount / 4);
		Assert.AreEqual(document.TextLength, finalEop.StartPosition);
		Assert.AreEqual(document.StoryLength, finalEop.EndPosition);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		document.CompactTrackedRangesForTesting();
		Assert.AreEqual(0, document.PendingRangeEditCount);
		Assert.AreEqual((pendingBefore + editCount) / 128 + 1, document.RangeEditLogCompactionCount);
		Assert.IsGreaterThanOrEqualTo(editCount, document.RetainedRangeEditCount);
		Assert.IsLessThanOrEqualTo(retainedBefore + editCount, document.RetainedRangeEditCount);
		Assert.IsTrue(document.RangeEditLogSegmentCount <= 4);
		Assert.IsGreaterThan(0, document.DeadRangeCleanupCount);
		Assert.IsTrue(document.TrackedRangeReferenceCount <= liveRangeCount + 4);
		Assert.IsTrue(document.AreRangeEditLogInvariantsValid());
		for (var i = 0; i < liveRanges.Length; i += 137)
		{
			Assert.AreEqual(512 + i + editCount, liveRanges[i].StartPosition);
		}

		var undoRange = document.GetRange(1000, 1001);
		document.GetRange(0, 0).Text = "y";
		document.Undo();
		document.Redo();
		Assert.AreEqual(1001, undoRange.StartPosition);
		Assert.AreEqual("a", undoRange.Text);
		Assert.IsTrue(document.AreRangeEditLogInvariantsValid());
	}

	[TestMethod]
	public void When_Million_Character_Story_Uses_Bounded_Pieces_For_Thousands_Of_Local_Edits()
	{
		const int initialLength = 1_000_000;
		const int editCount = 3000;
		const int undoCount = 128;
		const int imagePosition = initialLength / 4;
		var initial = new StringBuilder(new string('a', initialLength), initialLength + 16_384);
		for (var i = 4095; i < initial.Length; i += 4096)
		{
			initial[i] = '\r';
		}
		initial[12_345] = '\ud83d';
		initial[12_346] = '\ude00';
		initial[12_347] = 'Q';
		initial[12_348] = '\u0301';
		const string searchNeedle = "StoryNeedle";
		for (var i = 0; i < searchNeedle.Length; i++)
		{
			initial[12_360 + i] = searchNeedle[i];
		}

		var editor = new RichEditBox();
		var document = editor.Document;
		document.SetText(TextSetOptions.None, initial.ToString());
		using var imageStream = CreateImageStream(SKColors.Blue);
		document.GetRange(imagePosition, imagePosition).InsertImage(
			2,
			2,
			1,
			VerticalCharacterAlignment.Baseline,
			"story-image",
			imageStream);
		initial.Insert(imagePosition, '\ufffc');
		var imageRange = document.GetRange(imagePosition, imagePosition + 1);
		var emojiRange = document.GetRange(12_345, 12_347);
		var combiningRange = document.GetRange(12_347, 12_349);
		var needleRange = document.GetRange(12_360, 12_360 + searchNeedle.Length);
		var finalEop = document.GetRange(document.TextLength, document.StoryLength);
		var editRange = document.GetRange(0, 0);
		var edits = new List<StoryEdit>(editCount);
		var random = new Random(0x51a7b1e);
		var inserts = new[] { "x", "YZ", "\r", "\ud83d\ude03", "e\u0301", "\ufffc", string.Empty };

		document.ClearUndoRedoHistory();
		document.ResetTextBufferDiagnosticsForTesting();
		document.BatchDisplayUpdates();
		try
		{
			for (var i = 0; i < editCount; i++)
			{
				var anchor = i % 3 switch
				{
					0 => 128,
					1 => initial.Length / 2,
					_ => initial.Length - 128,
				};
				var start = Math.Clamp(anchor + random.Next(-64, 65), 0, initial.Length);
				var removeLength = Math.Min(random.Next(0, 5), initial.Length - start);
				var insert = inserts[random.Next(inserts.Length)];
				var removed = initial.ToString(start, removeLength);
				if (removeLength == 0 && insert.Length == 0
					|| removeLength == insert.Length && string.Equals(removed, insert, StringComparison.Ordinal))
				{
					insert = "q";
				}

				editRange.SetRange(start, start + removeLength);
				editRange.Text = insert;
				initial.Remove(start, removeLength);
				initial.Insert(start, insert);
				edits.Add(new StoryEdit(start, removed, insert));

				if ((i & 63) == 0)
				{
					Assert.IsTrue(document.AreTextBufferInvariantsValid(), $"Piece invariant failed after edit {i}.");
					Assert.IsTrue(document.AreRunIndexesValid(), $"Run invariant failed after edit {i}.");
					Assert.IsTrue(document.AreRangeEditLogInvariantsValid(), $"Range invariant failed after edit {i}.");
					var probe = Math.Clamp(start - 8, 0, initial.Length);
					var probeLength = Math.Min(32, initial.Length - probe);
					Assert.AreEqual(
						initial.ToString(probe, probeLength),
						document.GetTextInRange(probe, probe + probeLength));
				}
			}

			Assert.AreEqual(0, document.TextBufferFullMaterializationCount);
			Assert.IsGreaterThan(0, document.TextBufferCompactionCount);
			Assert.IsTrue(
				document.TextBufferPieceCount <= 512,
				$"The story retained {document.TextBufferPieceCount} pieces after {editCount} local edits.");
			Assert.IsTrue(
				document.TextBufferTreeHeight <= 96,
				$"The story tree height was {document.TextBufferTreeHeight} for {document.TextBufferPieceCount} pieces.");
			Assert.IsTrue(document.TextBufferCompactedCharacterCount <= document.TextBufferCompactionCount * 32L * 1024);

			for (var i = edits.Count - 1; i >= edits.Count - undoCount; i--)
			{
				var edit = edits[i];
				document.Undo();
				initial.Remove(edit.Start, edit.Inserted.Length);
				initial.Insert(edit.Start, edit.Removed);
			}
			for (var i = edits.Count - undoCount; i < edits.Count; i++)
			{
				var edit = edits[i];
				document.Redo();
				initial.Remove(edit.Start, edit.Removed.Length);
				initial.Insert(edit.Start, edit.Inserted);
			}

			Assert.AreEqual(0, document.TextBufferFullMaterializationCount);
			Assert.AreEqual(document.TextLength, finalEop.StartPosition);
			Assert.AreEqual(document.StoryLength, finalEop.EndPosition);
			Assert.AreEqual("\ud83d\ude00", emojiRange.Text);
			Assert.AreEqual("Q\u0301", combiningRange.Text);
			imageRange.GetText(TextGetOptions.UseObjectText, out var imageText);
			Assert.AreEqual("story-image", imageText);
			Assert.AreEqual(1, document.GetAutomationTextObjects().Count(static item => item.Kind == RichEditTextObjectKind.Image));
			Assert.IsGreaterThan(200, document.GetUnitBoundaries(TextRangeUnit.Paragraph)!.Count);
			Assert.IsTrue(document.AreTextBufferInvariantsValid());
			Assert.IsTrue(document.AreRunIndexesValid());
			Assert.IsTrue(document.AreRangeEditLogInvariantsValid());
			var search = document.GetRange(0, document.TextLength);
			Assert.AreEqual(searchNeedle.Length, search.FindText("storyneedle", document.TextLength, FindOptions.None));
			Assert.AreEqual(needleRange.StartPosition, search.StartPosition);
			Assert.AreEqual(0, document.TextBufferFullMaterializationCount);

			var emojiCluster = document.GetRange(emojiRange.StartPosition + 1, emojiRange.StartPosition + 1);
			emojiCluster.Expand(TextRangeUnit.Cluster);
			Assert.AreEqual("\ud83d\ude00", emojiCluster.Text);
			var combiningCluster = document.GetRange(combiningRange.StartPosition + 1, combiningRange.StartPosition + 1);
			combiningCluster.Expand(TextRangeUnit.Cluster);
			Assert.AreEqual("Q\u0301", combiningCluster.Text);
			Assert.AreEqual(1, document.TextBufferFullMaterializationCount);

			var actual = document.GetTextInRange(0, document.TextLength);
			Assert.AreEqual(1, document.TextBufferFullMaterializationCount);
			Assert.AreEqual(initial.ToString(), actual);
		}
		finally
		{
			document.ApplyDisplayUpdates();
		}
	}

	private static async Task<Windows.Foundation.Rect> WaitForRenderedColor(
		RichEditBox editor,
		global::Windows.UI.Color color,
		byte tolerance)
	{
		var bounds = default(Windows.Foundation.Rect);
		for (var attempt = 0; attempt < 5; attempt++)
		{
			await WindowHelper.WaitForIdle();
			await UITestHelper.WaitForRender(timeoutMS: 5000);
			await WindowHelper.WaitForIdle();
			var screenshot = await UITestHelper.ScreenShot(editor);
			bounds = ImageAssert.GetColorBounds(screenshot, color, tolerance);
			if (bounds is { Width: > 1, Height: > 2 })
			{
				break;
			}
		}

		return bounds;
	}

	private static void Shuffle<T>(T[] items, Random random)
	{
		for (var i = items.Length - 1; i > 0; i--)
		{
			var other = random.Next(i + 1);
			(items[i], items[other]) = (items[other], items[i]);
		}
	}

	private static string BuildAlternatingRunRtf(int runCount, int runLength)
	{
		var rtf = new StringBuilder(@"{\rtf1\ansi ");
		for (var i = 0; i < runCount; i++)
		{
			rtf.Append(i % 2 == 0 ? @"\b " : @"\b0 ");
			rtf.Append((char)('a' + i % 26), runLength);
		}
		rtf.Append('}');
		return rtf.ToString();
	}

	private static string BuildParagraphRunRtf(int paragraphCount, int paragraphLength)
	{
		var rtf = new StringBuilder(@"{\rtf1\ansi ");
		for (var i = 0; i < paragraphCount; i++)
		{
			rtf.Append(i % 2 == 0 ? @"\ql\li180 " : @"\qr\li360 ");
			rtf.Append((char)('a' + i % 26), paragraphLength - 1);
			rtf.Append(@"\par ");
		}

		rtf.Append('}');
		return rtf.ToString();
	}

	private readonly record struct StoryEdit(int Start, string Removed, string Inserted);

	private readonly record struct RandomRichVisualMarker(
		int Position,
		int Length,
		global::Windows.UI.Color Color,
		RandomRichVisualMarkerKind Kind,
		string? Metadata);

	private readonly record struct RandomRichTextEdit(int Position, string Before, string After);

	private enum RandomRichVisualMarkerKind
	{
		Image,
		ForegroundLink,
		Background,
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void CreateDeadRanges(RichEditTextDocument document, int count)
	{
		for (var i = 0; i < count; i++)
		{
			_ = document.GetRange(i % document.TextLength, i % document.TextLength);
		}
	}
}
