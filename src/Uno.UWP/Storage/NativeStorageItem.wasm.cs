#nullable enable

using System;
using System.Linq;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#endif

namespace Uno.Storage
{
	public partial class NativeStorageItem
	{
		private const string GuidSplit = ";";

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static string GenerateGuids(int count) => string.Join(GuidSplit, Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()));
	}
}
