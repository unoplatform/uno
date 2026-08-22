#nullable enable

using System;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// Marshalling helpers for the batched DOM style/attribute/property interop entry points.
	/// </summary>
	internal static class HtmlPropertyPairs
	{
		/// <summary>
		/// Flattens name/value pairs into the <c>[name0, value0, name1, value1, …]</c> layout
		/// expected by the <c>*NativeFast</c> JS entry points.
		/// </summary>
		/// <remarks>
		/// Declared as <c>params ReadOnlySpan</c> so that inline call sites get a stack allocated
		/// argument buffer, leaving the returned array as the only allocation of the batch.
		/// </remarks>
		internal static string[] Flatten(params ReadOnlySpan<(string name, string value)> pairs)
		{
			var flat = new string[pairs.Length * 2];

			for (var i = 0; i < pairs.Length; i++)
			{
				flat[i * 2 + 0] = pairs[i].name;
				flat[i * 2 + 1] = pairs[i].value;
			}

			return flat;
		}
	}
}
