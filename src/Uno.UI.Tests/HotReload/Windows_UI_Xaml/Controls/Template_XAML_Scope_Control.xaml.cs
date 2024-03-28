using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Tests.App.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.HotReload.Windows_UI_Xaml.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Template_XAML_Scope_Control : UserControl
	{
		public ResourceTestControl InlineTemplateControl => inlineTemplateControl;
		public ResourceTestControl TemplateFromResourceControl => templateFromResourceControl;

		public Template_XAML_Scope_Control()
		{
			this.InitializeComponent();
		}
	}
}
