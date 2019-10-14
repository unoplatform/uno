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
				_initialized = true;

				InitStyles();

				InitializeDefaultStyles();
			}
		}

		private static void InitializeDefaultStyles()
		{
			if (Uno.UI.FeatureConfiguration.Style.UseUWPDefaultStyles)
			{
#if !NET461
				Style.RegisterDefaultStyleForType(typeof(Controls.Button), Uno.UI.GlobalStaticResources.XamlDefaultButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.TextBox), Uno.UI.GlobalStaticResources.XamlDefaultTextBox);
				Style.RegisterDefaultStyleForType(typeof(Controls.PasswordBox), Uno.UI.GlobalStaticResources.XamlDefaultPasswordBox);
				Style.RegisterDefaultStyleForType(typeof(Controls.CheckBox), Uno.UI.GlobalStaticResources.XamlDefaultCheckBox);
				Style.RegisterDefaultStyleForType(typeof(Controls.RadioButton), Uno.UI.GlobalStaticResources.XamlDefaultRadioButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBarSeparator), Uno.UI.GlobalStaticResources.XamlDefaultAppBarSeparator);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBarButton), Uno.UI.GlobalStaticResources.XamlDefaultAppBarButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBarToggleButton), Uno.UI.GlobalStaticResources.XamlDefaultAppBarToggleButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.Frame), Uno.UI.GlobalStaticResources.XamlDefaultFrame);
				Style.RegisterDefaultStyleForType(typeof(Controls.ProgressBar), Uno.UI.GlobalStaticResources.XamlDefaultProgressBar);
				Style.RegisterDefaultStyleForType(typeof(Controls.Slider), Uno.UI.GlobalStaticResources.XamlDefaultSlider);
				Style.RegisterDefaultStyleForType(typeof(Controls.Primitives.ToggleButton), Uno.UI.GlobalStaticResources.XamlDefaultToggleButton);
				Style.RegisterDefaultStyleForType(typeof(Controls.ToggleSwitch), Uno.UI.GlobalStaticResources.XamlDefaultToggleSwitch);
				Style.RegisterDefaultStyleForType(typeof(Controls.Pivot), Uno.UI.GlobalStaticResources.XamlDefaultPivot);
				Style.RegisterDefaultStyleForType(typeof(Controls.MenuBar), Uno.UI.GlobalStaticResources.XamlDefaultMenuBar);
				Style.RegisterDefaultStyleForType(typeof(Controls.AppBar), Uno.UI.GlobalStaticResources.XamlDefaultCommandBar);
#endif
			}
		}

		static partial void InitStyles();
	}
}
