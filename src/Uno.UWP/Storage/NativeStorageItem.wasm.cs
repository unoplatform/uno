#nullable enable

using System;
using System.Linq;

namespace Uno.Storage
{
	public class NativeStorageItem
    {
		private const string GuidSplit = ";";

		[Preserve]
		public static string GenerateGuids(int count) => string.Join(GuidSplit, Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()));
    }
}
