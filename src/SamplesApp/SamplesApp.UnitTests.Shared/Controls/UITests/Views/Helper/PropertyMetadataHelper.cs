#if WINAPPSDK
using Windows.ApplicationModel.Store;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
#elif XAMARIN || UNO_REFERENCE_API
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
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
