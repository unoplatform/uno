#nullable enable

using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;

namespace Uno.Storage
{
	public partial class NativeStorageItem
	{
		private const string GuidSplit = ";";

		[JSExport]
		public static string GenerateGuids(int count) => string.Join(GuidSplit, Enumerable.Range(0, count).Select(i => Guid.NewGuid().ToString()));
	}
}
