#nullable enable

using System;
using Windows.ApplicationModel.Resources;

namespace Windows.Storage
{
	public partial class StorageProvider
	{
		private const string StorageProviderResourcesViewName = "Uno.UI/Resources";

		private static readonly Lazy<ResourceLoader> _resourceLoader = new Lazy<ResourceLoader>(() => ResourceLoader.GetForViewIndependentUse(StorageProviderResourcesViewName));

		private readonly string _displayNameResourceKey;

		internal StorageProvider(string id, string displayNameResourceKey)
		{
			Id = id;
			_displayNameResourceKey = displayNameResourceKey;
		}

		public string Id { get; }

		public string DisplayName => _resourceLoader.Value.GetString(_displayNameResourceKey);
	}
}
