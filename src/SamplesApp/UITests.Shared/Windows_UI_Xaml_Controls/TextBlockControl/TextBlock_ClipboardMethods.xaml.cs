using System;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock", IsManualTest = true)]
	public sealed partial class TextBlock_ClipboardMethods : Page
	{
		public TextBlock_ClipboardMethods()
		{
			this.InitializeComponent();
		}

		private void SelectAllText()
		{
			try
			{
				// Focus the text block so the selection is visually visible
				TestTextBlock.Focus(FocusState.Programmatic);
				await Task.Delay(100);
				TestTextBlock.SelectAll();
				UpdateLastCopyResult("✅ SelectAll() called successfully");
			}
			catch (Exception ex)
			{
				UpdateLastCopyResult($"❌ SelectAll() failed: {ex.Message}");
			}
		}

		private async void CopySelectionToClipboard()
		{
			try
			{
				TestTextBlock.CopySelectionToClipboard();
				UpdateLastCopyResult("✅ CopySelectionToClipboard() called successfully");

				// Verify the clipboard content
				await Task.Delay(100); // Small delay to ensure clipboard is updated
				await VerifyClipboardContentAsync();
			}
			catch (Exception ex)
			{
				UpdateLastCopyResult($"❌ CopySelectionToClipboard() failed: {ex.Message}");
			}
		}

		private void ClearSelection()
		{
			try
			{
				// For now, just move focus away to clear selection  
				// There's no public method to clear selection on TextBlock
				ContentInput.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
				UpdateLastCopyResult("✅ Selection cleared by moving focus");
			}
			catch (Exception ex)
			{
				UpdateLastCopyResult($"❌ Clear selection failed: {ex.Message}");
			}
		}

		private void UpdateContent()
		{
			if (!string.IsNullOrWhiteSpace(ContentInput.Text))
			{
				TestTextBlock.Text = ContentInput.Text;
				ContentInput.Text = "";
				UpdateLastCopyResult($"✅ TextBlock content updated");
			}
		}

		private async void PasteFromClipboard()
		{
			try
			{
				var clipboardContent = Clipboard.GetContent();
				if (clipboardContent != null && clipboardContent.Contains(StandardDataFormats.Text))
				{
					var text = await clipboardContent.GetTextAsync();
					PasteTarget.Text = text;
					UpdateLastCopyResult($"✅ Pasted from clipboard: '{text}'");
				}
				else
				{
					PasteTarget.Text = "";
					UpdateLastCopyResult("❌ No text content found in clipboard");
				}
			}
			catch (Exception ex)
			{
				UpdateLastCopyResult($"❌ Paste failed: {ex.Message}");
			}
		}

		private async Task VerifyClipboardContentAsync()
		{
			try
			{
				var clipboardContent = Clipboard.GetContent();
				if (clipboardContent != null && clipboardContent.Contains(StandardDataFormats.Text))
				{
					var text = await clipboardContent.GetTextAsync();
					if (!string.IsNullOrEmpty(text))
					{
						UpdateLastCopyResult($"✅ Clipboard verified: '{text}' (Length: {text.Length})");
					}
					else
					{
						UpdateLastCopyResult("⚠️ Clipboard contains empty text");
					}
				}
				else
				{
					UpdateLastCopyResult("⚠️ No text content found in clipboard after copy");
				}
			}
			catch (Exception ex)
			{
				UpdateLastCopyResult($"⚠️ Clipboard verification failed: {ex.Message}");
			}
		}

		private void UpdateLastCopyResult(string message)
		{
			LastCopyResult.Text = $"{DateTimeOffset.Now:HH:mm:ss} - {message}";
		}
	}
}