using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;

internal class XBind_ResourceDictionary_Control : Control
{
	/// <summary>
	/// Creates a new instance of the <see cref="ProjectTemplate_xBind"/> class.
	/// </summary>
	public XBind_ResourceDictionary_Control()
	{
		this.DefaultStyleKey = typeof(XBind_ResourceDictionary_Control);

		// Allows directly using this control as the x:DataType in the template.
		this.DataContext = this;
	}

	public bool ElementLoadedInvoked { get; private set; }

	public void Element_Loaded(object sender, RoutedEventArgs args)
	{
		ElementLoadedInvoked = true;
	}
}
