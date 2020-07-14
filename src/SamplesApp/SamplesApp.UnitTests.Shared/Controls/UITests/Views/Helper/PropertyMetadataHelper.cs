#if NETFX_CORE
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#elif XAMARIN || NETSTANDARD2_0
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#else
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;
#endif

namespace Uno.UI.Samples.Helper
{
	public class PropertyMetadataHelper
	{
		public PropertyMetadataHelper(PropertyChangedCallback propertyChangedCallback = null, object defaultValue = null)
		{
			DefaultValue = defaultValue;
			Callback = propertyChangedCallback;
		}

		public static implicit operator PropertyMetadata(PropertyMetadataHelper value)
		{
			if (value.Callback == null)
			{
				return new FrameworkPropertyMetadata(value.DefaultValue);
			}
			else
			{
				return new FrameworkPropertyMetadata(value.DefaultValue, value.Callback);
			}
		}

		public object DefaultValue { get; set; }

		public PropertyChangedCallback Callback { get; set; }
	}
}