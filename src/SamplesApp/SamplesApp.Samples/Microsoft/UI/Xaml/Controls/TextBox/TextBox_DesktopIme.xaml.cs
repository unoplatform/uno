#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl;

[Sample("TextBox", IsManualTest = true, IgnoreInSnapshotTests = true, Description = "Desktop IME Unicode offsets, composition and focus ownership across TextBox and RichEditBox.")]
public sealed partial class TextBox_DesktopIme : Page
{
	public TextBox_DesktopIme()
	{
		this.InitializeComponent();
		First.TextChanged += (_, _) => Events.Text = $"First: selection {First.SelectionStart}+{First.SelectionLength}, text {First.Text}";
		Second.TextChanged += (_, _) => Events.Text = $"Second: selection {Second.SelectionStart}+{Second.SelectionLength}, text {Second.Text}";
#if __SKIA__
		First.TextCompositionStarted += (_, args) => Events.Text = $"First preedit started: {args.StartIndex}+{args.Length}";
		First.TextCompositionChanged += (_, args) => Events.Text = $"First preedit changed: {args.StartIndex}+{args.Length}";
		First.TextCompositionEnded += (_, args) => Events.Text = $"First preedit ended: {args.StartIndex}+{args.Length}";
		Second.TextCompositionStarted += (_, args) => Events.Text = $"Second preedit started: {args.StartIndex}+{args.Length}";
		Second.TextCompositionEnded += (_, args) => Events.Text = $"Second preedit ended: {args.StartIndex}+{args.Length}";
		RichEditor.TextCompositionStarted += (_, args) => Events.Text = $"Rich editor preedit started: {args.StartIndex}+{args.Length}";
		RichEditor.TextCompositionChanged += (_, args) => Events.Text = $"Rich editor preedit changed: {args.StartIndex}+{args.Length}";
		RichEditor.TextCompositionEnded += (_, args) => Events.Text = $"Rich editor preedit ended: {args.StartIndex}+{args.Length}";
#endif
	}
}
