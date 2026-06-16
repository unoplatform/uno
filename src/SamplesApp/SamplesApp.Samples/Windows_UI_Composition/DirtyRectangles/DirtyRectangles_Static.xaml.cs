using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DirtyRectangles
{
	/// <summary>
	/// A fully static scene used as a deterministic baseline for dirty-rectangles validation.
	/// </summary>
	[Sample("Windows.UI.Composition", Name = "DirtyRectangles_Static", IsManualTest = true,
		Description = "A fully static scene for dirty-rectangles baseline validation.")]
	public sealed partial class DirtyRectangles_Static : Page
	{
		public DirtyRectangles_Static()
		{
			this.InitializeComponent();
		}
	}
}
