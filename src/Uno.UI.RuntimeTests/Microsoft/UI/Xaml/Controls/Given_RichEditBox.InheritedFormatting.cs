#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FontStyle = Windows.UI.Text.FontStyle;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_RichEditBox
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_InheritedItalic_CanBeExplicitlyCleared(bool useFontStyle)
	{
		var editor = new RichEditBox { FontStyle = FontStyle.Italic };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "ab");
			Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(0, 2).CharacterFormat.Italic);

			var range = editor.Document.GetRange(0, 1);
			if (useFontStyle)
			{
				range.CharacterFormat.FontStyle = FontStyle.Normal;
			}
			else
			{
				range.CharacterFormat.Italic = FormatEffect.Off;
			}

			Assert.AreEqual(FormatEffect.Off, range.CharacterFormat.Italic);
			Assert.AreEqual(FontStyle.Normal, range.CharacterFormat.FontStyle);
			Assert.AreEqual(FormatEffect.On, editor.Document.GetRange(1, 2).CharacterFormat.Italic);
			Assert.AreEqual(FormatEffect.Undefined, editor.Document.GetRange(0, 2).CharacterFormat.Italic);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_ExplicitItalicReset_SurvivesCloneAndUndo()
	{
		var editor = new RichEditBox { FontStyle = FontStyle.Italic };
		try
		{
			WindowHelper.WindowContent = editor;
			await WindowHelper.WaitForLoaded(editor);
			editor.Document.SetText(TextSetOptions.None, "ab");
			var range = editor.Document.GetRange(0, 1);
			var original = range.CharacterFormat.GetClone();
			Assert.AreEqual(FormatEffect.On, original.Italic);
			editor.Document.ClearUndoRedoHistory();
			editor.Document.BeginUndoGroup();
			try
			{
				range.CharacterFormat.Italic = FormatEffect.Off;
			}
			finally
			{
				editor.Document.EndUndoGroup();
			}

			var reset = range.CharacterFormat.GetClone();
			Assert.AreEqual(FormatEffect.Off, reset.Italic);
			editor.Document.Undo();
			Assert.AreEqual(original.Italic, range.CharacterFormat.Italic);
			editor.Document.Redo();
			Assert.AreEqual(FormatEffect.Off, range.CharacterFormat.Italic);

			var other = editor.Document.GetRange(1, 2);
			Assert.AreEqual(FormatEffect.On, other.CharacterFormat.Italic);
			other.CharacterFormat.SetClone(reset);
			Assert.AreEqual(FormatEffect.Off, other.CharacterFormat.Italic);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_InheritedItalic_CloneAndCaretToggle_UseEffectiveValues()
	{
		var source = new RichEditBox { FontStyle = FontStyle.Italic };
		var target = new RichEditBox();
		var panel = new StackPanel { Children = { source, target } };
		try
		{
			WindowHelper.WindowContent = panel;
			await WindowHelper.WaitForLoaded(panel);
			source.Document.SetText(TextSetOptions.None, "a");
			target.Document.SetText(TextSetOptions.None, "b");
			target.Document.GetRange(0, 1).CharacterFormat.SetClone(source.Document.GetRange(0, 1).CharacterFormat.GetClone());
			Assert.AreEqual(FormatEffect.On, target.Document.GetRange(0, 1).CharacterFormat.Italic);

			source.Document.Selection.SetRange(1, 1);
			source.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
			Assert.AreEqual(FormatEffect.Off, source.Document.Selection.CharacterFormat.Italic);
			source.Document.Selection.TypeText("x");
			Assert.AreEqual(FormatEffect.Off, source.Document.GetRange(1, 2).CharacterFormat.Italic);
			Assert.AreEqual(FormatEffect.On, source.Document.GetRange(0, 1).CharacterFormat.Italic);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
