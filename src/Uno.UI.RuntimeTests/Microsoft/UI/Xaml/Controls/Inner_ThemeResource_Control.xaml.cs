using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class Inner_ThemeResource_Control : Control
	{
		public Border ThemeBoundBorder { get; private set; }
		public Inner_ThemeResource_Control()
		{
			DefaultStyleKey = typeof(Inner_ThemeResource_Control);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			ThemeBoundBorder = GetTemplateChild("ThemeBoundBorder") as Border;
		}
	}
}
