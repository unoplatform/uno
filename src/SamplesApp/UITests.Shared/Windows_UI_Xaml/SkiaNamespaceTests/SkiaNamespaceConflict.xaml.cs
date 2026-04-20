using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml.SkiaNamespaceTests
{
	[Sample("XAML", Name = "SkiaNamespaceConflict_14004", Description = "Repro for #14004: xmlns prefix named 'skia' incorrectly treated as conditional XAML namespace")]
	public sealed partial class SkiaNamespaceConflict : UserControl
	{
		public SkiaNamespaceConflict()
		{
			this.InitializeComponent();
		}
	}
}
