// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\CommandBarFlyout\TestUI\TextCommandBarFlyoutPage.xaml.cs, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

using Common;
using System;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;

using TextCommandBarFlyout = Microsoft.UI.Xaml.Controls.TextCommandBarFlyout;

namespace MUXControlsTestApp;

// RichEditBox and RichTextBlock code is commented out because Uno does not yet fully
// support these controls (tracked by GitHub issue #81).
// Re-enable the commented-out sections below when RichEditBox and RichTextBlock gain
// support for ContextFlyout, SelectionFlyout, and text selection operations.

[Sample("CommandBarFlyout", "WinUI")]
public sealed partial class TextCommandBarFlyoutPage : TestPage
{
	public TextCommandBarFlyoutPage()
	{
		this.InitializeComponent();

		//RichEditBox1.Document.SetText(TextSetOptions.None, "Lorem ipsum ergo sum");
		Clipboard.ContentChanged += OnClipboardContentChanged;

		TextControlContextFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
		TextControlSelectionFlyout.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
		TextBox1.ContextFlyout = TextControlContextFlyout;
		TextBlock1.ContextFlyout = TextControlContextFlyout;
		//RichEditBox1.ContextFlyout = TextControlContextFlyout;
		//RichTextBlock1.ContextFlyout = TextControlContextFlyout;
		PasswordBox1.ContextFlyout = TextControlContextFlyout;
		TextBox1.SelectionFlyout = TextControlSelectionFlyout;
		TextBlock1.SelectionFlyout = TextControlSelectionFlyout;
		//RichEditBox1.SelectionFlyout = TextControlSelectionFlyout;
		//RichTextBlock1.SelectionFlyout = TextControlSelectionFlyout;
		PasswordBox1.SelectionFlyout = TextControlSelectionFlyout;
	}

	private async void OnClipboardContentChanged(object sender, object e)
	{
		var clipboardContent = Clipboard.GetContent();

		if (clipboardContent.Contains(StandardDataFormats.Rtf))
		{
			RecordEvent(string.Format("Clipboard content changed to '{0}'", await Clipboard.GetContent().GetRtfAsync()));
		}
		else if (clipboardContent.Contains(StandardDataFormats.Text))
		{
			RecordEvent(string.Format("Clipboard content changed to '{0}'", await Clipboard.GetContent().GetTextAsync()));
		}
		else
		{
			RecordEvent(string.Format("Clipboard content changed to ''"));
		}
	}

	private void RecordEvent(string eventString)
	{
		StatusReportingTextBox.Text = eventString;
	}

	private void RecordEvent(object sender, string eventString)
	{
		DependencyObject senderAsDO = sender as DependencyObject;

		if (senderAsDO != null)
		{
			RecordEvent(AutomationProperties.GetAutomationId(senderAsDO) + " " + eventString);
		}
	}

	private void OnTextBoxSelectAllClicked(object sender, object args)
	{
		TextBox1.SelectAll();
	}

	private void OnTextBlockSelectAllClicked(object sender, object args)
	{
		TextBlock1.SelectAll();
	}

	private void OnRichEditBoxSelectAllClicked(object sender, object args)
	{
		//RichEditBox1.Document.Selection.Expand(TextRangeUnit.Story);
	}

	private void OnRichTextBlockSelectAllClicked(object sender, object args)
	{
		//RichTextBlock1.SelectAll();
	}

	private void OnTextBoxClearSelectionClicked(object sender, object args)
	{
		TextBox1.Select(0, 0);
	}

	private void OnTextBlockClearSelectionClicked(object sender, object args)
	{
		TextBlock1.Select(TextBlock1.ContentStart, TextBlock1.ContentStart);
	}

	private void OnRichEditBoxClearSelectionClicked(object sender, object args)
	{
		//RichEditBox1.Document.Selection.Collapse(true);
	}

	private void OnTextBoxFillWithTextClicked(object sender, object args)
	{
		TextBox1.Text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
	}

	private void OnRichEditBoxFillWithTextClicked(object sender, object args)
	{
		//RichEditBox1.Document.SetText(TextSetOptions.None, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
	}

	private void OnRichTextBlockClearSelectionClicked(object sender, object args)
	{
		//RichTextBlock1.Select(RichTextBlock1.ContentStart, RichTextBlock1.ContentStart);
	}

	private void OnShowTextControlFlyoutOnTextBoxClicked(object sender, object args)
	{
		ShowTextControlContextFlyoutTransient(TextBox1);
	}

	private void OnShowTextControlFlyoutOnTextBlockClicked(object sender, object args)
	{
		ShowTextControlContextFlyoutTransient(TextBlock1);
	}

	private void OnShowTextControlFlyoutOnRichEditBoxClicked(object sender, object args)
	{
		//ShowTextControlContextFlyoutTransient(RichEditBox1);
	}

	private void OnShowTextControlFlyoutOnRichTextBlockClicked(object sender, object args)
	{
		//ShowTextControlContextFlyoutTransient(RichTextBlock1);
	}

	private void OnShowTextControlFlyoutOnPasswordBoxClicked(object sender, object args)
	{
		ShowTextControlContextFlyoutTransient(PasswordBox1);
	}

	private void OnShowStandardTextControlFlyoutOnTextBoxClicked(object sender, object args)
	{
		ShowTextControlContextFlyoutStandard(TextBox1);
	}

	private void OnShowStandardTextControlFlyoutOnTextBlockClicked(object sender, object args)
	{
		ShowTextControlContextFlyoutStandard(TextBlock1);
	}

	private void OnShowStandardTextControlFlyoutOnRichEditBoxClicked(object sender, object args)
	{
		//ShowTextControlContextFlyoutStandard(RichEditBox1);
	}

	private void OnShowStandardTextControlFlyoutOnRichTextBlockClicked(object sender, object args)
	{
		//ShowTextControlContextFlyoutStandard(RichTextBlock1);
	}

	private void OnShowStandardTextControlFlyoutOnPasswordBoxClicked(object sender, object args)
	{
		ShowTextControlContextFlyoutStandard(PasswordBox1);
	}

	private void ShowTextControlContextFlyoutTransient(FrameworkElement targetElement)
	{
		TextControlContextFlyout.ShowAt(targetElement, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient });
	}

	private void ShowTextControlContextFlyoutStandard(FrameworkElement targetElement)
	{
		TextControlSelectionFlyout.ShowAt(targetElement, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Standard });
	}

	private void OnClearClipboardContentsClicked(object sender, object args)
	{
		Clipboard.Clear();
	}

	private void OnSetClipboardContentsClicked(object sender, object args)
	{
		DataPackage dataPackage = new DataPackage();

		dataPackage.RequestedOperation = DataPackageOperation.Copy;
		dataPackage.SetText("Automatically set clipboard text");

		Clipboard.SetContent(dataPackage);
	}

	private void OnGetRichEditBoxRtfContentClicked(object sender, object args)
	{
		string rtfContent;
		RichEditBox1.Document.GetText(TextGetOptions.FormatRtf, out rtfContent);
		StatusReportingTextBox.Text = rtfContent;
	}
}
