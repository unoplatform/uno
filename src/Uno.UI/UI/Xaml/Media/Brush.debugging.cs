#if DEBUG
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Media
{
	partial class Brush
	{
		/// <summary>
		/// Debugging method to get the resource key associated with this resource, if it came from a <see cref="ResourceDictionary"/>.
		/// </summary>
		/// <remarks>Note: The DEBUG_SET_RESOURCE_SOURCE symbol must be set in <see cref="ResourceDictionary"/> for this to return a value.</remarks>
		public string GetResourceName()
		{
			var source = ResourceDictionary.GetResourceSource(this);

			return source?.ResourceKey.Key ?? "No associated key found. Make sure you uncommented '//#define DEBUG_SET_RESOURCE_SOURCE' in ResourceDictionary.cs";

		}

		public ResourceDictionary? GetContainingResourceDictionary()
		{
			var source = ResourceDictionary.GetResourceSource(this);

			return source?.ContainingDictionary;
		}
	}
}

#endif
