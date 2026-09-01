// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TemplateNamescope.h/.cpp (NameScopeHelper), commit fc2f82117

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Markup;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core.NameScoping;

namespace Microsoft.UI.Xaml;

/// <summary>
/// The parser's handle on a namescope: it remembers the owner and forwards registrations to the
/// owner's table in CoreServices.NameScopeRoot. This is WinUI's NameScopeHelper; generated
/// InitializeComponent/template code plays the role of the parser.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class NameScope : INameScope
{
	// Owner of the namescope, may be null until EnsureNamescopeOwner
	private ManagedWeakReference? _ownerRef;

	// WinUI's parser knows the root before it registers any child name. Generated template code
	// only learns its root after the body has run, so names registered until then wait here.
	private Dictionary<string, ManagedWeakReference>? _pendingNames;

	public NameScope()
	{
	}

	public DependencyObject? Owner
	{
		get => _ownerRef?.Target as DependencyObject;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			EnsureNamescopeOwner(value);
		}
	}

	// Receive a namescope owner to use when talking to the core, if we don't already
	// have one.  If we didn't have one, then create the name scope.
	private void EnsureNamescopeOwner(DependencyObject nameScopeOwner)
	{
		// Ignore this if we already have an owner... the typical TemplateContent
		// scneario.
		if (Owner is not null)
		{
			return;
		}

		if (_ownerRef is not null)
		{
			WeakReferencePool.ReturnWeakReference(this, _ownerRef);
		}

		_ownerRef = WeakReferencePool.RentWeakReference(this, nameScopeOwner);

		nameScopeOwner.GetContext().NameScopeRoot.EnsureNameScope(nameScopeOwner, NameScopeType.StandardNameScope);
		nameScopeOwner.IsStandardNameScopeOwner = true;
		nameScopeOwner.IsStandardNameScopeMember = true;

		if (_pendingNames is { } pending)
		{
			_pendingNames = null;
			foreach (var (name, reference) in pending)
			{
				if (reference.Target is DependencyObject element)
				{
					nameScopeOwner.GetContext().SetNamedObject(name, nameScopeOwner, NameScopeType.StandardNameScope, element);
				}

				WeakReferencePool.ReturnWeakReference(this, reference);
			}
		}
	}

	/// <summary>
	/// Flags the owner as possibly carrying an x:Class definition name (as opposed to a usage name
	/// given by whoever instantiates it). Called by generated code once the root's own properties
	/// have been applied, because setting Name marks it as a usage name.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void MarkOwnerAsPossiblyHavingDefinitionName() => Owner?.MarkAsPossiblyHavingDefinitionName();

	public object? FindName(string name)
	{
		if (Owner is { } owner)
		{
			return owner.GetContext().GetNamedObject(name, owner, NameScopeType.StandardNameScope);
		}

		return _pendingNames is not null && _pendingNames.TryGetValue(name, out var reference)
			? reference.Target
			: null;
	}

	public void RegisterName(string name, object scopedElement)
	{
		if (scopedElement is not DependencyObject element)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Cannot register the name [{name}]: {scopedElement?.GetType()} is not a DependencyObject.");
			}

			return;
		}

		if (Owner is { } owner)
		{
			var context = owner.GetContext();
			WarnIfDuplicate(name, element, context.NameScopeRoot.PeekNamedObjectIfExists(name, owner, NameScopeType.StandardNameScope));
			context.SetNamedObject(name, owner, NameScopeType.StandardNameScope, element);
			return;
		}

		_pendingNames ??= new Dictionary<string, ManagedWeakReference>(StringComparer.Ordinal);

		if (_pendingNames.TryGetValue(name, out var existing))
		{
			WarnIfDuplicate(name, element, existing.Target as DependencyObject);
			WeakReferencePool.ReturnWeakReference(this, existing);
		}

		_pendingNames[name] = WeakReferencePool.RentWeakReference(this, element);
	}

	private void WarnIfDuplicate(string name, DependencyObject element, DependencyObject? existing)
	{
		// Re-registering the same element under the same name is idempotent, not a duplicate.
		if (existing is not null && !ReferenceEquals(existing, element) && this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"The name [{name}] already exists in the current XAML scope");
		}
	}

	public void UnregisterName(string name)
	{
		if (Owner is { } owner)
		{
			owner.GetContext().NameScopeRoot.GetTable(owner, NameScopeType.StandardNameScope)?.TryRemove(name);
			return;
		}

		if (_pendingNames is not null && _pendingNames.Remove(name, out var reference))
		{
			WeakReferencePool.ReturnWeakReference(this, reference);
		}
	}

	#region NameScope attached property

	/// <summary>
	/// Provides the attached property set accessor for the NameScope attached property.
	/// </summary>
	/// <param name="dependencyObject">Object to change XAML namescope for.</param>
	/// <param name="value">The new XAML namescope, using an interface cast.</param>
	public static void SetNameScope(DependencyObject dependencyObject, INameScope value)
	{
		dependencyObject.SetValue(NameScopeProperty, value);
	}

	/// <summary>
	/// Provides the attached property get accessor for the NameScope attached property.
	/// </summary>
	/// <param name="dependencyObject">The object to get the XAML namescope from.</param>
	/// <returns>A XAML namescope, as an INameScope instance.</returns>
	public static INameScope? GetNameScope(DependencyObject dependencyObject)
	{
		return (INameScope?)dependencyObject.GetValue(NameScopeProperty);
	}

	/// <summary>
	/// Identifies the NameScope attached property.
	/// </summary>
	public static DependencyProperty NameScopeProperty
	{
		[DynamicDependency(nameof(GetNameScope))]
		[DynamicDependency(nameof(SetNameScope))]
		get;
	} = DependencyProperty.RegisterAttached(
			"NameScope",
			typeof(INameScope),
			typeof(NameScope),
			new FrameworkPropertyMetadata(
				null,
				// This property is inherited to ensure the NameScope is available to all children.
				// This differs from WPF's implementation, which doesn't seem to inherit this property,
				// but still appears to have the effect we want.
				FrameworkPropertyMetadataOptions.Inherits
			)
		);

	#endregion

	/// <summary>
	/// Search for a named element in all available namescopes, preferring scopes that are 'closest' in the hierarchy.
	/// </summary>
	internal static object? FindInNamescopes(DependencyObject? caller, string name)
	{
		var parent = caller;
		while (parent != null)
		{
			var scope = NameScope.GetNameScope(parent);
			var target = scope?.FindName(name);

			if (target != null)
			{
				return target;
			}

			var newParent = parent.GetParent() as DependencyObject;

			if (newParent is null && !(parent is UIElement))
			{
				// This case is about handling ElementName Bindings on non-UIElement
				// dependency objects (e.g. XAML Behaviors triggers). Those objects
				// cannot have a parent set, and in order to find ancestor scopes
				// (DataTemplate inside a DataTemplate) we need to find a known ancestor
				// through the NameScope owner.

				if (scope?.Owner is DependencyObject owner)
				{
					return FindInNamescopes(owner, name);
				}
			}

			parent = newParent;
		}

		return null;
	}
}
