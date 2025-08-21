// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ElementManager.cpp, commit 864c068

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	internal static partial class CollectionExtensions
	{
		public static bool TryGetElementAt<T>(this IList<T> collection, int index, out T element)
			where T : class
		{
			if (index < collection.Count)
			{
				element = collection[index];
				return element != default; // TODO UNO
			}
			else
			{
				element = default;
				return false;
			}
		}

		public static void AddOrInsert<T>(this IList<T> collection, int index, T element)
		{
			if (index >= collection.Count)
			{
				collection.Add(element);
			}
			else
			{
				collection.Insert(index, element);
			}
		}
	}

}
