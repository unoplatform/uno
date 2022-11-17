// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;
using System;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;

using TextCommandBarFlyout = Microsoft.UI.Xaml.Controls.TextCommandBarFlyout;

namespace MUXControlsTestApp
{
	[Sample("CommandBarFlyout", "WinUI")]
    public sealed partial class TextCommandBarFlyoutPage : TestPage
    {
        public TextCommandBarFlyoutPage()
        {
            this.InitializeComponent();

#if !HAS_UNO
			RichEditBox1.Document.SetText(Windows.UI.Text.TextSetOptions.None, "Lorem ipsum ergo sum");
#endif
			Clipboard.ContentChanged += OnClipboardContentChanged;

            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode", "BottomEdgeAlignedLeft"))
            {
                TextControlContextFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
            }
            else
            {
                TextControlContextFlyout.Placement = FlyoutPlacementMode.Top;
            }

            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode", "TopEdgeAlignedLeft"))
            {
                TextControlSelectionFlyout.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;
            }
            else
            {
                TextControlSelectionFlyout.Placement = FlyoutPlacementMode.Top;
            }

            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ContextFlyout"))
            {
                TextBox1.ContextFlyout = TextControlContextFlyout;
                TextBlock1.ContextFlyout = TextControlContextFlyout;
#if !HAS_UNO
				RichEditBox1.ContextFlyout = TextControlContextFlyout;
#endif
				RichTextBlock1.ContextFlyout = TextControlContextFlyout;
                PasswordBox1.ContextFlyout = TextControlContextFlyout;
            }

            try
            {
                if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.TextBox", "SelectionFlyout"))
                {
                    TextBox1.SelectionFlyout = TextControlSelectionFlyout;
                }

                if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.TextBlock", "SelectionFlyout"))
                {
                    TextBlock1.SelectionFlyout = TextControlSelectionFlyout;
                }

                if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.RichEditBox", "SelectionFlyout"))
                {
#if !HAS_UNO
					RichEditBox1.SelectionFlyout = TextControlSelectionFlyout;
#endif
				}

                if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.RichTextBlock", "SelectionFlyout"))
                {
                    RichTextBlock1.SelectionFlyout = TextControlSelectionFlyout;
                }

                if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.PasswordBox", "SelectionFlyout"))
                {
                    PasswordBox1.SelectionFlyout = TextControlSelectionFlyout;
                }
            }
            catch (InvalidCastException)
            {
                // RS5 interfaces can change before release, so we need to make sure we don't crash if they do.
            }
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
#if !HAS_UNO
			RichEditBox1.Document.Selection.Expand(Windows.UI.Text.TextRangeUnit.Story);
#endif
		}

        private void OnRichTextBlockSelectAllClicked(object sender, object args)
        {
            RichTextBlock1.SelectAll();
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
#if !HAS_UNO
			RichEditBox1.Document.Selection.Collapse(true);
#endif
		}

        private void OnTextBoxFillWithTextClicked(object sender, object args)
        {
            TextBox1.Text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        }

        private void OnRichEditBoxFillWithTextClicked(object sender, object args)
        {
#if !HAS_UNO
            RichEditBox1.Document.SetText(TextSetOptions.None, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
#endif
		}

        private void OnRichTextBlockClearSelectionClicked(object sender, object args)
        {
            RichTextBlock1.Select(RichTextBlock1.ContentStart, RichTextBlock1.ContentStart);
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
#if !HAS_UNO
			ShowTextControlContextFlyoutTransient(RichEditBox1);
#endif
		}

        private void OnShowTextControlFlyoutOnRichTextBlockClicked(object sender, object args)
        {
            ShowTextControlContextFlyoutTransient(RichTextBlock1);
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
#if !HAS_UNO
			ShowTextControlContextFlyoutStandard(RichEditBox1);
#endif
		}

        private void OnShowStandardTextControlFlyoutOnRichTextBlockClicked(object sender, object args)
        {
            ShowTextControlContextFlyoutStandard(RichTextBlock1);
        }

        private void OnShowStandardTextControlFlyoutOnPasswordBoxClicked(object sender, object args)
        {
            ShowTextControlContextFlyoutStandard(PasswordBox1);
        }

        private void ShowTextControlContextFlyoutTransient(FrameworkElement targetElement)
        {
            if (PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
            {
                TextControlContextFlyout.ShowAt(targetElement, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient });
            }
            else
            {
                TextControlContextFlyout.ShowAt(targetElement);
            }
        }

        private void ShowTextControlContextFlyoutStandard(FrameworkElement targetElement)
        {
            if (PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
            {
                TextControlSelectionFlyout.ShowAt(targetElement, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Standard });
            }
            else
            {
                TextControlSelectionFlyout.ShowAt(targetElement);
            }
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
#if !HAS_UNO
			string rtfContent;
            RichEditBox1.Document.GetText(TextGetOptions.FormatRtf, out rtfContent);
			StatusReportingTextBox.Text = rtfContent;
#endif
		}
    }
}
