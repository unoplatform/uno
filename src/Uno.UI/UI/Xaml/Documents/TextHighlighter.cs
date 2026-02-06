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
			set => this.SetValue(ForegroundProperty, value);
		}

		public Brush Background
		{
			get => (Brush)this.GetValue(BackgroundProperty);
			set => this.SetValue(BackgroundProperty, value);
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
	}
}
