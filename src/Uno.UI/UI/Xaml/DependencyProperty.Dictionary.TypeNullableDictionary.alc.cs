#nullable enable

using System;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private partial class TypeNullableDictionary
		{
			/// <summary>
			/// Removes entries whose key <see cref="Type"/> belongs to a non-default
			/// (collectible) <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, so the
			/// app-lifetime dictionary does not pin an unloaded secondary app's types.
			/// </summary>
			internal void RemoveNonDefaultAlcEntries()
			{
				var defaultAlc = global::System.Runtime.Loader.AssemblyLoadContext.Default;
				var keysToRemove = new global::System.Collections.Generic.List<Type>();

				foreach (Type key in _entries.Keys)
				{
					// Type.IsCollectible also catches generic instantiations over collectible
					// type arguments, whose declaring assembly is a shared (default-ALC) one.
					var alc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(key.Assembly);
					if (key.IsCollectible || (alc is not null && alc != defaultAlc))
					{
						keysToRemove.Add(key);
					}
				}

				foreach (var key in keysToRemove)
				{
					_entries.Remove(key);
				}
			}
		}
	}
}
