#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.UI.Text;

namespace Windows.UI.Xaml.Controls
{
	public  partial class TextBlock : FrameworkElement
	{
		public TextBlock()
		{
			Inlines = new Documents.InlineCollection(this);
		}

		public global::Windows.UI.Xaml.Documents.InlineCollection Inlines { get; }
		
		#region TextDecorations

		public TextDecorations TextDecorations
		{
			get { return (TextDecorations)this.GetValue(TextDecorationsProperty); }
			set { this.SetValue(TextDecorationsProperty, value); }
		}

		public static DependencyProperty TextDecorationsProperty =
			DependencyProperty.Register(
				"TextDecorations",
				typeof(int),
				typeof(TextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextDecorations.None,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((TextBlock)s).OnTextDecorationsChanged()
				)
			);

		private void OnTextDecorationsChanged()
		{
			OnTextDecorationsChangedPartial();
			this.InvalidateMeasure();
		}

		partial void OnTextDecorationsChangedPartial();

		#endregion
	}
}
