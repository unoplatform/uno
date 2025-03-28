#if WINAPPSDK
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#elif XAMARIN || UNO_REFERENCE_API
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
				return new PropertyMetadata(value.DefaultValue);
			}
			else
			{
				return new PropertyMetadata(value.DefaultValue, value.Callback);
			}
		}

		public object DefaultValue { get; set; }

		public PropertyChangedCallback Callback { get; set; }
	}
}
