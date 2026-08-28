using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.App.Xaml
{
	public sealed partial class ResourceDictionarySourceLocation : Page
	{
		public ResourceDictionarySourceLocation()
		{
			this.InitializeComponent();
		}
	}

	/// <summary>
	/// A dictionary defined in code, as the theme dictionaries of a library are: it has no
	/// InitializeComponent of its own to stamp its source location.
	/// </summary>
	public class SourceLocation_CodeDictionary : ResourceDictionary
	{
	}
}
