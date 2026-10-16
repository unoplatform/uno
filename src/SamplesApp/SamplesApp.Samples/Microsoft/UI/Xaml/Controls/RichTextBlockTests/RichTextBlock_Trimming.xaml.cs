using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.RichTextBlockTests;

[Sample("RichTextBlock", Name = "RichTextBlock_Trimming", Description =
	"Trimming matrix: TextTrimming (None/CharacterEllipsis/WordEllipsis) crossed with TextWrapping " +
	"(Wrap/NoWrap), all with MaxLines=2. Drag the Width slider and watch the ellipsis and each " +
	"IsTextTrimmed status flip; every flip is also appended to the event log.",
	IsManualTest = true)]
public sealed partial class RichTextBlock_Trimming : Page
{
	private const int MaxLogEntries = 20;

	private readonly List<string> _logEntries = new();

	public RichTextBlock_Trimming()
	{
		this.InitializeComponent();

		Track(Rtb_None_Wrap, Status_None_Wrap, "None / Wrap");
		Track(Rtb_None_NoWrap, Status_None_NoWrap, "None / NoWrap");
		Track(Rtb_CharEllipsis_Wrap, Status_CharEllipsis_Wrap, "CharacterEllipsis / Wrap");
		Track(Rtb_CharEllipsis_NoWrap, Status_CharEllipsis_NoWrap, "CharacterEllipsis / NoWrap");
		Track(Rtb_WordEllipsis_Wrap, Status_WordEllipsis_Wrap, "WordEllipsis / Wrap");
		Track(Rtb_WordEllipsis_NoWrap, Status_WordEllipsis_NoWrap, "WordEllipsis / NoWrap");
	}

	private void Track(RichTextBlock richTextBlock, TextBlock status, string label)
	{
		status.Text = $"IsTextTrimmed: {richTextBlock.IsTextTrimmed}";

		richTextBlock.IsTextTrimmedChanged += (s, e) =>
		{
			status.Text = $"IsTextTrimmed: {richTextBlock.IsTextTrimmed}";

			_logEntries.Insert(0, $"{label}: IsTextTrimmed={richTextBlock.IsTextTrimmed}");
			while (_logEntries.Count > MaxLogEntries)
			{
				_logEntries.RemoveAt(_logEntries.Count - 1);
			}

			TrimLog.ItemsSource = _logEntries.ToList();
		};
	}
}
