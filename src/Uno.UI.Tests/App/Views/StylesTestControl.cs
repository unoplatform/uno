using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.App.Views
{
	public partial class StylesTestControl : Control
	{
		public MyControl InnerMyControl { get; private set; }

		public string Rugosity
		{
			get { return (string)GetValue(RugosityProperty); }
			set { SetValue(RugosityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Rugosity.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty RugosityProperty =
			DependencyProperty.Register("Rugosity", typeof(string), typeof(StylesTestControl), new PropertyMetadata("FromDefault"));

		protected override void OnApplyTemplate()
		{
			InnerMyControl = GetTemplateChild("MyControl") as MyControl;
		}
	}

	public partial class StylesTestButton : Button
	{
		public object ExposedKey => DefaultStyleKey;

		public string Animosity
		{
			get { return (string)GetValue(AnimosityProperty); }
			set { SetValue(AnimosityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Animosity.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AnimosityProperty =
			DependencyProperty.Register("Animosity", typeof(string), typeof(StylesTestButton), new PropertyMetadata("FromDefault"));


	}

	public partial class StylesTestButtonCustomKey : Button
	{
		public StylesTestButtonCustomKey()
		{
			DefaultStyleKey = typeof(StylesTestButtonCustomKey);
		}
	}

	public partial class StylesTestRadioButton : RadioButton
	{
		public object ExposedKey => DefaultStyleKey;
	}
}
