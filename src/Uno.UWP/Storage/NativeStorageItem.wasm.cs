#nullable enable

using System;

namespace Uno.Storage
{
	public class NativeStorageItem
    {
		[Preserve]
		public static string GenerateGuid() => Guid.NewGuid().ToString();
    }
}
