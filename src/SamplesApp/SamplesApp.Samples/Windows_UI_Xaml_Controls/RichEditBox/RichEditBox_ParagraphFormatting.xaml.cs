using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_ParagraphFormatting", Description = "Functional RichEditBox on Skia: apply a uniform paragraph alignment (Left/Center/Right/Justify) to the whole document via the Text Object Model, projected onto the shared DisplayBlock, with undo/redo.")]
	public sealed partial class RichEditBox_ParagraphFormatting : Page
	{
		// A multi-paragraph document whose lines are long enough to wrap inside the constrained editor,
		// so a uniform alignment is visible across the wrapped lines.
		private const string InitialText =
			"Uno Platform lets you build cross-platform apps from a single codebase.\r" +
			"This paragraph is deliberately long so that it wraps across several lines inside the editor.\r" +
			"Use the buttons below to change the alignment for the entire document.";

		public RichEditBox_ParagraphFormatting()
		{
			this.InitializeComponent();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Editor.Document.SetText(TextSetOptions.None, InitialText);
			Output.Text = "Apply an alignment to the whole document.";
		}

		// Returns a range covering the entire document story so the alignment is applied uniformly to
		// every paragraph (the case the single-TextBlock DisplayBlock can render).
		private ITextRange GetWholeDocument()
		{
			Editor.Document.GetText(TextGetOptions.None, out var text);
			return Editor.Document.GetRange(0, text.Length);
		}

		private void Run(Func<string> action)
		{
			try
			{
				Output.Text = action();
			}
			catch (Exception ex)
			{
				Output.Text = ex.Message;
			}
		}

		private void OnLeftClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			GetWholeDocument().ParagraphFormat.Alignment = ParagraphAlignment.Left;
			return "Alignment: Left.";
		});

		private void OnCenterClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			GetWholeDocument().ParagraphFormat.Alignment = ParagraphAlignment.Center;
			return "Alignment: Center.";
		});

		private void OnRightClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			GetWholeDocument().ParagraphFormat.Alignment = ParagraphAlignment.Right;
			return "Alignment: Right.";
		});

		private void OnJustifyClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			GetWholeDocument().ParagraphFormat.Alignment = ParagraphAlignment.Justify;
			return "Alignment: Justify.";
		});

		private void OnUndoClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			if (!Editor.Document.CanUndo())
			{
				return "Nothing to undo.";
			}

			Editor.Document.Undo();
			return "Undone.";
		});

		private void OnRedoClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			if (!Editor.Document.CanRedo())
			{
				return "Nothing to redo.";
			}

			Editor.Document.Redo();
			return "Redone.";
		});

		private void OnResetClick(object sender, RoutedEventArgs e) => Run(() =>
		{
			Editor.Document.SetText(TextSetOptions.None, InitialText);
			return "Document reset (alignment Left).";
		});
	}
}
