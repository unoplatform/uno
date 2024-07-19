using System;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Helpers.WinUI;

namespace DirectUI;

internal static class NavigationHelpers
{
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

		var sourcePageType = Type.GetType(descriptor);

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

		var sourcePageType = Type.GetType(descriptor);

		// All properties can be null.
		NavigatingCancelEventArgs spNavigatingCancelEventArgs = new(
			navigationMode,
			pTransitionInfo,
			parameter,
			sourcePageType);
		return spNavigatingCancelEventArgs;
	}

	private void WriteuintToString(
		 uint value,
		 string &buffer)
	{

		VARIANT src;
		VARIANT dest;

		VariantInit(&src);
		VariantInit(&dest);

		// Convert uint to String
		V_VT(&src) = VT_UI4;
		V_UI4(&src) = value;
		VariantChangeType(&dest, &src, 0, VT_BSTR);

		// Format: <uint>,
		buffer.append(V_BSTR(&dest));
		buffer.append(",");

	Cleanup:
		VariantClear(&src);
		VariantClear(&dest);
		RRETURN(hr);
	}

	private void ReaduintFromString(
		 string &buffer,
		int currentPosition,
		out uint* pValue,
		out int* pNextPosition)
	{

		VARIANT src;
		VARIANT dest;
		string subString;
		BSTR bstrValue = null;

		VariantInit(&src);
		VariantInit(&dest);

		*pValue = 0;

		// Read next substring 
		ReadNextSubString(buffer, currentPosition, subString, pNextPosition);

		// Convert substring to uint
		bstrValue = SysAllocString(subString.c_str());
		IFCOOMFAILFAST(bstrValue);
		V_VT(&src) = VT_BSTR;
		V_BSTR(&src) = bstrValue;
		bstrValue = null;
		VariantChangeType(&dest, &src, 0, VT_UI4);
		*pValue = V_UI4(&dest);

	Cleanup:
		VariantClear(&src);
		VariantClear(&dest);
		SysFreeString(bstrValue);
		RRETURN(hr);
	}

	private void WritestringToString(
		 string hstr,
		 string &buffer)
	{
		string psz = null;
		uint length = 0;

		// Write <length>,<string>, if a string was provided
		// Write 0, if string is empty or null. Empty strings are read back as null.
		if (hstr)
		{
			psz = HStringUtil.GetRawBuffer(hstr, &length);
			NavigationHelpers.WriteuintToString(length, buffer);
			if (length > 0)
			{
				buffer.append(psz);
				buffer.append(",");
			}
		}
		else
		{
			NavigationHelpers.WriteuintToString(0, buffer);
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.WriteNavigationParameterToString
	//
	//  Synopsis:    
	//     Serialize NavigationParameter into string if conversion is supported
	//  Serialization Format: 
	//      null param or parameter type not supported: <wf.PropertyType_Empty>,
	//      Non-null param: <parameter type (wf.PropertyType)>,<length of serialized parameter>,<serialized parameter>,
	//
	//------------------------------------------------------------------------

	private void WriteNavigationParameterToString(
		 object pNavigationParameter,
		 string &buffer,
		out bool* pIsParameterTypeSupported)
	{

		string strPropertyValue;
		wf.PropertyType propertyType = wf.PropertyType_Empty;
		bool isParameterTypeSupported = false;

		*pIsParameterTypeSupported = false;

		if (pNavigationParameter)
		{
			(ConvertNavigationParameterTostring(
				pNavigationParameter,
				strPropertyValue.GetAddressOf(),
				&propertyType,
				&isParameterTypeSupported));

			if (!isParameterTypeSupported)
			{
				propertyType = wf.PropertyType_Empty;
			}
		}
		else
		{
			// null parameters are supported
			isParameterTypeSupported = true;
			propertyType = wf.PropertyType_Empty;
		}

		// Write property type. wf.PropertyType_Empty will be written if 
		// property value was not provided or parameted serialization is not supported
		NavigationHelpers.WriteuintToString(propertyType, buffer);

		// Write property value as a string. 
		if (propertyType != wf.PropertyType_Empty)
		{
			WritestringToString(strPropertyValue, buffer);
		}

		*pIsParameterTypeSupported = isParameterTypeSupported;
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ReadNavigationParameterFromString
	//
	//  Synopsis:    
	//     Create NavigationParameter from serialized string
	//  Serialization Format: 
	//      null param or parameter type not supported: <wf.PropertyType_Empty>,
	//      Non-null param: <parameter type (wf.PropertyType)>,<length of serialized parameter>,<serialized parameter>,
	//
	//------------------------------------------------------------------------

	private void ReadNavigationParameterFromString(
		 string &buffer,
		int currentPosition,
		out object* ppNavigationParameter,
		out int* pNextPosition)
	{

		string strPropertyValue;
		wf.PropertyType propertyType = wf.PropertyType_Empty;
		object pNavigationParameter = null;
		uint value = 0;
		bool isParameterTypeSupported = false;

		*ppNavigationParameter = null;

		// Read parameter's property type
		(NavigationHelpers.ReaduintFromString(buffer, currentPosition,
				&value, pNextPosition));
		currentPosition = *pNextPosition;
		propertyType = (wf.PropertyType)(value);

		if (propertyType == wf.PropertyType_Empty)
		{
			// null parameter or parameter serialization is not supported
			goto Cleanup;
		}

		// Read parameter's serialized property value 
		(ReadstringFromString(
			buffer,
			currentPosition,
			strPropertyValue.GetAddressOf(),
			pNextPosition));
		currentPosition = *pNextPosition;

		// Create NavigationParameter from serialized state in string
		ConvertstringToNavigationParameter(strPropertyValue, propertyType, &pNavigationParameter, &isParameterTypeSupported);
		IFCCHECK(isParameterTypeSupported);

		*ppNavigationParameter = pNavigationParameter;
		pNavigationParameter = null;

	Cleanup:
		ReleaseInterface(pNavigationParameter);
		RRETURN(hr);
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ReadstringFromString
	//
	//  Synopsis:    
	//     Return string. Can return null if null or empty string was stored.
	//
	//------------------------------------------------------------------------

	private void ReadstringFromString(
		 string &buffer,
		int currentPosition,
		out string* phstr,
		out int* pNextPosition)
	{

		int nextPosition = string.npos;
		uint subStringLength = 0;
		string subString;

		*phstr = null;

		// Read length 
		NavigationHelpers.ReaduintFromString(buffer, currentPosition, &subStringLength, &nextPosition);
		currentPosition = nextPosition;

		// Read sub string if it is not a null string
		if (subStringLength)
		{
			ReadNextSubString(buffer, currentPosition, subStringLength, subString, &nextPosition);
			currentPosition = nextPosition;
        
        .WindowsCreateString(subString.c_str(), subString.length(), phstr);
		}

		*pNextPosition = nextPosition;
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

	private void ReadNextSubString(
		 string &buffer,
		int currentPosition,
		 string &subString,
		out int* pNextPosition)
	{

		int bufferLength = 0;
		int delimiterPosition = 0;

		// Reached end of string?
		IFCCHECK(currentPosition != string.npos);

		bufferLength = buffer.length();
		IFCCHECK((bufferLength > 0) && (currentPosition < bufferLength));

		// Find ',' delimiter after the substring to be read
		delimiterPosition = buffer.find(",", currentPosition);
		if (delimiterPosition == string.npos)
		{
			// Delimiter not found. Use string's terminator as delimiter.
			delimiterPosition = bufferLength;
		}
		IFCCHECK(delimiterPosition > currentPosition);

		// Get substring
		ReadNextSubString(buffer, currentPosition, delimiterPosition - currentPosition, subString, pNextPosition);
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

	private void ReadNextSubString(
		 string &buffer,
		int currentPosition,
		int subStringLength,
		 string &subString,
		out int* pNextPosition)
	{

		int bufferLength = 0;
		int nextPosition = string.npos;

		// Reached end of string?
		MUX_ASSERT(currentPosition != string.npos);
		IFCCHECK(currentPosition != string.npos);

		bufferLength = buffer.length();
		IFCCHECK((bufferLength > 0) && ((currentPosition + subStringLength) <= bufferLength));

		// Get substring 
		subString = buffer.substr(currentPosition, subStringLength);

		// Skip over delimiter, and adjust for length of string
		nextPosition = currentPosition + subStringLength + 1;
		if (nextPosition >= bufferLength)
		{
			// End of string
			*pNextPosition = string.npos;
		}
		else
		{
			*pNextPosition = nextPosition;
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method: NavigationHelpers.ConvertNavigationParameterTostring
	//
	//  Synopsis:    
	//     Convert NavigationParameter to a string. String, char, numeric and
	//  GUID property values are supported. Return the property type of
	//  the NavigationParameter and indicates if the conversion is supported. 
	//  Returns null string if conversion is not supported.
	//
	//------------------------------------------------------------------------

	private void ConvertNavigationParameterTostring(
		 object* pNavigationParameter,
		out string* phstr,
		out wf.PropertyType* pParameterType,
		out bool* pIsConversionSupported)
	{

		wf.PropertyType propertyType = wf.PropertyType_Empty;
		bool supportedType = true;
		string strValue;
		wf.IPropertyValue* pPropertyValue = null;

		*pParameterType = wf.PropertyType_Empty;
		*pIsConversionSupported = false;
		*phstr = null;

		// Only Navigation Parameter which is a IPropertyValue can be converted
		pPropertyValue = ctl.get_property_value(pNavigationParameter);
		if (!pPropertyValue)
		{
			goto Cleanup;
		}

		propertyType = pPropertyValue.Type;

		if (ValueConversionHelpers.CanConvertValueToString(propertyType))
		{
			ValueConversionHelpers.ConvertValueToString(pPropertyValue, propertyType, strValue.GetAddressOf());
		}
		else
		{
			supportedType = false;
		}

		*pIsConversionSupported = supportedType;
		*pParameterType = propertyType;
		*phstr = strValue.Detach();

	Cleanup:
		ReleaseInterface(pPropertyValue);
		RRETURN(hr);
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

	private void ConvertstringToNavigationParameter(
		 string hstr,
		 wf.PropertyType parameterType,
		out object* ppNavigationParameter,
		out bool* pIsConversionSupported)
	{

		bool supportedType = true;
		object pNavigationParameter = null;

		*pIsConversionSupported = false;
		*ppNavigationParameter = null;

		switch (parameterType)
		{
			case wf.PropertyType_UInt8:
			case wf.PropertyType_Int16:
			case wf.PropertyType_UInt16:
			case wf.PropertyType_Int32:
			case wf.PropertyType_UInt32:
			case wf.PropertyType_Int64:
			case wf.PropertyType_UInt64:
			case wf.PropertyType_Single:
			case wf.PropertyType_Double:
			case wf.PropertyType_Char16:
			case wf.PropertyType_Boolean:
			case wf.PropertyType_String:
			case wf.PropertyType_Guid:
				{
					ValueConversionHelpers.ConvertStringToValue(hstr, parameterType, &pNavigationParameter);
					break;
				}

			default:
				{
					supportedType = false;
					break;
				}
		}

		*pIsConversionSupported = supportedType;
		*ppNavigationParameter = pNavigationParameter;
		pNavigationParameter = null;

	Cleanup:
		ReleaseInterface(pNavigationParameter);
		RRETURN(hr);
	}
}
