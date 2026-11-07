using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents
{
	public partial class TextHighlighter
	{
		public Brush Foreground
		{
			get => (Brush)this.GetValue(ForegroundProperty);
			set
			{
				ValidateBrush(value);
				this.SetValue(ForegroundProperty, value);
			}
		}

		public Brush Background
		{
			get => (Brush)this.GetValue(BackgroundProperty);
			set
			{
				ValidateBrush(value);
				this.SetValue(BackgroundProperty, value);
			}
		}

		public IList<TextRange> Ranges { get; } = new ObservableCollection<TextRange>();

		public static DependencyProperty BackgroundProperty { get; } =
			DependencyProperty.Register(
				nameof(Background),
				typeof(Brush),
				typeof(TextHighlighter),
				new FrameworkPropertyMetadata(default(Brush)));

		public static DependencyProperty ForegroundProperty { get; } =
			DependencyProperty.Register(
				nameof(Foreground),
				typeof(Brush),
				typeof(TextHighlighter),
				new FrameworkPropertyMetadata(default(Brush)));

		public TextHighlighter()
		{
		}

		// WinUI only allows a SolidColorBrush (or null) for Foreground/Background:
		// https://github.com/microsoft/microsoft-ui-xaml/blob/8463f45162149de0ec3ad7df752596893fe3e13e/dxaml/xcp/components/text/TextHighlighter.cpp#L43-L68
		private static void ValidateBrush(Brush value)
		{
			if (value is not null and not SolidColorBrush)
			{
				throw new ArgumentException("TextHighlighter only supports a SolidColorBrush for the Foreground and Background properties.");
			}
		}
	}
}
