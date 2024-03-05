using System;

namespace Windows.Foundation;

// Those methods are for WinUI code compatibility
partial class PropertyValue
{
	internal static void CreateFromDateTime(DateTimeOffset date, out object value)
	{
		value = CreateDateTime(date);
	}

	internal static void CreateFromDateTime(WindowsFoundationDateTime date, out object value)
	{
		value = CreateDateTime(date);
	}

	//internal static bool AreEqual(IPropertyValue oldValue, IPropertyValue newValue)
	internal static bool AreEqualImpl(object oldValue, object newValue)
	{
		// Check if these are the same object by comparing the identity unknowns.
		if (oldValue == newValue)
		{
			return true;
		}

		// If exactly one of the values is nullptr, they're not equal.
		if (oldValue == null || newValue == null)
		{
			return false;
		}

		//oldValueAsPV.Attach(ctl::get_property_value(oldValue));
		//newValueAsPV.Attach(ctl::get_property_value(newValue));

		// If we're not dealing with PropertyValues, then the objects aren't equal, because we already
		// did a reference check.
		//if (oldValueAsPV != nullptr && newValueAsPV != nullptr)
		{
			// If the types don't match, then they're not equal.
			//IFC_RETURN(oldValueAsPV->get_Type(&oldValueType));
			//IFC_RETURN(newValueAsPV->get_Type(&newValueType));

			var oldValueType = oldValue.GetType();
			var newValueType = newValue.GetType();
			if (oldValueType == newValueType)
			{
				return oldValue switch
				{
					// For strings we do a deep check.
					string oldValueAsString => oldValueAsString.Equals((string)newValue, StringComparison.InvariantCulture),
					char oldValueAsChar16 => oldValueAsChar16 == (char)newValue,
					bool oldValueAsBoolean => oldValueAsBoolean == (bool)newValue,
					byte oldValueAsUInt8 => oldValueAsUInt8 == (byte)newValue,
					short oldValueAsInt16 => oldValueAsInt16 == (short)newValue,
					ushort oldValueAsUInt16 => oldValueAsUInt16 == (ushort)newValue,
					int oldValueAsInt32 => oldValueAsInt32 == (int)newValue,
					uint oldValueAsUInt32 => oldValueAsUInt32 == (uint)newValue,
					long oldValueAsInt64 => oldValueAsInt64 == (long)newValue,
					ulong oldValueAsUInt64 => oldValueAsUInt64 == (ulong)newValue,
					float oldValueAsSingle => oldValueAsSingle == (float)newValue,
					double oldValueAsDouble => oldValueAsDouble == (double)newValue,
					Guid oldValueAsGuid => oldValueAsGuid == (Guid)newValue,
					DateTime oldValueAsDateTime => oldValueAsDateTime == (DateTime)newValue,
					TimeSpan oldValueAsTimeSpan => oldValueAsTimeSpan == (TimeSpan)newValue,
					Point oldValueAsPoint => oldValueAsPoint == (Point)newValue,
					Size oldValueAsSize => oldValueAsSize == (Size)newValue,
					Rect oldValueAsRect => oldValueAsRect == (Rect)newValue,

					_ => CompareOtherType(),
				};

				// case wf::PropertyType_OtherType:
				bool CompareOtherType()
				{
					//const CClassInfo* oldTypeInfo = nullptr;
					//const CClassInfo* newTypeInfo = nullptr;

					// TODO: Add TryGetClassInfoFromWinRTPropertyType to MetadataAPI and use that instead.
					//if (SUCCEEDED(MetadataAPI::GetClassInfoFromWinRTPropertyType(oldValueAsPV.Get(), wf::PropertyType_OtherType, &oldTypeInfo)) &&
					//	SUCCEEDED(MetadataAPI::GetClassInfoFromWinRTPropertyType(newValueAsPV.Get(), wf::PropertyType_OtherType, &newTypeInfo)))
					{
						// Make sure the types match. If this is a built-in enum, we know how to get the underlying values.
						//if (oldTypeInfo == newTypeInfo && newTypeInfo->IsBuiltinType() && newTypeInfo->IsEnum())
						//{
						//	UINT oldEnumValue = 0, newEnumValue = 0;
						//	IFC_RETURN(GetEnumValueFromKnownWinRTBox(oldValueAsPV.Get(), oldTypeInfo, &oldEnumValue));
						//	IFC_RETURN(GetEnumValueFromKnownWinRTBox(newValueAsPV.Get(), newTypeInfo, &newEnumValue));
						//	*areEqual = (oldEnumValue == newEnumValue);
						//}
						if (oldValueType.IsEnum)
						{
							return oldValue switch
							{
								byte oldValueAsByte => oldValueAsByte == (byte)newValue,
								sbyte oldValueAsSByte => oldValueAsSByte == (sbyte)newValue,
								short oldValueAsShort => oldValueAsShort == (short)newValue,
								ushort oldValueAsUShort => oldValueAsUShort == (ushort)newValue,
								int oldValueAsInt => oldValueAsInt == (int)newValue,
								uint oldValueAsUInt => oldValueAsUInt == (uint)newValue,
								long oldValueAsLong => oldValueAsLong == (long)newValue,
								ulong oldValueAsULong => oldValueAsULong == (ulong)newValue,

								// CS1008 Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
								_ => throw new Exception($"The '{oldValueType.Name}' enum underlying type '{oldValueType.GetEnumUnderlyingType().Name}' is not expected."),
							};
						}
					}

					return false;
				}
			}
		}

		return false;
	}
}
