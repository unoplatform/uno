using System;
using Microsoft.VisualBasic.FileIO;
using System.Net;

namespace Windows.Storage.AccessCache;

/// <summary>
/// Describes the behavior to use when the app accesses an item in a list.
/// This enumeration supports a bitwise combination of its member values.
/// </summary>
[FlagsAttribute]
public enum AccessCacheOptions : uint
{
	/// <summary>
	/// Default.
	/// When the app accesses the item, the app retrieves the most current version 
	/// of the item from any available location and, if necessary, the user can 
	/// enter additional information.
	/// </summary>
	None = 0,

	/// <summary>
	/// When the app accesses the item, the user is prevented from entering information.
	/// For example, if the app accesses a file that is stored using this option 
	/// and the file normally triggers a request for the user to enter credentials, the request is suppressed.
	/// </summary>
	DisallowUserInput = 1,

	/// <summary>
	/// When the app accesses the item, it is retrieved from a fast location like the local file system.
	/// For example, if the app accesses a file that is stored using this option and a version of the file 
	/// is only available remotely, the file will not be accessed.
	/// </summary>
	FastLocationsOnly = 2,

	/// <summary>
	/// When the app accesses the item, the app retrieves a cached, read-only version of the file. 
	/// This version of the file might not be the most recent.
	/// </summary>
	UseReadOnlyCachedCopy = 4,

	/// <summary>
	/// When the app accesses the item in the StorageItemMostRecentlyUsedList, Windows preserves the item's 
	/// current position in the most recently used (MRU) and does not update the access time of the item.
	/// </summary>
	SuppressAccessTimeUpdate = 8,
}
