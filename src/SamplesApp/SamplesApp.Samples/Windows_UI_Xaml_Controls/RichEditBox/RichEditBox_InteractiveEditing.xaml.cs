using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
#if __SKIA__ || !HAS_UNO
	[Sample("RichEditBox", Name = "RichEditBox_InteractiveEditing", Description = "RichEditBox parity probe for keyboard, pointer, caret, selection, undo and redo behavior.")]
#endif
	public sealed partial class RichEditBox_InteractiveEditing : Page
	{
		public RichEditBox_InteractiveEditing()
		{
			this.InitializeComponent();
		}

		private void OnSnapshotClick(object sender, RoutedEventArgs e)
		{
			try
			{
				Editor.Document.GetText(TextGetOptions.None, out var text);
				var selection = Editor.Document.Selection;
				Output.Text =
					$"Text (len {text.Length}): \"{text}\"\n" +
					$"Selection: [{selection.StartPosition}, {selection.EndPosition}]  CanUndo: {Editor.Document.CanUndo()}  CanRedo: {Editor.Document.CanRedo()}";
			}
			catch (Exception ex)
			{
				Output.Text = ex.Message;
			}
		}

		private void OnUndoClick(object sender, RoutedEventArgs e)
		{
			Editor.Document.Undo();
			OnSnapshotClick(sender, e);
		}

		private void OnRedoClick(object sender, RoutedEventArgs e)
		{
			Editor.Document.Redo();
			OnSnapshotClick(sender, e);
		}
	}
}
