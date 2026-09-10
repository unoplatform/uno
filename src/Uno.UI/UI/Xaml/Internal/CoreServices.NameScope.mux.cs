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

			// SetNamedObject on ourselves should only register a weakref to avoid a circular reference.
			// TODO Uno: WinUI holds every other entry strongly. Until names are unregistered on Leave,
			// a strong entry would keep a removed child alive for as long as its owner, so descendants
			// stay weak too (spec 058, D7).
			NameScopeRoot.GetTable(namescopeOwner, NameScopeType.StandardNameScope)!
				.RegisterName(name, WeakReferencePool.RentWeakReference(this, obj));
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
}
