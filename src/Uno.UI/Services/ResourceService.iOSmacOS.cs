using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Foundation;
using CoreGraphics;
using Uno.Extensions;

namespace Uno.UI.Services
{
	public class ResourcesService : IResourcesService
	{
		private const string KeyNotFoundValue = "__KeyNotFoundValue__";
		private readonly NSBundle[] _resourceBundles;

		public ResourcesService(IEnumerable<NSBundle> resourceBundles)
		{
			_resourceBundles = resourceBundles.ToArray();
		}

		public string Get(string id)
		{
			return _resourceBundles
				.Select(b => GetLocalizedStringOrDefault(id, b))
				.FirstOrDefault() ?? "";
		}
		
		private string GetLocalizedStringOrDefault(string id, NSBundle bundle)
		{
			// From Apple doc : 
			// If key is nil and value is nil, returns an empty string.
			// If key is nil and value is non-nil, returns value.
			// If key is not found and value is nil or an empty string, returns key.
			// If key is not found and value is non-nil and not empty, return value.
#pragma warning disable CS0618 // Type or member is obsolete
			var localizedString = bundle.LocalizedString(id, KeyNotFoundValue, null);
#pragma warning restore CS0618 // Type or member is obsolete

			if (localizedString == KeyNotFoundValue)
			{
				return null;
			}

			return localizedString;
		}
	}
}
