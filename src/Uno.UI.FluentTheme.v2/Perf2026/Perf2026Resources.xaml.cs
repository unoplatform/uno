using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI;

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
	public sealed partial class Perf2026Resources : ResourceDictionary, IXamlResourceDictionaryProvider
	{
		public Perf2026Resources()
		{
			InitializeComponent();

			Style.RegisterOptimizedDefaultStyleForType(typeof(AppBarButton), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(AppBarToggleButton), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(Button), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(CheckBox), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(ComboBox), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(ComboBoxItem), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(CommandBar), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(ScrollBar), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(Slider), this);
			Style.RegisterOptimizedDefaultStyleForType(typeof(ToggleSwitch), this);
		}

		ResourceDictionary IXamlResourceDictionaryProvider.GetResourceDictionary() => this;
	}
}
