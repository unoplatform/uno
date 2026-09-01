// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference xcpcore_namescope.cpp, commit fc2f82117

#nullable enable

using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core.NameScoping;

namespace Uno.UI.Xaml.Core;

internal partial class CoreServices
{
	internal void SetNamedObject(
		string name,
		DependencyObject namescopeOwner,
		NameScopeType nameScopeType,
		DependencyObject obj)
	{
		ArgumentNullException.ThrowIfNull(namescopeOwner);
		Debug.Assert(nameScopeType == NameScopeType.TemplateNameScope || namescopeOwner.IsStandardNameScopeOwner);

		if (nameScopeType == NameScopeType.TemplateNameScope)
		{
			throw new NotSupportedException("Template namescopes are not implemented yet.");
		}
		else
		{
			// Is this needed / could it be just an ASSERT?
			if (!namescopeOwner.IsStandardNameScopeOwner)
			{
				throw new InvalidOperationException($"{namescopeOwner} is not a namescope owner.");
			}

			NameScopeRoot.EnsureNameScope(namescopeOwner, NameScopeType.StandardNameScope);

			var table = NameScopeRoot.GetTable(namescopeOwner, NameScopeType.StandardNameScope)!;

			// SetNamedObject on ourselves should only register a weakref to avoid a circular reference.
			if (ReferenceEquals(namescopeOwner, obj))
			{
				table.RegisterName(name, WeakReferencePool.RentWeakReference(this, obj));
			}
			else
			{
				table.RegisterName(name, obj);
			}
		}
	}

	internal DependencyObject? GetNamedObject(
		string name,
		DependencyObject? namescopeOwner,
		NameScopeType nameScopeType)
	{
		if (namescopeOwner is null)
		{
			return null;
		}

		Debug.Assert(nameScopeType == NameScopeType.TemplateNameScope || namescopeOwner.IsStandardNameScopeOwner);

		// Deferred (x:Load) entries realize inside the table lookup, so no DeferredElement branch is needed here.
		return NameScopeRoot.GetNamedObjectIfExists(name, namescopeOwner, nameScopeType);
	}

	/// <summary>
	/// Removes a name registration, but only if it still refers to <paramref name="originalEntry"/>.
	/// MUX Reference: CCoreServices::ClearNamedObject (xcpcore_namescope.cpp:109-142).
	/// </summary>
	internal void ClearNamedObject(string name, DependencyObject? namescopeOwner, DependencyObject originalEntry)
	{
		if (namescopeOwner is null || !NameScopeRoot.HasStandardNameScopeTable(namescopeOwner))
		{
			return;
		}

		NameScopeRoot.ClearNamedObjectIfExists(name, namescopeOwner, NameScopeType.StandardNameScope, originalEntry);
	}

	internal bool HasRegisteredNames(DependencyObject namescopeOwner)
		=> NameScopeRoot.HasStandardNameScopeTable(namescopeOwner);
}
