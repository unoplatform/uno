#nullable disable

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Derived from https://github.com/dotnet/runtime/blob/57bfe474518ab5b7cfe6bf7424a79ce3af9d6657/src/libraries/System.Collections.NonGeneric/tests/Helpers.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Uno.Collections;

namespace Uno.UI.Tests.Collections;

internal static class Helpers
{
	public static void PerformActionOnAllHashtableWrappers(HashtableEx hashtable, Action<HashtableEx> action)
	{
		// Synchronized returns a slightly different version of HashtableEx
		HashtableEx[] hashtableTypes =
		{
				(HashtableEx)hashtable.Clone(),
			};

		foreach (HashtableEx hashtableType in hashtableTypes)
		{
			action(hashtableType);
		}
	}

	public static HashtableEx CreateIntHashtable(int count, int start = 0)
	{
		var hashtable = new HashtableEx();

		for (int i = start; i < start + count; i++)
		{
			hashtable.Add(i, i);
		}

		return hashtable;
	}

	public static HashtableEx CreateStringHashtable(int count, int start = 0)
	{
		var hashtable = new HashtableEx();

		for (int i = start; i < start + count; i++)
		{
			string key = "Key_" + i;
			string value = "Value_" + i;

			hashtable.Add(key, value);
		}

		return hashtable;
	}
}
