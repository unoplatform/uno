#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace DirectUI;

internal enum PropertyPathStepDescriptorKind : byte
{
	None = 0,
	SourceAccess,
	PropertyAccess,
	IntIndexer,
	StringIndexer,
	DependencyProperty,
}

/// <summary>
/// A compact, tagged description of a single step of a property path.
/// </summary>
/// <remarks>
/// This is a value type so that parsing a path does not allocate one object per step. The payload is
/// a single object reference (a property/indexer name, or a <see cref="Microsoft.UI.Xaml.DependencyProperty"/>)
/// plus an integer index; which one is meaningful depends on <see cref="Kind"/>.
/// </remarks>
internal readonly struct PropertyPathStepDescriptor // src\dxaml\xcp\dxaml\lib\PropertyPathStepDescriptor.h
{
	private readonly object? _payload;
	private readonly int _index;

	private PropertyPathStepDescriptor(PropertyPathStepDescriptorKind kind, object? payload, int index)
	{
		Kind = kind;
		_payload = payload;
		_index = index;
	}

	public PropertyPathStepDescriptorKind Kind { get; }

	/// <summary>
	/// The property name of a <see cref="PropertyPathStepDescriptorKind.PropertyAccess"/> step, or the key of a
	/// <see cref="PropertyPathStepDescriptorKind.StringIndexer"/> step. <c>null</c> for any other kind.
	/// </summary>
	public string? Name => _payload as string;

	/// <summary>
	/// The index of a <see cref="PropertyPathStepDescriptorKind.IntIndexer"/> step, 0 for any other kind.
	/// </summary>
	public int Index => _index;

	/// <summary>
	/// The property of a <see cref="PropertyPathStepDescriptorKind.DependencyProperty"/> step, <c>null</c> for any other kind.
	/// </summary>
	public DependencyProperty? Property => _payload as DependencyProperty;

	public static PropertyPathStepDescriptor CreateSourceAccess()
		=> new(PropertyPathStepDescriptorKind.SourceAccess, null, 0);

	public static PropertyPathStepDescriptor CreatePropertyAccess(string szName)
		=> new(PropertyPathStepDescriptorKind.PropertyAccess, szName, 0);

	public static PropertyPathStepDescriptor CreateIntIndexer(int nIndex)
		=> new(PropertyPathStepDescriptorKind.IntIndexer, null, nIndex);

	public static PropertyPathStepDescriptor CreateStringIndexer(string szIndex)
		=> new(PropertyPathStepDescriptorKind.StringIndexer, szIndex, 0);

	public static PropertyPathStepDescriptor CreateDependencyProperty(DependencyProperty pDP)
		=> new(PropertyPathStepDescriptorKind.DependencyProperty, pDP, 0);

	// src\dxaml\xcp\dxaml\lib\PropertyPathStepDescriptor.cpp
	public PropertyPathStep CreateStep(
		PropertyPathListener pListener,
		bool fListenToChanges)
	{
		switch (Kind)
		{
			case PropertyPathStepDescriptorKind.SourceAccess:
				{
					var spStep = new SourceAccessPathStep();
					spStep.Initialize(pListener);

					return spStep;
				}

			case PropertyPathStepDescriptorKind.PropertyAccess:
				{
					var spStep = new PropertyAccessPathStep();
					spStep.Initialize(pListener, (string)_payload!, fListenToChanges);

					return spStep;
				}

			case PropertyPathStepDescriptorKind.IntIndexer:
				{
					var spStep = new IntIndexerPathStep();
					spStep.Initialize(pListener, _index, fListenToChanges);

					return spStep;
				}

			case PropertyPathStepDescriptorKind.StringIndexer:
				{
					var spStep = new StringIndexerPathStep();
					spStep.Initialize(pListener, (string)_payload!, fListenToChanges);

					return spStep;
				}

			case PropertyPathStepDescriptorKind.DependencyProperty:
				{
					var spStep = new PropertyAccessPathStep();
					spStep.Initialize(pListener, (DependencyProperty)_payload!, fListenToChanges);

					return spStep;
				}

			default:
				throw new InvalidOperationException($"Unsupported property path step descriptor kind '{Kind}'.");
		}
	}
}
