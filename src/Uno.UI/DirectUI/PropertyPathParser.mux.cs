// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml/xcp/dxaml/lib/PropertyPathParser.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Markup;

namespace DirectUI;

partial class PropertyPathParser
{
	// Called from PropertyPathParser::Parse to move descriptors from stack storage into the object.
	// Must only be called once per object.
	private void FinalizeDescriptors(
		in InlineDescriptorBuffer descriptors,
		List<PropertyPathStepDescriptor>? overflowDescriptors,
		int count)
	{
		Debug.Assert(m_descriptorCount == 0);
		m_inlineDescriptors = descriptors;
		m_overflowDescriptors = overflowDescriptors;
		m_descriptorCount = count;
	}

	public void SetSource(string? szPath, XamlServiceProviderContext? context)
	{
		// The source can only be called once
		if (m_descriptorCount != 0)
		{
			throw new COMException("The property path has already been parsed.", unchecked((int)0x8000FFFF));
		}

		Parse(szPath, context);
	}

	private void Parse(string? szPropertyPath, XamlServiceProviderContext? context)
	{
		// Uno: instead of going through the string with char pointer, we will use index.
		// `cXyz` stores the count or length of 'Xyz' being processed, whereas `iXyz` denotes the starting index for 'Xyz'.
		// `iXyz >= szPropertyPath.Length` is equivalent of `*pXyz == L'\0'`

		//string pPropertyPath = null;
		//string pCurrentProperty = null;
		bool fExpectingProperty = false;

		// Build up descriptors in a local stack_vector during parsing.
		// At the end, we'll transfer to our storage with FinalizeDescriptors.
		// Uno: a captured value-type buffer keeps the first two slots allocation-free.
		InlineDescriptorBuffer localDescriptors = default;
		List<PropertyPathStepDescriptor>? localOverflowDescriptors = null;
		var localDescriptorCount = 0;

		// If the property path is empty or NULL then this means that we're binding
		// directly to the source
		if (string.IsNullOrEmpty(szPropertyPath) || szPropertyPath[0] == '\0')
		{
			// This will be the only step in the chain
			AppendStepDescriptor(PropertyPathStepDescriptor.CreateSourceAccess());
			FinalizeDescriptors(localDescriptors, localOverflowDescriptors, localDescriptorCount);

			return;
		}

		// This "parser" will go through the characters collecting the different types
		// of path steps supported
		//pPropertyPath = szPropertyPath;
		//pCurrentProperty = pPropertyPath;
		var source = szPropertyPath;
		var path = szPropertyPath.AsSpan();
		// Uno: native WCHAR pointers stop at the first terminator, even inside a managed string.
		if (path.IndexOf('\0') is var terminator && terminator >= 0)
		{
			path = path.Slice(0, terminator);
		}
		var iPropertyPath = 0;
		var iCurrentProperty = 0;

		while (true)
		{
			// We found a typed property, (Class.Property) and thus we will have to
			// collect all of the property name
			//if (path[iPropertyPath] == '(')
			if (iPropertyPath < path.Length && path[iPropertyPath] == '(') // dont have the extra 1char from null-terminate string here.
			{
				// Collect all of the property name
				//const WCHAR *pProperty = pPropertyPath + 1;
				var iProperty = iPropertyPath + 1;
				int cProperty = 0;

				while (iPropertyPath < path.Length && path[iPropertyPath] != ')')
				{
					cProperty++;
					iPropertyPath++;
				}

				// If we couldn't find the ')' then this is an invalid
				// property path
				if (iPropertyPath >= path.Length)
				{
					throw new ArgumentException();
				}

				//pCurrentStep = CreateDependencyPropertyPathStepDescriptor(cProperty - 1, pProperty, context);
				// Add the new step
				AppendStepDescriptor(CreateDependencyPropertyPathStepDescriptor(path.Slice(iProperty, cProperty - 1), context));

				// Go to the next character
				fExpectingProperty = false;
				iPropertyPath++;

				// Adjust the pointer to look for the
				// next step in the path
				if (iPropertyPath >= path.Length)
				{
					// We're done with the parsing
					break;
				}
				else if (path[iPropertyPath] == '.')
				{
					iPropertyPath++;
				}
				else if (path[iPropertyPath] != '[')
				{
					throw new ArgumentException();
				}

				iCurrentProperty = iPropertyPath;
			}

			// We found a separator then we need to separate the strings that represent the
			// property name and create another instance of an step. The end of the string
			// also counts as a separator
			//if (path[iPropertyPath] == '.' || path[iPropertyPath] == '[' || iPropertyPath >= path.Length)
			if (iPropertyPath >= path.Length || path[iPropertyPath] == '.' || path[iPropertyPath] == '[')
			{
				// The name of the property starts after the last separator until
				// the current character
				var cProperty = iPropertyPath - iCurrentProperty;
				var iProperty = iCurrentProperty;
				bool fHitIndexer = iPropertyPath < path.Length && path[iPropertyPath] == '[';

				// Only if actually have characters to collect can we create
				// a property, if we have something like [0][1][2] then there
				// will not be any characters to collect and thus no PropertyAccessPathStep to create
				if (cProperty > 0)
				{
					// Update the pointer for the current property
					iCurrentProperty = iPropertyPath + 1;

					// Check if this is a common property name we can use without allocation
					var commonName = PropertyPathCommonNames.TryGetCommonPropertyName(path.Slice(iProperty, cProperty));
					AppendStepDescriptor(PropertyPathStepDescriptor.CreatePropertyAccess(
						commonName ?? GetSegment(source, iProperty, cProperty)));

					// If the separator found was a '.' then the next
					// step must be a property otherwise it is an indexer
					fExpectingProperty = !fHitIndexer;
				}
				else
				{
					// We were expecting a property but we got the empty string instead
					// this is an error
					if (fExpectingProperty)
					{
						throw new ArgumentException();
					}
				}

				// If this is the last char then just break the loop
				if (iPropertyPath >= path.Length)
				{
					break;
				}

				// If we are now inside of an indexer, separated by a '[', let's extract the
				// index, looking for the matching ']' and analize it
				// We know that at this point we're not at the end of the string, the previous
				// condition makes sure of that
				if (fHitIndexer)
				{
					var iIndex = iPropertyPath + 1;
					var cIndex = 0;

					// Look for the matching ']' or the end of the string, whatever
					// happends first
					while (iPropertyPath < path.Length && path[iPropertyPath] != ']')
					{
						iPropertyPath++;
					}

					// If we found the end of the string, this is a bad property path
					if (iPropertyPath >= path.Length)
					{
						throw new ArgumentException();
					}

					cIndex = iPropertyPath - iIndex;

					var szIndex = path.Slice(iIndex, cIndex);

					// Create the right type of indexer
					if (IsNumericIndex(szIndex))
					{
						AppendStepDescriptor(PropertyPathStepDescriptor.CreateIntIndexer(ParseIntIndexer(szIndex)));
					}
					else
					{
						// TODO: Implement the string index, perhaps it is redundant?
						AppendStepDescriptor(
							PropertyPathStepDescriptor.CreateStringIndexer(GetSegment(source, iIndex, cIndex)));
					}

					// Move the char pointer to the begining of the next step
					iPropertyPath++;
					iCurrentProperty = iPropertyPath;

					// If the next character is the end of the string, then we're done
					if (iPropertyPath >= path.Length)
					{
						break;
					}
					else if (path[iPropertyPath] == '.')
					{

						// If the next character is a '.' then skip it, go to the next char so we can
						// start collecting the next property
						iPropertyPath++;
						iCurrentProperty = iPropertyPath;
						fExpectingProperty = true;
					}
					else if (path[iPropertyPath] != '[')
					{
						// The only other thing that is legal after an indexer is another indexer or a .
						// this is neither so error out

						throw new ArgumentException();
					}

					// On the next step
					continue;
				}
			}

			iPropertyPath++;
		}

		// Transfer descriptors to optimal storage if parsing succeeded
		FinalizeDescriptors(localDescriptors, localOverflowDescriptors, localDescriptorCount);

		void AppendStepDescriptor(in PropertyPathStepDescriptor descriptor)
		{
			if (localDescriptorCount < InlineDescriptorCapacity)
			{
				localDescriptors[localDescriptorCount] = descriptor;
			}
			else
			{
				(localOverflowDescriptors ??= new List<PropertyPathStepDescriptor>()).Add(descriptor);
			}

			localDescriptorCount++;
		}
	}

	private static bool IsNumericIndex(ReadOnlySpan<char> szIndex)
	{
		if (szIndex.IsEmpty)
		{
			return false;
		}

		foreach (var c in szIndex)
		{
			if (!IsWideDigit(c))
			{
				return false;
			}
		}

		return true;
	}

	private PropertyPathStepDescriptor CreateDependencyPropertyPathStepDescriptor(
		//uint nPropertyLength,
		//string pchProperty,
		ReadOnlySpan<char> propertyName, // passing the name directly, instead of a "mid-string" char* and its length
		XamlServiceProviderContext? context)
	{
		DependencyProperty? pDP = null;

		pDP = GetDPFromName(propertyName, context);
		if (pDP == null)
		{
			throw new ArgumentException();
		}

		return PropertyPathStepDescriptor.CreateDependencyProperty(pDP);
	}

	private DependencyProperty/*?*/ GetDPFromName(
		//uint nPropertyLength,
		//string pchProperty,
		ReadOnlySpan<char> propertyName, // passing the name directly, instead of a "mid-string" char* and its length
		XamlServiceProviderContext? context)
	{
		return MetadataAPI.TryGetDependencyPropertyByFullyQualifiedName(
			//XSTRING_PTR_EPHEMERAL2(pchProperty, nPropertyLength),
			propertyName,
			context
		);
	}
}
