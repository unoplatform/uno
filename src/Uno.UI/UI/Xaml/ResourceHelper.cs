using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Converters;
using Microsoft.UI.Xaml.Data;
using Uno.UI;
using Windows.ApplicationModel.Resources;

namespace Microsoft.UI.Xaml
{
	public static class ResourceHelper
	{
		static ResourceHelper()
		{
			ResourceLoader.GetStringInternal = key => ResourcesService.Get(key);
		}

		/// <summary>
		/// Provides a global registry, similar to the Application.Current.Resources in WinRT.
		/// </summary>
		public static Uno.Presentation.Resources.IResourceRegistry Registry
		{
			get; set;
		}

		/// <summary>
		/// Provides a global resource service for localization in Android and iOS
		/// </summary>
		public static IResourcesService ResourcesService
		{
			get; set;
		}

		/// <summary>
		/// Gets a global resource.
		/// </summary>
		/// <param name="resourceName"></param>
		/// <returns></returns>
		public static object FindResource(string resourceName)
		{
			return Registry.FindResource(resourceName);
		}

		/// <summary>
		/// Use to get resource for XamlFileGenerator in Android and iOS
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string FindResourceString(string name)
		{
			return ResourcesService.Get(name);
		}

		/// <summary>
		/// Tries to find a Converter using its static resource name.
		/// </summary>
		/// <param name="converterName"></param>
		/// <returns>The converter, or a NullConverter instance.</returns>
		public static IValueConverter FindConverter(string converterName)
		{
			// Get instead of Find as we want to reproduce the Jupiter behavior : is the resource is missing we get an exception.
			var converter = Registry.FindResource(converterName) as IValueConverter;

			if (converter == null)
			{
				Registry.Log().ErrorFormat("Resource [{0}] does not implement IValueConverter.", converterName);

				// Reproduce the beahvior on Jupiter : if the resource does not implements IValueConverter,
				// nothing appear in the view.
				converter = new NullConverter();
			}

			return converter;
		}
	}
}
