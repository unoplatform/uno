using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
    public static partial class GenericStyles
    {
		private static bool _initialized = false;

		public static void Initialize()
		{
			if (!_initialized)
			{
				InitStyles();

				_initialized = true;

				InitializeDefaultStyles();
			}
		}

		private static void InitializeDefaultStyles()
		{
			if (Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles)
			{
				// netstd is disabled until the codegen is fixed on 15.6
#if !NET46 && !NETSTANDARD2_0
				Style.RegisterDefaultStyleForType(typeof(Controls.Button), Uno.UI.GlobalStaticResources.XamlDefaultButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.TextBox), Uno.UI.GlobalStaticResources.XamlDefaultTextBox);
				Style.RegisterDefaultStyleForType(typeof(Controls.CheckBox), Uno.UI.GlobalStaticResources.XamlDefaultCheckBox);
				Style.RegisterDefaultStyleForType(typeof(Controls.RadioButton), Uno.UI.GlobalStaticResources.XamlDefaultRadioButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBarSeparator), Uno.UI.GlobalStaticResources.XamlAppBarSeparator);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBarButton), Uno.UI.GlobalStaticResources.XamlAppBarButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBarToggleButton), Uno.UI.GlobalStaticResources.XamlAppBarToggleButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.Frame), Uno.UI.GlobalStaticResources.XamlDefaultFrame);
				Style.RegisterDefaultStyleForType(typeof(Controls.ProgressBar), Uno.UI.GlobalStaticResources.XamlDefaultProgressBar);
				Style.RegisterDefaultStyleForType(typeof(Controls.Slider), Uno.UI.GlobalStaticResources.XamlDefaultSlider);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBar), Uno.UI.GlobalStaticResources.XamlCommandBar);
#endif
			}
		}

		static partial void InitStyles();
    }
}
