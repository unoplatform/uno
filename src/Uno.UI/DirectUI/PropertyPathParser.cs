using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Extensions.Specialized;
using Uno.UI.Xaml.Markup;

namespace DirectUI;

internal partial class PropertyPathParser // src\dxaml\xcp\dxaml\lib\PropertyPathParser.h
{
	// public:
	//public PropertyPathParser();
	//public ~PropertyPathParser();

	// public:
	//public void SetSource(string szPath, XamlServiceProviderContext context);

	public IReadOnlyList<PropertyPathStepDescriptor> Descriptors => m_descriptors;

	// private:
	//private void Parse(string szPropertyPath, XamlServiceProviderContext context);

	//private bool IsNumericIndex(string szIndex);

	//private void AppendStepDescriptor(PropertyPathStepDescriptor pDescriptor);

	//private PropertyPathStepDescriptor CreateDependencyPropertyPathStepDescriptor(
	//	uint nPropertyLength,
	//	string pchProperty,
	//	XamlServiceProviderContext context);

	//private DependencyProperty/*?*/ GetDPFromName(
	//	uint nPropertyLength,
	//	string pchProperty,
	//	XamlServiceProviderContext context);

	// private:
	private List<PropertyPathStepDescriptor> m_descriptors = new();
}
partial class PropertyPathParser // src\dxaml\xcp\dxaml\lib\PropertyPathParser.cpp
{
	public PropertyPathParser()
	{
	}
	//~PropertyPathParser()
	//{
	//	std::for_each(m_descriptors.begin(), m_descriptors.end(),
	//		[](PropertyPathStepDescriptor * pDescriptor)
	//		{
	//		delete pDescriptor;
	//	});
	//}

	public void SetSource(string szPath, XamlServiceProviderContext context)
	{
		// The source can only be called once
		if (m_descriptors.Any())
		{
			return;
		}

		Parse(szPath, context);
	}

	private void Parse(string szPropertyPath, XamlServiceProviderContext context)
	{
		// Uno: instead of going through the string with char pointer, we will use index.
		// `cXyz` stores the count or length of 'Xyz' being processed, whereas `iXyz` denotes the starting index for 'Xyz'.
		// `iXyz >= szPropertyPath.Length` is equivalent of `*pXyz == L'\0'`

		//string pPropertyPath = null;
		//string pCurrentProperty = null;
		string szCurrentProperty = null;
		string szIndex = null;
		PropertyPathStepDescriptor pCurrentStep = null;
		bool fExpectingProperty = false;

		// If the property path is empty or NULL then this means that we're binding
		// directly to the source
		if (string.IsNullOrEmpty(szPropertyPath))
		{
			pCurrentStep = new SourceAccessPathStepDescriptor();

			// This will be the only step in the chain
			AppendStepDescriptor(pCurrentStep);

			return;
		}

		// This "parser" will go through the characters collecting the different types
		// of path steps supported
		//pPropertyPath = szPropertyPath;
		//pCurrentProperty = pPropertyPath;
		var path = szPropertyPath;
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

				while (path[iPropertyPath] != ')' && iPropertyPath < path.Length)
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
				pCurrentStep = CreateDependencyPropertyPathStepDescriptor(path.Substring(iProperty, cProperty - 1), context);

				// Add the new step
				AppendStepDescriptor(pCurrentStep);
				pCurrentStep = null;

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
					//szCurrentProperty = new WCHAR[cProperty + 1];   // +1 for the 0 at the end

					// Fill the string with the current property name, which will be from the last 
					// separator until the '.'
					//wcsncpy_s(szCurrentProperty, cProperty + 1, pProperty, cProperty);
					szCurrentProperty = path.Substring(iProperty, cProperty);

					// Update the pointer for the current property
					iCurrentProperty = iPropertyPath + 1;

					// Now we can create a property path step
					pCurrentStep = new PropertyAccessPathStepDescriptor(szCurrentProperty);
					szCurrentProperty = null;

					// Now add the step to the list
					AppendStepDescriptor(pCurrentStep);
					pCurrentStep = null;

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

					//szIndex = new WCHAR[cIndex + 1];

					// Fill the string with the index
					//if (0 != wcsncpy_s(szIndex, cIndex + 1, pIndex, cIndex))
					//{
					//	throw new ArgumentException();
					//}

					szIndex = path.Substring(iIndex, cIndex);

					//#pragma prefast(push)
					// wcsncpy_s will always null-terminate szIndex on success
					//#pragma prefast(disable: __WARNING_BUFFER_OVERFLOW, "Read overflow of null terminated buffer using expression '(WCHAR *)szIndex'")
					// Create the right type of indexer
					if (IsNumericIndex(szIndex))
					{
						pCurrentStep = new IntIndexerPathStepDescriptor(int.Parse(szIndex, CultureInfo.InvariantCulture));
						szIndex = null;
					}
					else
					{
						// TODO: Implement the string index, perhaps it is redundant?
						pCurrentStep = new StringIndexerPathStepDescriptor(szIndex);
						szIndex = null; // The indexer now owns the string
					}
					//#pragma prefast(pop)

					// Now add the step to the list
					AppendStepDescriptor(pCurrentStep);
					pCurrentStep = null;

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
	}

	private void AppendStepDescriptor(PropertyPathStepDescriptor pDescriptor)
	{
		m_descriptors.Add(pDescriptor);
	}

	private bool IsNumericIndex(string szIndex)
	{
		foreach (var c in szIndex)
		{
			if (!char.IsDigit(c)) // std::iswdigit -> "0123456789"
			{
				return false;
			}
		}

		return true;
	}

	private PropertyPathStepDescriptor CreateDependencyPropertyPathStepDescriptor(
		//uint nPropertyLength,
		//string pchProperty,
		string propertyName, // passing the name directly, instead of a "mid-string" char* and its length
		XamlServiceProviderContext context)
	{
		DependencyProperty pDP = null;

		pDP = GetDPFromName(propertyName, context);
		if (pDP == null)
		{
			throw new ArgumentException();
		}

		return new DependencyPropertyPathStepDescriptor(pDP);
	}

	private DependencyProperty/*?*/ GetDPFromName(
		//uint nPropertyLength,
		//string pchProperty,
		string propertyName, // passing the name directly, instead of a "mid-string" char* and its length
		XamlServiceProviderContext context)
	{
		return MetadataAPI.TryGetDependencyPropertyByFullyQualifiedName(
			//XSTRING_PTR_EPHEMERAL2(pchProperty, nPropertyLength),
			propertyName,
			context
		);
	}
}
