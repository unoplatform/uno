using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_ApplicationModel_Resources_ResourceLoader
{
	[Sample("Resources", Name = "ResourceLoader_FilenameLayout", Description = "Demonstrates the MRT spec filename-suffix layout: Resources.resw + Resources.language-cs-CZ.resw siblings in one folder.")]
	public sealed partial class ResourceLoader_FilenameLayout : UserControl
	{
		public ResourceLoader_FilenameLayout()
		{
			this.InitializeComponent();
		}
	}
}
