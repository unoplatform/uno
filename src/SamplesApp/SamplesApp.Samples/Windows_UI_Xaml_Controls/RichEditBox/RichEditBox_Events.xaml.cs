using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.RichEditBoxControl
{
	[Sample("RichEditBox", Name = "RichEditBox_Events", Description = "RichEditBox event surface on Skia: TextChanging/TextChanged, SelectionChanging (cancellable)/SelectionChanged, and CopyingToClipboard/CuttingToClipboard/Paste (suppressible).")]
	public sealed partial class RichEditBox_Events : Page
	{
		private readonly List<string> _eventLog = new();

		public RichEditBox_Events()
		{
			this.InitializeComponent();

			Editor.TextChanging += (s, e) => Append($"TextChanging (IsContentChanging={e.IsContentChanging})");
			Editor.TextChanged += (s, e) => Append("TextChanged");

			Editor.SelectionChanging += (s, e) =>
			{
				if (CancelSelectionToggle.IsOn)
				{
					e.Cancel = true;
					Append($"SelectionChanging CANCELLED (proposed [{e.SelectionStart}, {e.SelectionStart + e.SelectionLength}])");
				}
				else
				{
					Append($"SelectionChanging (proposed [{e.SelectionStart}, {e.SelectionStart + e.SelectionLength}])");
				}
			};
			Editor.SelectionChanged += (s, e) =>
			{
				var selection = Editor.Document.Selection;
				Append($"SelectionChanged ([{selection.StartPosition}, {selection.EndPosition}])");
			};

			Editor.CopyingToClipboard += (s, e) =>
			{
				e.Handled = SuppressCopyToggle.IsOn;
				Append(e.Handled ? "CopyingToClipboard SUPPRESSED" : "CopyingToClipboard");
			};
			Editor.CuttingToClipboard += (s, e) =>
			{
				e.Handled = SuppressCutToggle.IsOn;
				Append(e.Handled ? "CuttingToClipboard SUPPRESSED" : "CuttingToClipboard");
			};
			Editor.Paste += (s, e) =>
			{
				e.Handled = SuppressPasteToggle.IsOn;
				Append(e.Handled ? "Paste SUPPRESSED" : "Paste");
			};
		}

		private void Append(string entry)
		{
			_eventLog.Insert(0, entry);
			if (_eventLog.Count > 20)
			{
				_eventLog.RemoveRange(20, _eventLog.Count - 20);
			}

			Log.Text = string.Join(Environment.NewLine, _eventLog);
		}

		private void OnClearLogClick(object sender, RoutedEventArgs e)
		{
			_eventLog.Clear();
			Log.Text = string.Empty;
		}
	}
}
