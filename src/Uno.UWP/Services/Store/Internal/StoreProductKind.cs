namespace Uno.Services.Store.Internal
{
	/// <summary>
	/// Helper enum for store product kinds in Uno,
	/// UWP does not have equivalent enum
	/// </summary>
	internal enum StoreProductKind
    {
		Application,
		Game,
		Consumable,
		UnmanagedConsumable,
		Durable
	}
}
