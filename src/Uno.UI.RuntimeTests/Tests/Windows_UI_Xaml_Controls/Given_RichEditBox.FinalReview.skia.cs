#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	public void When_Rtf_Hyperlink_Export_Uses_The_Safe_Scheme_Allowlist()
	{
		var targets = new[]
		{
			("file", "file:///C:/Windows/System32/calc.exe", false),
			("custom", "contoso-shell:open", false),
			("script", "javascript:alert(1)", false),
			("http", "http://example.com/path", true),
			("https", "https://example.com/path", true),
			("mail", "mailto:user@example.com", true),
		};
		var rtf = @"{\rtf1 "
			+ string.Join(" ", targets.Select(target =>
				$@"{{\field{{\*\fldinst HYPERLINK ""{target.Item2}""}}{{\fldrslt {target.Item1}}}}}"))
			+ "}";
		var document = new RichEditBox().Document;

		document.SetText(TextSetOptions.FormatRtf, rtf);
		GetTextWithoutFinalEop(document, out var text);
		foreach (var (label, target, allowed) in targets)
		{
			var start = text.IndexOf(label, StringComparison.Ordinal);
			Assert.IsGreaterThanOrEqualTo(0, start);
			Assert.AreEqual($"\"{target}\"", document.GetRange(start, start + label.Length).Link);
			Assert.AreEqual(allowed, RichEditBox.TryGetLinkUri($"\"{target}\"", out _), target);
		}

		document.GetText(TextGetOptions.FormatRtf, out var roundTrippedRtf);
		var roundTripped = new RichEditBox().Document;
		roundTripped.SetText(TextSetOptions.FormatRtf, roundTrippedRtf);
		GetTextWithoutFinalEop(roundTripped, out var roundTrippedText);
		foreach (var (label, target, allowed) in targets)
		{
			var start = roundTrippedText.IndexOf(label, StringComparison.Ordinal);
			Assert.AreEqual(
				allowed ? $"\"{target}\"" : string.Empty,
				roundTripped.GetRange(start, start + label.Length).Link);
		}
	}

	[TestMethod]
	public async Task When_Pointer_Link_Activation_Uses_The_Safe_Scheme_Allowlist()
	{
		var launched = new List<Uri>();
		var editor = new RichEditBox
		{
			LinkLauncherForTesting = uri =>
			{
				launched.Add(uri);
				return Task.FromResult(true);
			},
		};
		editor.Document.SetText(TextSetOptions.None, "link");

		foreach (var blocked in new[] { "file:///C:/secret.txt", "contoso-shell:open", "javascript:alert(1)" })
		{
			editor.Document.GetRange(0, 4).Link = $"\"{blocked}\"";
			Assert.IsFalse(editor.TryNavigateLinkAt(1), blocked);
			Assert.HasCount(0, launched, blocked);
		}

		foreach (var allowed in new[] { "http://example.com", "https://example.com", "mailto:user@example.com" })
		{
			editor.Document.GetRange(0, 4).Link = $"\"{allowed}\"";
			Assert.IsTrue(editor.TryNavigateLinkAt(1), allowed);
			await WindowHelper.WaitFor(() => launched.Count > 0 && launched[^1].OriginalString == allowed);
		}

		CollectionAssert.AreEqual(
			new[] { "http://example.com/", "https://example.com/", "mailto:user@example.com" },
			launched.Select(uri => uri.ToString()).ToArray());
	}

	[TestMethod]
	public async Task When_UIA_Invoke_Uses_The_Safe_Link_Scheme_Allowlist()
	{
		var launched = new List<Uri>();
		var richEditBox = new RichEditBox
		{
			Width = 320,
			LinkLauncherForTesting = uri =>
			{
				launched.Add(uri);
				return Task.FromResult(true);
			},
		};
		try
		{
			WindowHelper.WindowContent = richEditBox;
			await WindowHelper.WaitForLoaded(richEditBox);
			richEditBox.Document.SetText(TextSetOptions.None, "link");

			foreach (var blocked in new[] { "file:///C:/secret.txt", "contoso-shell:open", "javascript:alert(1)" })
			{
				InvokeCurrentLink($"\"{blocked}\"");
				Assert.HasCount(0, launched, blocked);
			}

			foreach (var allowed in new[] { "http://example.com", "https://example.com", "mailto:user@example.com" })
			{
				InvokeCurrentLink($"\"{allowed}\"");
				await WindowHelper.WaitFor(() => launched.Count > 0 && launched[^1].OriginalString == allowed);
			}

			CollectionAssert.AreEqual(
				new[] { "http://example.com/", "https://example.com/", "mailto:user@example.com" },
				launched.Select(uri => uri.ToString()).ToArray());

			void InvokeCurrentLink(string link)
			{
				richEditBox.Document.GetRange(0, 4).Link = link;
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox);
				var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
				var provider = textProvider?.DocumentRange.GetChildren().Single();
				var invoke = provider?.AutomationPeer?.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
				Assert.IsNotNull(invoke);
				invoke.Invoke();
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Compressed_Large_Image_History_Drops_Decoded_Caches_And_Charges_Pixels()
	{
		const int dimension = 512;
		var document = new RichEditBox().Document;
		using var surface = SKSurface.Create(new SKImageInfo(dimension, dimension));
		surface.Canvas.Clear(SKColors.CornflowerBlue);
		using var sourceImage = surface.Snapshot();
		using var encoded = sourceImage.Encode(SKEncodedImageFormat.Png, 100);
		using var stream = new MemoryStream(encoded.ToArray()).AsRandomAccessStream();
		document.GetRange(0, 0).InsertImage(
			dimension,
			dimension,
			dimension,
			VerticalCharacterAlignment.Baseline,
			"large compressed image",
			stream);
		document.ClearUndoRedoHistory();

		for (var iteration = 0; iteration < 20; iteration++)
		{
			var image = document.GetAutomationTextObjects().Single().Image;
			Assert.IsNotNull(image);
			Assert.IsNotNull(image.GetDecodedImage());
			document.GetRange(0, 1).CharacterFormat.Bold =
				iteration % 2 == 0 ? FormatEffect.On : FormatEffect.Off;
		}

		var diagnostics = document.UndoImageDiagnostics;
		Assert.AreEqual(0, diagnostics.DecodedCaches);
		Assert.IsGreaterThanOrEqualTo(2, diagnostics.References);
		Assert.IsGreaterThan(0, diagnostics.EncodedBytes);
		Assert.IsGreaterThan(diagnostics.EncodedBytes * 10, diagnostics.DecodedBytes);
		Assert.IsGreaterThanOrEqualTo(
			2L * dimension * dimension * 4,
			diagnostics.DecodedBytes);
		Assert.IsTrue(document.UndoHistoryCost <= 4 * 1024 * 1024, $"History cost was {document.UndoHistoryCost} bytes.");
		Assert.IsTrue(document.UndoEntryCount is >= 1 and <= 2, $"History retained {document.UndoEntryCount} entries.");

		document.Undo();
		Assert.AreEqual(FormatEffect.On, document.GetRange(0, 1).CharacterFormat.Bold);
		document.Redo();
		Assert.AreEqual(FormatEffect.Off, document.GetRange(0, 1).CharacterFormat.Bold);
		Assert.IsNotNull(document.GetAutomationTextObjects().Single().Image?.GetDecodedImage());
	}

	[TestMethod]
	public void When_MathML_Policy_Rejections_Are_Atomic()
	{
		const string valid = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mi>x</mi></math>";
		var invalidValues = new[]
		{
			"<math><mi>x</mi></math>",
			"<!DOCTYPE math [<!ENTITY value SYSTEM \"file:///etc/passwd\">]>"
				+ "<math xmlns=\"http://www.w3.org/1998/Math/MathML\"><mtext>&value;</mtext></math>",
			"<math xmlns=\"http://www.w3.org/1998/Math/MathML\">"
				+ string.Concat(Enumerable.Repeat("<mi>x</mi>", MathDocument.MaxNodeCount + 1))
				+ "</math>",
			new string('x', MathDocument.MaxInputLength + 1),
		};
		var document = new RichEditBox().Document;
		document.SetMathMode(RichEditMathMode.MathOnly);
		document.SetMathML(valid);
		document.GetMathML(out var canonical);
		document.Selection.SetRange(0, 1);
		document.ClearUndoRedoHistory();

		foreach (var invalid in invalidValues)
		{
			Assert.ThrowsExactly<ArgumentException>(() => document.SetMathML(invalid));
			document.GetMathML(out var afterFailure);
			Assert.AreEqual(canonical, afterFailure);
			Assert.AreEqual(0, document.Selection.StartPosition);
			Assert.AreEqual(1, document.Selection.EndPosition);
			Assert.IsFalse(document.CanUndo());
			Assert.IsFalse(document.CanRedo());
		}
	}

	[TestMethod]
	public void When_MathML_Css_Alpha_Colors_RoundTrip_As_RRGGBBAA()
	{
		const string source = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\">"
			+ "<mstyle mathcolor=\"#11223344\" mathbackground=\"#A0B0C080\"><mi>x</mi></mstyle>"
			+ "</math>";
		var document = new RichEditBox().Document;
		document.SetMathMode(RichEditMathMode.MathOnly);

		document.SetMathML(source);

		var style = document.MathAtoms.Single().Atom.Style;
		Assert.IsTrue(style.Foreground.HasValue);
		Assert.IsTrue(style.Background.HasValue);
		var foreground = style.Foreground.Value;
		var background = style.Background.Value;
		Assert.AreEqual((byte)0x44, foreground.A);
		Assert.AreEqual((byte)0x11, foreground.R);
		Assert.AreEqual((byte)0x22, foreground.G);
		Assert.AreEqual((byte)0x33, foreground.B);
		Assert.AreEqual((byte)0x80, background.A);
		Assert.AreEqual((byte)0xA0, background.R);
		Assert.AreEqual((byte)0xB0, background.G);
		Assert.AreEqual((byte)0xC0, background.B);

		document.GetMathML(out var canonical);
		var element = XDocument.Parse(canonical).Descendants().Single(node => node.Name.LocalName == "mi");
		Assert.AreEqual("#11223344", element.Attribute("mathcolor")?.Value);
		Assert.AreEqual("#A0B0C080", element.Attribute("mathbackground")?.Value);

		document.SetMathML(canonical);
		document.GetMathML(out var second);
		Assert.AreEqual(canonical, second);
	}

	[TestMethod]
	public void When_Large_Plain_Text_MathML_Export_Is_Lossless()
	{
		var text = new string('x', MathDocument.MaxProjectionLength + 32_768) + "<&\U0001D7D8";
		var document = new RichEditBox().Document;
		document.SetMathMode(RichEditMathMode.MathOnly);
		document.SetText(TextSetOptions.None, text);

		document.GetMathML(out var mathML);

		var parsed = XDocument.Parse(mathML);
		Assert.AreEqual(text, parsed.Root?.Value);
		Assert.AreEqual("mtext", parsed.Root?.Elements().Single().Name.LocalName);
	}

	[TestMethod]
	public void When_Length_Expanding_ChangeCase_Updates_Selection_And_History()
	{
		var document = new RichEditBox().Document;
		document.SetText(TextSetOptions.None, "a\uFB03b");
		document.Selection.SetRange(2, 1);
		document.ClearUndoRedoHistory();
		document.ChangeCaseMapperForTesting = static (text, letterCase) =>
			text == "\uFB03" && letterCase == LetterCase.Upper ? "FFI" : text;
		try
		{
			document.Selection.ChangeCase(LetterCase.Upper);

			GetTextWithoutFinalEop(document, out var changed);
			Assert.AreEqual("aFFIb", changed);
			Assert.AreEqual(1, document.Selection.StartPosition);
			Assert.AreEqual(4, document.Selection.EndPosition);
			Assert.IsTrue(document.Selection.Options.HasFlag(SelectionOptions.StartActive));
			Assert.IsTrue(document.CanUndo());

			document.Undo();
			GetTextWithoutFinalEop(document, out var undone);
			Assert.AreEqual("a\uFB03b", undone);
			Assert.AreEqual(1, document.Selection.StartPosition);
			Assert.AreEqual(2, document.Selection.EndPosition);
			Assert.IsTrue(document.Selection.Options.HasFlag(SelectionOptions.StartActive));

			document.Redo();
			GetTextWithoutFinalEop(document, out var redone);
			Assert.AreEqual("aFFIb", redone);
			Assert.AreEqual(1, document.Selection.StartPosition);
			Assert.AreEqual(4, document.Selection.EndPosition);
			Assert.IsTrue(document.Selection.Options.HasFlag(SelectionOptions.StartActive));
		}
		finally
		{
			document.ChangeCaseMapperForTesting = null;
		}
	}

	[TestMethod]
	public async Task When_Duplicate_Link_Removal_Preserves_The_Surviving_UIA_Peer()
	{
		var richEditBox = new RichEditBox { Width = 320 };
		try
		{
			WindowHelper.WindowContent = richEditBox;
			await WindowHelper.WaitForLoaded(richEditBox);
			richEditBox.Document.SetText(TextSetOptions.None, "first second");
			richEditBox.Document.GetRange(0, 5).Link = "\"https://example.com\"";
			richEditBox.Document.GetRange(6, 12).Link = "\"https://example.com\"";

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(richEditBox);
			var textProvider = peer?.GetPattern(PatternInterface.Text) as ITextProvider;
			Assert.IsNotNull(textProvider);
			var initial = textProvider.DocumentRange.GetChildren();
			Assert.HasCount(2, initial);
			var removed = initial.Single(provider => textProvider.RangeFromChild(provider).GetText(-1) == "first");
			var surviving = initial.Single(provider => textProvider.RangeFromChild(provider).GetText(-1) == "second");

			richEditBox.Document.GetRange(0, 6).Text = string.Empty;
			await WindowHelper.WaitForIdle();

			var current = textProvider.DocumentRange.GetChildren().Single();
			Assert.AreSame(surviving.AutomationPeer, current.AutomationPeer);
			Assert.AreNotSame(removed.AutomationPeer, current.AutomationPeer);
			Assert.IsNull(textProvider.RangeFromChild(removed));
			Assert.AreEqual("second", textProvider.RangeFromChild(surviving).GetText(-1));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_RichEditBox_Unloads_The_IME_Host_Is_Released_And_Callbacks_Reroute()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = RichEditBox.SetImeExtensionForTesting(fake);
		var oldStarted = 0;
		var weak = await CreateAndUnloadRichEditBox(fake, () => oldStarted++);
		Assert.IsNull(ImeSessionCoordinator.ActiveHost);

		fake.SimulateCompositionStart();
		Assert.AreEqual(0, oldStarted);

		var replacement = new RichEditBox();
		var replacementStarted = 0;
		replacement.TextCompositionStarted += (_, _) => replacementStarted++;
		try
		{
			WindowHelper.WindowContent = replacement;
			await WindowHelper.WaitForLoaded(replacement);
			Assert.IsTrue(replacement.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			fake.SimulateCompositionStart();
			Assert.AreEqual(1, replacementStarted);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}

		ForceFullCollection();
		Assert.IsFalse(weak.IsAlive, "The unloaded RichEditBox was retained by the IME coordinator.");
	}

	[TestMethod]
	public async Task When_TextBox_Unloads_The_IME_Host_Is_Released_And_Callbacks_Reroute()
	{
		var fake = new FakeImeTextBoxExtension();
		using var imeDisposable = TextBox.SetImeExtensionForTesting(fake);
		var oldStarted = 0;
		var weak = await CreateAndUnloadTextBox(() => oldStarted++);
		Assert.IsNull(ImeSessionCoordinator.ActiveHost);

		fake.SimulateCompositionStart();
		Assert.AreEqual(0, oldStarted);

		var replacement = new TextBox();
		var replacementStarted = 0;
		replacement.TextCompositionStarted += (_, _) => replacementStarted++;
		try
		{
			WindowHelper.WindowContent = replacement;
			await WindowHelper.WaitForLoaded(replacement);
			Assert.IsTrue(replacement.Focus(FocusState.Programmatic));
			await WindowHelper.WaitForIdle();
			fake.SimulateCompositionStart();
			Assert.AreEqual(1, replacementStarted);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
		}

		ForceFullCollection();
		Assert.IsFalse(weak.IsAlive, "The unloaded TextBox was retained by the IME coordinator.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> CreateAndUnloadRichEditBox(
		FakeImeTextBoxExtension fake,
		Action onStarted)
	{
		var editor = new RichEditBox();
		editor.TextCompositionStarted += (_, _) => onStarted();
		WindowHelper.WindowContent = editor;
		await WindowHelper.WaitForLoaded(editor);
		Assert.IsTrue(editor.Focus(FocusState.Programmatic));
		await WindowHelper.WaitForIdle();
		Assert.AreSame(editor, ImeSessionCoordinator.ActiveHost);
		var weak = new WeakReference(editor);
		WindowHelper.WindowContent = new Button();
		await WindowHelper.WaitForIdle();
		return weak;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> CreateAndUnloadTextBox(Action onStarted)
	{
		var editor = new TextBox();
		editor.TextCompositionStarted += (_, _) => onStarted();
		WindowHelper.WindowContent = editor;
		await WindowHelper.WaitForLoaded(editor);
		Assert.IsTrue(editor.Focus(FocusState.Programmatic));
		await WindowHelper.WaitForIdle();
		Assert.AreSame(editor, ImeSessionCoordinator.ActiveHost);
		var weak = new WeakReference(editor);
		WindowHelper.WindowContent = new Button();
		await WindowHelper.WaitForIdle();
		return weak;
	}

	private static void ForceFullCollection()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}
}
