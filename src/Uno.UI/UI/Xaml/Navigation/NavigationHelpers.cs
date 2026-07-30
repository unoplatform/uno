// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\NavigationHelpers.cpp, tag winui3/release/1.5.5, commit fd8e26f1d

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace DirectUI;

internal static class NavigationHelpers
{
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "The provided type has been marked before getting at that location")]
	internal static NavigationEventArgs CreateINavigationEventArgs(
		object content,
		object parameter,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode)
	{
		if (descriptor is null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		var sourcePageType = PageStackEntry.ResolveDescriptor(descriptor);

		// All properties can be null.
		NavigationEventArgs spNavigationEventArgs = new(
			content,
			navigationMode,
			pTransitionInfo,
			parameter,
			sourcePageType,
			null);
		return spNavigationEventArgs;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "The provided type has been marked before getting at that location")]
	internal static NavigatingCancelEventArgs CreateINavigatingCancelEventArgs(
		 object parameter,
		 NavigationTransitionInfo pTransitionInfo,
		 string descriptor,
		 NavigationMode navigationMode)
	{
		if (descriptor is null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		var sourcePageType = PageStackEntry.ResolveDescriptor(descriptor);

		// All properties can be null.
		NavigatingCancelEventArgs spNavigatingCancelEventArgs = new(
			navigationMode,
			pTransitionInfo,
			parameter,
			sourcePageType);
		return spNavigatingCancelEventArgs;
	}

	internal static void WriteUINT32ToString(
		 uint value,
		 StringBuilder buffer)
	{
		// Format: <uint>,
		buffer.Append(value.ToString(CultureInfo.InvariantCulture));
		buffer.Append(',');
	}

	internal static void ReadUINT32FromString(
		string buffer,
		int currentPosition,
		out uint pValue,
		out int pNextPosition)
	{
		//VARIANT src;
		//VARIANT dest;
		//string subString;
		//BSTR bstrValue = null;

		//VariantInit(&src);
		//VariantInit(&dest);

		//*pValue = 0;

		// Read next substring 
		ReadNextSubString(buffer, currentPosition, out var subString, out pNextPosition);

		// Convert substring to uint
		pValue = uint.Parse(subString, CultureInfo.InvariantCulture);

		//bstrValue = SysAllocString(subString.c_str());
		//IFCOOMFAILFAST(bstrValue);
		//V_VT(&src) = VT_BSTR;
		//V_BSTR(&src) = bstrValue;
		//bstrValue = null;
		//VariantChangeType(&dest, &src, 0, VT_UI4);
		//*pValue = V_UI4(&dest);
	}

	internal static void WriteHSTRINGToString(
		 string hstr,
		 StringBuilder buffer)
	{
		// Write <length>,<string>, if a string was provided
		// Write 0, if string is empty or null. Empty strings are read back as null.
		if (hstr is not null)
		{
			var psz = hstr;
			var length = psz.Length;
			NavigationHelpers.WriteUINT32ToString((uint)length, buffer);
			if (length > 0)
			{
				buffer.Append(psz);
				buffer.Append(',');
			}
		}
		else
		{
			NavigationHelpers.WriteUINT32ToString(0, buffer);
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.WriteNavigationParameterToString
	//
	//  Synopsis:    
	//     Serialize NavigationParameter into string if conversion is supported
	//  Serialization Format: 
	//      null param or parameter type not supported: <PropertyType.Empty>,
	//      Non-null param: <parameter type (PropertyType)>,<length of serialized parameter>,<serialized parameter>,
	//
	//------------------------------------------------------------------------

	internal static void WriteNavigationParameterToString(
		object pNavigationParameter,
		StringBuilder buffer,
		out bool pIsParameterTypeSupported)
	{

		string strPropertyValue = null;
		PropertyType propertyType = PropertyType.Empty;
		bool isParameterTypeSupported = false;

		pIsParameterTypeSupported = false;

		if (pNavigationParameter is not null)
		{
			ConvertNavigationParameterToHSTRING(
				pNavigationParameter,
				out strPropertyValue,
				out propertyType,
				out isParameterTypeSupported);

			if (!isParameterTypeSupported)
			{
				propertyType = PropertyType.Empty;
			}
		}
		else
		{
			// null parameters are supported
			isParameterTypeSupported = true;
			propertyType = PropertyType.Empty;
		}

		// Write property type. PropertyType.Empty will be written if 
		// property value was not provided or parameted serialization is not supported
		NavigationHelpers.WriteUINT32ToString((uint)(int)propertyType, buffer);

		// Write property value as a string. 
		if (propertyType != PropertyType.Empty)
		{
			WriteHSTRINGToString(strPropertyValue, buffer);
		}

		pIsParameterTypeSupported = isParameterTypeSupported;
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ReadNavigationParameterFromString
	//
	//  Synopsis:    
	//     Create NavigationParameter from serialized string
	//  Serialization Format: 
	//      null param or parameter type not supported: <PropertyType.Empty>,
	//      Non-null param: <parameter type (PropertyType)>,<length of serialized parameter>,<serialized parameter>,
	//
	//------------------------------------------------------------------------

	internal static void ReadNavigationParameterFromString(
		string buffer,
		int currentPosition,
		out object ppNavigationParameter,
		out int pNextPosition)
	{

		string strPropertyValue;
		PropertyType propertyType = PropertyType.Empty;
		object pNavigationParameter = null;
		uint value = 0;
		bool isParameterTypeSupported = false;

		ppNavigationParameter = null;

		// Read parameter's property type
		NavigationHelpers.ReadUINT32FromString(buffer, currentPosition, out value, out pNextPosition);
		currentPosition = pNextPosition;
		propertyType = (PropertyType)(value);

		if (propertyType == PropertyType.Empty)
		{
			// null parameter or parameter serialization is not supported
			return;
		}

		// Read parameter's serialized property value 
		ReadHSTRINGFromString(
			buffer,
			currentPosition,
			out strPropertyValue,
			out pNextPosition);
		currentPosition = pNextPosition;

		// Create NavigationParameter from serialized state in string
		ConvertHSTRINGToNavigationParameter(strPropertyValue, propertyType, out pNavigationParameter, out isParameterTypeSupported);
		if (!isParameterTypeSupported)
		{
			throw new InvalidOperationException("Invalid parameter type");
		}

		ppNavigationParameter = pNavigationParameter;
		pNavigationParameter = null;
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ReadHSTRINGFromString
	//
	//  Synopsis:    
	//     Return string. Can return null if null or empty string was stored.
	//
	//------------------------------------------------------------------------

	internal static void ReadHSTRINGFromString(
		string buffer,
		int currentPosition,
		out string phstr,
		out int pNextPosition)
	{

		int nextPosition = 0;
		uint subStringLength = 0;
		string subString;

		phstr = null;

		// Read length 
		NavigationHelpers.ReadUINT32FromString(buffer, currentPosition, out subStringLength, out nextPosition);
		currentPosition = nextPosition;

		// Read sub string if it is not a null string
		if (subStringLength != 0)
		{
			ReadNextSubString(buffer, currentPosition, subStringLength, out subString, out nextPosition);
			currentPosition = nextPosition;

			phstr = subString;
		}

		pNextPosition = nextPosition;
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ReadNextSubString
	//
	//  Synopsis:    
	//     Return substring from current position of given string to next 
	//  delimiter. If no delimiter is found, read from current position
	//  to end of given string.
	//
	//------------------------------------------------------------------------

	private static void ReadNextSubString(
		string buffer,
		int currentPosition,
		out string subString,
		out int pNextPosition)
	{

		int bufferLength = 0;
		int delimiterPosition = 0;

		// Reached end of string?
		//IFCCHECK(currentPosition != string.npos);

		bufferLength = buffer.Length;
		if ((bufferLength <= 0) || (currentPosition >= bufferLength))
		{
			throw new InvalidOperationException("Invalid buffer");
		}

		// Find ',' delimiter after the substring to be read
		delimiterPosition = buffer.IndexOf(',', currentPosition);
		if (delimiterPosition == -1)
		{
			// Delimiter not found. Use string's terminator as delimiter.
			delimiterPosition = bufferLength;
		}

		if (delimiterPosition <= currentPosition)
		{
			throw new InvalidOperationException("Invalid delimiter position");
		}

		// Get substring
		ReadNextSubString(buffer, currentPosition, (uint)(delimiterPosition - currentPosition), out subString, out pNextPosition);
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ReadNextSubString
	//
	//  Synopsis:    
	//     Given the length of a substring, return substring from current position
	//  of given string.
	//
	//------------------------------------------------------------------------

	private static void ReadNextSubString(
		string buffer,
		int currentPosition,
		uint subStringLength,
		out string subString,
		out int pNextPosition)
	{

		int bufferLength = 0;
		int nextPosition = -1;

		// Reached end of string?
		MUX_ASSERT(currentPosition != -1);
		//IFCCHECK(currentPosition != string.npos);

		bufferLength = buffer.Length;
		if ((bufferLength <= 0) || ((currentPosition + subStringLength) > bufferLength))
		{
			throw new InvalidOperationException("Invalid position in buffer");
		}

		// Get substring 
		subString = buffer.Substring(currentPosition, (int)subStringLength);

		// Skip over delimiter, and adjust for length of string
		nextPosition = currentPosition + (int)subStringLength + 1;
		if (nextPosition >= bufferLength)
		{
			// End of string
			pNextPosition = int.MaxValue; //TODO:MZ: or -1???
		}
		else
		{
			pNextPosition = nextPosition;
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ConvertNavigationParameterToHSTRING
	//
	//  Synopsis:    
	//     Convert NavigationParameter to a string. String, char, numeric and
	//  GUID property values are supported. Return the property type of
	//  the NavigationParameter and indicates if the conversion is supported. 
	//  Returns null string if conversion is not supported.
	//
	//------------------------------------------------------------------------

	private static void ConvertNavigationParameterToHSTRING(
		object pNavigationParameter,
		out string phstr,
		out PropertyType pParameterType,
		out bool pIsConversionSupported)
	{

		PropertyType propertyType = PropertyType.Empty;
		bool supportedType = true;
		string strValue = null;

		pParameterType = PropertyType.Empty;
		pIsConversionSupported = false;
		phstr = null;

		var type = pNavigationParameter?.GetType();
		propertyType = ValueConversionHelpers.GetPropertyType(type);
		// Only Navigation Parameter which is a IPropertyValue can be converted
		if (ValueConversionHelpers.CanConvertValueToString(type))
		{
			strValue = ValueConversionHelpers.ConvertValueToString(pNavigationParameter, type);
		}
		else
		{
			supportedType = false;
		}

		pIsConversionSupported = supportedType;
		pParameterType = propertyType;
		phstr = strValue;
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ConvertstringToNavigationParameter
	//
	//  Synopsis:    
	//     Use serialized parameter value from string and parameter type to 
	//  re-create NavigationParameter. Indicates if conversion is supported.
	//  Returns null NavigationParameter if conversion is not supported.
	//
	//------------------------------------------------------------------------

	private static void ConvertHSTRINGToNavigationParameter(
		string hstr,
		PropertyType parameterType,
		out object ppNavigationParameter,
		out bool pIsConversionSupported)
	{

		bool supportedType = true;
		object pNavigationParameter = null;

		pIsConversionSupported = false;
		ppNavigationParameter = null;

		switch (parameterType)
		{
			case PropertyType.UInt8:
			case PropertyType.Int16:
			case PropertyType.UInt16:
			case PropertyType.Int32:
			case PropertyType.UInt32:
			case PropertyType.Int64:
			case PropertyType.UInt64:
			case PropertyType.Single:
			case PropertyType.Double:
			case PropertyType.Char16:
			case PropertyType.Boolean:
			case PropertyType.String:
			case PropertyType.Guid:
				{
					pNavigationParameter = ValueConversionHelpers.ConvertStringToValue(hstr, parameterType);
					break;
				}

			default:
				{
					supportedType = false;
					break;
				}
		}

		pIsConversionSupported = supportedType;
		ppNavigationParameter = pNavigationParameter;
		pNavigationParameter = null;
	}
}
