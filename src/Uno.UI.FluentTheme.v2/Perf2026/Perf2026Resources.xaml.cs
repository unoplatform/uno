using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace Uno.UI.FluentTheme.v2
{
	/// <summary>
	/// Optimized (perf2026) variants of the Fluent v2 control styles.
	/// </summary>
	/// <remarks>
	/// This dictionary is overlaid on <see cref="Microsoft.UI.Xaml.Controls.XamlControlsResources"/>
	/// when <see cref="global::Uno.UI.FeatureConfiguration.Style.UseDefaultStyleOptimizations"/> is
	/// enabled. It is not meant to be merged directly by apps.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed partial class Perf2026Resources : ResourceDictionary
	{
		public Perf2026Resources()
		{
			InitializeComponent();
		}
	}
}
