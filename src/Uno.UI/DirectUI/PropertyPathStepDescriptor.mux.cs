// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml/xcp/dxaml/lib/PropertyPathStepDescriptor.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace DirectUI;

readonly partial struct PropertyPathStepDescriptor
{
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
