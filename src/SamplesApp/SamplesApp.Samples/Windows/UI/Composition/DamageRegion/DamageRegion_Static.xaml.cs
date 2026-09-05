using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Composition.DamageRegion
{
	[Sample("Windows.UI.Composition", Name = "DamageRegion_Static", IsManualTest = true,
		Description = "A fully static scene for damage-region baseline validation.")]
	public sealed partial class DamageRegion_Static : Page
	{
		public DamageRegion_Static()
		{
			this.InitializeComponent();
		}
	}
}
