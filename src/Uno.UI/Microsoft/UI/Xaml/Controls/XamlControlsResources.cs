using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno;
using Uno.UI.Xaml;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class XamlControlsResources : ResourceDictionary
	{
		public XamlControlsResources()
		{
			UpdateSource();
		}

		private void UpdateSource() => Source = new Uri(XamlFilePathHelper.AppXIdentifier + XamlFilePathHelper.WinUIThemeResourceURL);

		[NotImplemented]
		public static void EnsureRevealLights(UIElement element) { }


		[NotImplemented]
		public bool UseCompactResources
		{
			get => (bool)GetValue(UseCompactResourcesProperty);
			set => SetValue(UseCompactResourcesProperty, value);
		}

		[NotImplemented]
		public static readonly DependencyProperty UseCompactResourcesProperty =
			DependencyProperty.Register("UseCompactResources", typeof(bool), typeof(XamlControlsResources), new PropertyMetadata(false));


	}
}
