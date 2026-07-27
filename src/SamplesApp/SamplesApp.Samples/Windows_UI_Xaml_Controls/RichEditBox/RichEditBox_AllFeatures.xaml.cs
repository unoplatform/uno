#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
#if __SKIA__ || !HAS_UNO
	[Sample(
		"RichEditBox",
		Name = "RichEditBox_AllFeatures",
		Description = "Manual RichEditBox playground for rich formatting, paragraphs, links, images, RTF, MathML, clipboard, undo, events, input options, and large-document stress.",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
#endif
	public sealed partial class RichEditBox_AllFeatures : Page
	{
		private const string MathMl =
			"<math xmlns=\"http://www.w3.org/1998/Math/MathML\">" +
			"<mrow><mfrac><mrow><mi>x</mi><mo>+</mo><mn>1</mn></mrow><msqrt><mi>y</mi></msqrt></mfrac>" +
			"<mo>=</mo><msup><mi>z</mi><mn>2</mn></msup></mrow></math>";

		private static readonly byte[] _imageBytes = Convert.FromBase64String(
			"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4AWJiZmT6DwAAAP//EKnFGgAAAAZJREFUAwABIQEIIJGZrwAAAABJRU5ErkJggg==");

		private readonly List<string> _events = new();
		private bool _suppressEvents;

		public RichEditBox_AllFeatures()
		{
			this.InitializeComponent();
			SeedDocument();
			InitializeMath();
			Editor.Document.ClearUndoRedoHistory();
			MathEditor.Document.ClearUndoRedoHistory();
			HookEvents();
			ExportRtf();
			ClearEventsAfterPendingNotifications();
			UpdateStatus("Ready. Select text and use the commands.");
		}

		private void HookEvents()
		{
			Editor.TextChanging += (_, args) => AppendEvent($"TextChanging content={args.IsContentChanging}");
			Editor.TextChanged += (_, _) =>
			{
				AppendEvent("TextChanged");
			};
			Editor.SelectionChanged += (_, _) =>
			{
				AppendEvent("SelectionChanged");
			};
			Editor.CopyingToClipboard += (_, _) => AppendEvent("CopyingToClipboard");
			Editor.CuttingToClipboard += (_, _) => AppendEvent("CuttingToClipboard");
			Editor.Paste += (_, _) => AppendEvent("Paste");
			Editor.TextCompositionStarted += (_, _) => AppendEvent("CompositionStarted");
			Editor.TextCompositionChanged += (_, _) => AppendEvent("CompositionChanged");
			Editor.TextCompositionEnded += (_, _) => AppendEvent("CompositionEnded");
		}

		private void SeedDocument()
		{
			var wasSuppressingEvents = _suppressEvents;
			_suppressEvents = true;
			try
			{
				SeedDocumentCore();
			}
			finally
			{
				_suppressEvents = wasSuppressingEvents;
			}
		}

		private void SeedDocumentCore()
		{
			const string text =
				"RichEditBox all-features playground\r" +
				"Select text and apply formatting, colors, highlighting, alignment, lists, links, images, clipboard commands, and undo.\r" +
				"Uno Platform link | inline image: \r" +
				"First list item\r" +
				"Second list item\r" +
				"Formula text: H2O and x2. Find this formatting word.\r" +
				"Arabic and RTL probe: مرحبا بالعالم";

			Editor.Document.SetText(TextSetOptions.None, text);

			var titleEnd = text.IndexOf('\r');
			var title = Editor.Document.GetRange(0, titleEnd);
			title.CharacterFormat.Bold = FormatEffect.On;
			title.CharacterFormat.Size = 22;
			title.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.DeepSkyBlue;

			var instructionStart = text.IndexOf("Select text", StringComparison.Ordinal);
			var instructionEnd = text.IndexOf('\r', instructionStart);
			Editor.Document.GetRange(instructionStart, instructionEnd).CharacterFormat.Italic = FormatEffect.On;

			var linkStart = text.IndexOf("Uno Platform link", StringComparison.Ordinal);
			var linkRange = Editor.Document.GetRange(linkStart, linkStart + "Uno Platform link".Length);
			linkRange.Link = "\"https://platform.uno\"";
			linkRange.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Blue;

			var imagePosition = text.IndexOf("inline image: ", StringComparison.Ordinal) + "inline image: ".Length;
			using (var stream = new MemoryStream(_imageBytes).AsRandomAccessStream())
			{
				Editor.Document.GetRange(imagePosition, imagePosition)
					.InsertImage(32, 24, 18, VerticalCharacterAlignment.Baseline, "blue test image", stream);
			}

			Editor.Document.GetText(TextGetOptions.None, out var currentText);
			foreach (var item in new[] { "First list item", "Second list item" })
			{
				var itemStart = currentText.IndexOf(item, StringComparison.Ordinal);
				var format = Editor.Document.GetRange(itemStart, itemStart + item.Length).ParagraphFormat;
				format.ListType = MarkerType.Bullet;
				format.ListStyle = MarkerStyle.Plain;
				format.SetIndents(0, 24, 0);
			}

			var h2o = currentText.IndexOf("H2O", StringComparison.Ordinal);
			Editor.Document.GetRange(h2o + 1, h2o + 2).CharacterFormat.Subscript = FormatEffect.On;
			var x2 = currentText.IndexOf("x2", StringComparison.Ordinal);
			Editor.Document.GetRange(x2 + 1, x2 + 2).CharacterFormat.Superscript = FormatEffect.On;

			var formatting = currentText.IndexOf("formatting", StringComparison.Ordinal);
			Editor.Document.GetRange(formatting, formatting + "formatting".Length).CharacterFormat.BackgroundColor =
				Microsoft.UI.Colors.Yellow;

			var rtlStart = currentText.IndexOf("مرحبا", StringComparison.Ordinal);
			Editor.Document.GetRange(rtlStart, currentText.Length).ParagraphFormat.RightToLeft = FormatEffect.On;
			Editor.Document.Selection.SetRange(0, 0);
		}

		private void InitializeMath()
		{
			MathMlBox.Text = MathMl;
			MathEditor.Document.SetMathMode(RichEditMathMode.MathOnly);
			MathEditor.Document.SetMathML(MathMl);
		}

		private void Run(string success, Action action)
		{
			try
			{
				action();
				UpdateStatus(success);
			}
			catch (Exception error)
			{
				UpdateStatus(error.Message);
			}
		}

		private void UpdateStatus(string? message = null)
		{
			var selection = Editor.Document.Selection;
			Status.Text =
				$"{message ?? "State updated."}\n" +
				$"Text length: {GetDocumentTextLength()}\n" +
				$"Selection: [{selection.StartPosition}, {selection.EndPosition}]\n" +
				$"CanUndo: {Editor.Document.CanUndo()}  CanRedo: {Editor.Document.CanRedo()}";
		}

		private void AppendEvent(string value)
		{
			if (_suppressEvents)
			{
				return;
			}

			_events.Insert(0, $"{DateTime.Now:HH:mm:ss.fff} {value}");
			if (_events.Count > 24)
			{
				_events.RemoveRange(24, _events.Count - 24);
			}
			EventLog.Text = string.Join(Environment.NewLine, _events);
		}

		private ITextSelection Selection => Editor.Document.Selection;

		private void OnSelectAllClick(object sender, RoutedEventArgs e) => Run("All document text selected.", () =>
		{
			Selection.SetRange(0, GetDocumentTextLength());
		});

		private void OnCaretToEndClick(object sender, RoutedEventArgs e) => Run("Caret moved to the document end.", () =>
		{
			var end = GetDocumentTextLength();
			Selection.SetRange(end, end);
			Editor.Focus(FocusState.Programmatic);
		});

		private int GetDocumentTextLength()
		{
			Editor.Document.GetText(TextGetOptions.None, out var text);
			return text.EndsWith('\r') ? text.Length - 1 : text.Length;
		}

		private void OnSelectFormattingClick(object sender, RoutedEventArgs e) => FindText("formatting");

		private void OnJumpRtfClick(object sender, RoutedEventArgs e) => BringIntoView(RtfBox, "RTF section focused.");
		private void OnJumpMathClick(object sender, RoutedEventArgs e) => BringIntoView(MathMlBox, "MathML section focused.");
		private void OnJumpEventsClick(object sender, RoutedEventArgs e) => BringIntoView(EventLog, "Event log focused.");

		private void BringIntoView(Control target, string message)
		{
			target.StartBringIntoView();
			target.Focus(FocusState.Programmatic);
			UpdateStatus(message);
		}

		private void OnResetClick(object sender, RoutedEventArgs e) => Run("Sample document restored.", () =>
		{
			ReadOnlyToggle.IsOn = false;
			SpellCheckToggle.IsOn = true;
			PredictionToggle.IsOn = true;
			ColorFontToggle.IsOn = true;
			WrapToggle.IsOn = true;
			SuppressKeyboardToggle.IsOn = false;
			CopyFormatBox.SelectedIndex = 0;
			SeedDocument();
			InitializeMath();
			Editor.Document.ClearUndoRedoHistory();
			MathEditor.Document.ClearUndoRedoHistory();
			ExportRtf();
			ClearEventsAfterPendingNotifications();
		});

		private void ClearEventsAfterPendingNotifications()
		{
			_events.Clear();
			EventLog.Text = string.Empty;
			DispatcherQueue.TryEnqueue(() =>
			{
				_events.Clear();
				EventLog.Text = string.Empty;
			});
		}

		private void OnUndoClick(object sender, RoutedEventArgs e) => Run("Undo requested.", () =>
		{
			if (Editor.Document.CanUndo())
			{
				Editor.Document.Undo();
			}
		});

		private void OnRedoClick(object sender, RoutedEventArgs e) => Run("Redo requested.", () =>
		{
			if (Editor.Document.CanRedo())
			{
				Editor.Document.Redo();
			}
		});

		private void OnBoldClick(object sender, RoutedEventArgs e) => Run("Bold toggled.", () =>
			Selection.CharacterFormat.Bold =
				Selection.CharacterFormat.Bold == FormatEffect.On ? FormatEffect.Off : FormatEffect.On);

		private void OnItalicClick(object sender, RoutedEventArgs e) => Run("Italic toggled.", () =>
			Selection.CharacterFormat.Italic =
				Selection.CharacterFormat.Italic == FormatEffect.On ? FormatEffect.Off : FormatEffect.On);

		private void OnUnderlineClick(object sender, RoutedEventArgs e) => Run("Underline toggled.", () =>
			Selection.CharacterFormat.Underline =
				Selection.CharacterFormat.Underline == UnderlineType.Single ? UnderlineType.None : UnderlineType.Single);

		private void OnStrikeClick(object sender, RoutedEventArgs e) => Run("Strikethrough toggled.", () =>
			Selection.CharacterFormat.Strikethrough =
				Selection.CharacterFormat.Strikethrough == FormatEffect.On ? FormatEffect.Off : FormatEffect.On);

		private void OnFontIncreaseClick(object sender, RoutedEventArgs e) => Run("Font size increased.", () =>
		{
			var current = Selection.CharacterFormat.Size;
			Selection.CharacterFormat.Size = current > 0 ? current + 2 : 16;
		});

		private void OnFontDecreaseClick(object sender, RoutedEventArgs e) => Run("Font size decreased.", () =>
		{
			var current = Selection.CharacterFormat.Size;
			Selection.CharacterFormat.Size = current > 8 ? current - 2 : 10;
		});

		private void OnRedClick(object sender, RoutedEventArgs e) => Run("Foreground set to red.", () =>
			Selection.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Red);

		private void OnBlueClick(object sender, RoutedEventArgs e) => Run("Foreground set to blue.", () =>
			Selection.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Blue);

		private void OnYellowHighlightClick(object sender, RoutedEventArgs e) => Run("Background set to yellow.", () =>
			Selection.CharacterFormat.BackgroundColor = Microsoft.UI.Colors.Yellow);

		private void OnLeftClick(object sender, RoutedEventArgs e) => SetAlignment(ParagraphAlignment.Left);
		private void OnCenterClick(object sender, RoutedEventArgs e) => SetAlignment(ParagraphAlignment.Center);
		private void OnRightClick(object sender, RoutedEventArgs e) => SetAlignment(ParagraphAlignment.Right);

		private void SetAlignment(ParagraphAlignment alignment) => Run($"Paragraph alignment: {alignment}.", () =>
			Selection.ParagraphFormat.Alignment = alignment);

		private void OnBulletClick(object sender, RoutedEventArgs e) => Run("Bullet list applied.", () =>
		{
			Selection.ParagraphFormat.ListType = MarkerType.Bullet;
			Selection.ParagraphFormat.ListStyle = MarkerStyle.Plain;
		});

		private void OnNumberClick(object sender, RoutedEventArgs e) => Run("Numbered list applied.", () =>
		{
			Selection.ParagraphFormat.ListType = MarkerType.Arabic;
			Selection.ParagraphFormat.ListStyle = MarkerStyle.Period;
		});

		private void OnIndentClick(object sender, RoutedEventArgs e) => Run("Paragraph indented.", () =>
			Selection.ParagraphFormat.SetIndents(0, 36, 12));

		private void OnCopyClick(object sender, RoutedEventArgs e) => Run("Selection copied.", Selection.Copy);
		private void OnCutClick(object sender, RoutedEventArgs e) => Run("Selection cut.", Selection.Cut);
		private void OnPasteClick(object sender, RoutedEventArgs e) => Run("Best clipboard format pasted.", () => Selection.Paste(0));

		private void OnAddLinkClick(object sender, RoutedEventArgs e) => Run("Link applied to the selection.", () =>
		{
			var start = Selection.StartPosition;
			if (Selection.StartPosition == Selection.EndPosition)
			{
				Selection.Text = "Uno Platform";
				Selection.SetRange(start, start + "Uno Platform".Length);
			}
			Selection.Link = "\"https://platform.uno\"";
			Selection.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Blue;
		});

		private void OnInsertImageClick(object sender, RoutedEventArgs e) => Run("Inline image inserted.", () =>
		{
			using var stream = new MemoryStream(_imageBytes).AsRandomAccessStream();
			Selection.InsertImage(40, 30, 22, VerticalCharacterAlignment.Baseline, "blue test image", stream);
		});

		private void OnFindClick(object sender, RoutedEventArgs e) => FindText(FindBox.Text);

		private void FindText(string value) => Run($"Find: {value}", () =>
		{
			Editor.Document.GetText(TextGetOptions.None, out var text);
			var range = Editor.Document.GetRange(0, text.Length);
			if (range.FindText(value, text.Length, FindOptions.None) > 0)
			{
				Editor.Document.Selection.SetRange(range.StartPosition, range.EndPosition);
				range.ScrollIntoView(PointOptions.Start);
			}
			else
			{
				throw new InvalidOperationException("Text was not found.");
			}
		});

		private void OnOptionsChanged(object sender, RoutedEventArgs e)
		{
			if (Editor is null)
			{
				return;
			}

			Editor.IsReadOnly = ReadOnlyToggle.IsOn;
			Editor.IsSpellCheckEnabled = SpellCheckToggle.IsOn;
			Editor.IsTextPredictionEnabled = PredictionToggle.IsOn;
			Editor.IsColorFontEnabled = ColorFontToggle.IsOn;
			Editor.TextWrapping = WrapToggle.IsOn ? TextWrapping.Wrap : TextWrapping.NoWrap;
			Editor.PreventKeyboardDisplayOnProgrammaticFocus = SuppressKeyboardToggle.IsOn;
			UpdateStatus("Options updated.");
		}

		private void OnCopyFormatChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Editor is null)
			{
				return;
			}

			Editor.ClipboardCopyFormat = CopyFormatBox.SelectedIndex == 1
				? RichEditClipboardFormat.PlainText
				: RichEditClipboardFormat.AllFormats;
			UpdateStatus($"Clipboard copy format: {Editor.ClipboardCopyFormat}.");
		}

		private void OnExportRtfClick(object sender, RoutedEventArgs e) => Run("RTF exported.", ExportRtf);

		private void ExportRtf()
		{
			Editor.Document.GetText(TextGetOptions.FormatRtf, out var rtf);
			RtfBox.Text = rtf;
		}

		private void OnImportRtfClick(object sender, RoutedEventArgs e) => Run("RTF imported.", () =>
			Editor.Document.SetText(TextSetOptions.FormatRtf, RtfBox.Text));

		private void OnLoadMathClick(object sender, RoutedEventArgs e) => Run("MathML loaded.", () =>
			MathEditor.Document.SetMathML(MathMlBox.Text));

		private void OnExtractMathClick(object sender, RoutedEventArgs e) => Run("MathML extracted.", () =>
		{
			MathEditor.Document.GetMathML(out var mathMl);
			MathMlBox.Text = mathMl;
		});

		private void OnUndoMathClick(object sender, RoutedEventArgs e) => Run("MathML undo requested.", () =>
		{
			if (MathEditor.Document.CanUndo())
			{
				MathEditor.Document.Undo();
			}
		});

		private void OnLoadStressClick(object sender, RoutedEventArgs e) => Run("Loaded 20,000 alternating rich runs.", () =>
		{
			var rtf = new StringBuilder(@"{\rtf1\ansi ");
			for (var index = 0; index < 20_000; index++)
			{
				rtf.Append(index % 2 == 0 ? @"\b " : @"\b0 ");
				rtf.Append((char)('a' + index % 26));
			}
			rtf.Append('}');
			Editor.Document.SetText(TextSetOptions.FormatRtf, rtf.ToString());
			Editor.Document.Selection.SetRange(19_500, 19_501);
			Editor.Document.Selection.CharacterFormat.ForegroundColor = Microsoft.UI.Colors.Blue;
			Editor.Document.Selection.ScrollIntoView(PointOptions.Start);
		});
	}
}
