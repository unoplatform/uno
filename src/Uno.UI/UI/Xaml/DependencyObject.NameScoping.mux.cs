// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CDependencyObject.h (NameScoping region) / depends.cpp, commit fc2f82117
//
// The namescope-owner model of CDependencyObject: the per-object bitfield flags and the walk
// that finds the object owning the namescope table a name lives in. The tables themselves are
// CoreServices.NameScopeRoot; registration on Enter/Leave is ported separately.

#nullable enable

using System;
using System.Diagnostics;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public partial class DependencyObject
{
	// MUX Reference: CDependencyObject.h:247-326 — the namescope subset of m_bitFields.
	[Flags]
	private enum NameScopeFlags : byte
	{
		None = 0,
		IsStandardNameScopeOwner = 1 << 0,
		IsStandardNameScopeMember = 1 << 1,

		// Indicates that this element, while it may have its own namescope
		// (meaning that it could have fIsPermanentNamescopeOwner set to true),
		// should have its name registered in its parent's namescope.
		ShouldRegisterInParentNamescope = 1 << 2,

		// This DO is part of a Template Namescope.  This flag is set by the Parser on any DO
		// that is created by the parser when a ControlTemplate is being Loaded/Applied.
		IsTemplateNamescopeMember = 1 << 3,

		// This flag will be set when this object is on parser's stack and parser owns its parent relationship.
		// It will be reset when the parser pops the object off its stack.
		ParserOwnsParent = 1 << 4,

		// Is name a usage name or definition name?
		HasUsageName = 1 << 5,
	}

	private NameScopeFlags _nameScopeFlags;

	private bool GetNameScopeFlag(NameScopeFlags flag) => (_nameScopeFlags & flag) != 0;

	private void SetNameScopeFlag(NameScopeFlags flag, bool value)
		=> _nameScopeFlags = value ? _nameScopeFlags | flag : _nameScopeFlags & ~flag;

	internal bool IsStandardNameScopeOwner
	{
		get => GetNameScopeFlag(NameScopeFlags.IsStandardNameScopeOwner);
		set => SetNameScopeFlag(NameScopeFlags.IsStandardNameScopeOwner, value);
	}

	internal bool IsStandardNameScopeMember
	{
		get => GetNameScopeFlag(NameScopeFlags.IsStandardNameScopeMember);
		set => SetNameScopeFlag(NameScopeFlags.IsStandardNameScopeMember, value);
	}

	internal bool ShouldRegisterInParentNamescope
	{
		get => GetNameScopeFlag(NameScopeFlags.ShouldRegisterInParentNamescope);
		set => SetNameScopeFlag(NameScopeFlags.ShouldRegisterInParentNamescope, value);
	}

	internal bool IsTemplateNamescopeMember
	{
		get => GetNameScopeFlag(NameScopeFlags.IsTemplateNamescopeMember);
		set => SetNameScopeFlag(NameScopeFlags.IsTemplateNamescopeMember, value);
	}

	internal bool HasUsageName
	{
		get => GetNameScopeFlag(NameScopeFlags.HasUsageName);
		set
		{
			// Usage name is registered in parent namescope
			Debug.Assert(ShouldRegisterInParentNamescope);
			SetNameScopeFlag(NameScopeFlags.HasUsageName, value);
		}
	}

	internal bool ParserOwnsParent => GetNameScopeFlag(NameScopeFlags.ParserOwnsParent);

	// Sets a bit flag indicating that parser owns this object's parent.
	internal void SetParserParentLock() => SetNameScopeFlag(NameScopeFlags.ParserOwnsParent, true);

	// Resets the parent lock flag.
	internal void ResetParserParentLock() => SetNameScopeFlag(NameScopeFlags.ParserOwnsParent, false);

	// DOCollection calls here to determine if should use fSkipNameRegistration on the Enter walk.
	// We want to skip when in the parser, just as we do in CDependencyObject::SetName, in order to let the parser do the
	// name registration.
	// We also want to skip when the owner is a namescope owner(a UserControl or anything else with an x:Class on it), in
	// order to maintain compatibility with previous behavior(since Win8 and likely Silverlight).
	internal virtual bool SkipNameRegistrationForChildren => ParserOwnsParent && !IsStandardNameScopeOwner;

	internal bool ShouldParticipateInParentNameScope => !ShouldRegisterInParentNamescope || HasUsageName;

	internal void MarkAsPossiblyHavingDefinitionName() => ShouldRegisterInParentNamescope = true;

	// TemplateNameScoes lookup redirection is handled entirely in CCoreServices- this tree walk is only concerned with
	// standard runtime-built NameScope entries.
	internal DependencyObject? GetStandardNameScopeOwner() => GetStandardNameScopeOwnerInternal(null);

	internal virtual DependencyObject? GetStandardNameScopeParent() => this.GetParentInternal(false);

	/// <summary>
	/// Gets the DO that has a Namescope store associated with it.
	/// </summary>
	private protected DependencyObject? GetStandardNameScopeOwnerInternal(DependencyObject? firstOwner)
	{
		DependencyObject? namescopeOwner;
		if (IsStandardNameScopeOwner)
		{
			namescopeOwner = this;
		}
		else if (IsActive && !IsStandardNameScopeMember)
		{
			// get the real root, not the "Dummy Root"
			namescopeOwner = GetPublicRootVisual();
		}
		else
		{
			namescopeOwner = GetStandardNameScopeParent();
			if (namescopeOwner is not null)
			{
				// In some rare cases where we build a cyclic tree outside the live tree we could be
				// stuck in an endless loop here, so prevent that. See bug# 3106
				if (namescopeOwner == firstOwner)
				{
					namescopeOwner = null;
				}
				else
				{
					// get the namescope owner of the namescope parent
					namescopeOwner = namescopeOwner.GetStandardNameScopeOwnerInternal(firstOwner ?? namescopeOwner);
				}
			}
		}

		return namescopeOwner;
	}

	internal DependencyObject? GetPublicRootVisual()
	{
		if (VisualTree.GetForElement(this) is { } visualTree)
		{
			return visualTree.PublicRootVisual;
		}

		return this.GetContext().VisualRoot;
	}
}
