#nullable enable

namespace Windows.Storage
{
	public partial class StorageProvider
	{
		internal StorageProvider(string id, string displayName)
		{
			Id = id;
			DisplayName = displayName;
		}

		public string Id { get; }

		public string DisplayName { get; }
	}
}
