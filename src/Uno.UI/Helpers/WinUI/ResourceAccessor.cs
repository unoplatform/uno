// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the ResourceAccessor.cpp file from WinUI controls.
//

using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Helpers.WinUI
{
	internal partial class ResourceAccessor
	{
#if !IS_UNO
		public const string MUXCONTROLS_PACKAGE_NAME = "Microsoft" + /* UWP don't rename */ ".UI.Xaml.3.0";

		private static ResourceMap s_resourceMap = GetPackageResourceMap();
		private static ResourceContext s_resourceContext = ResourceContext.GetForViewIndependentUse();

		private static ResourceMap GetPackageResourceMap()
		{
			if (SharedHelpers.IsInFrameworkPackage())
			{
				string packageName = MUXCONTROLS_PACKAGE_NAME;
				if (ResourceManager.Current.AllResourceMaps.TryGetValue(packageName, out var value))
				{
					return value;
				}

				return null;
			}
			else
			{
				return ResourceManager.Current.MainResourceMap;
			}
		}

		private static ResourceMap GetResourceMap()
		{
			return s_resourceMap.GetSubtree(c_resourceLoc);
		}
#else
		public const string MUXCONTROLS_PACKAGE_NAME = "Microsoft" + /* UWP don't rename */ ".UI.Xaml.3.0";
		private const string c_resourceLoc = "Uno.UI/Resources";
#endif

		public static string GetLocalizedStringResource(string resourceName)
		{
#if !IS_UNO
			return s_resourceMap.GetValue(resourceName, s_resourceContext).ToString();
#else
			return Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse(c_resourceLoc).GetString(resourceName);
#endif
		}

		public static LoadedImageSurface GetImageSurface(string assetName, Size imageSize)
		{
			Uri getImageUri()
			{
				if (SharedHelpers.IsInFrameworkPackage())
				{
					return new Uri("ms-resource://" + MUXCONTROLS_PACKAGE_NAME + "/Files/Microsoft" + /* UWP don't rename */ ".UI.Xaml/Assets/" + assetName + ".png");
				}
				else
				{
					return new Uri("ms-resource:///Files/Microsoft" + /* UWP don't rename */ ".UI.Xaml/Assets/" + assetName + ".png");
				}
			}

			return LoadedImageSurface.StartLoadFromUri(getImageUri(), imageSize);
		}

		public static object ResourceLookup(Control control, object key)
		{
			return control.Resources.HasKey(key) ? control.Resources.Lookup(key) : Application.Current.Resources.Lookup(key);
			//WinUI uses TryLookup for Application Resources, but Lookup seems to have the same semantics
		}
	}
}
