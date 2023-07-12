using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// Helper class to map types to their original types and vice versa as part of hot reload
	/// </summary>
	public static class TypeMappingHelper
	{
		/// <summary>
		/// This maps a replacement type to the original type. This dictionary will grow with each iteration 
		/// of the original type.
		/// </summary>
		private static IDictionary<Type, Type> MappedTypeToOrignalTypeMapings { get; } = new Dictionary<Type, Type>();

		/// <summary>
		/// This maps an original type to the most recent replacement type. This dictionary will only grow when
		/// a different original type is modified.
		/// </summary>
		private static IDictionary<Type, Type> OriginalTypeToMappedType { get; } = new Dictionary<Type, Type>();

		/// <summary>
		/// Extension method to return the replacement type for a given instance type
		/// </summary>
		/// <param name="instanceType">This is the type that may have been replaced</param>
		/// <returns>If instanceType has been replaced, then the replacement type, otherwise the instanceType</returns>
		public static Type GetReplacementType(this Type instanceType)
		{
			// Two scenarios:
			// 1. The instance type is a mapped type, in which case we need to get the original type
			// 2. The instance type is an original type, in which case we need to get the mapped type
			var originalType = GetOriginalType(instanceType) ?? instanceType;
			return originalType.GetMappedType() ?? instanceType;
		}

		internal static Type GetMappedType(this Type originalType) =>
			OriginalTypeToMappedType.TryGetValue(originalType, out var mappedType) ? mappedType : default;

		internal static Type GetOriginalType(this Type mappedType) =>
			MappedTypeToOrignalTypeMapings.TryGetValue(mappedType, out var originalType) ? originalType : default;

		internal static bool IsReplacedBy(this Type sourceType, Type mappedType)
		{
			if (mappedType.GetOriginalType() is { } originalType)
			{
				if (originalType == sourceType)
				{
					return true;
				}
				return sourceType.GetOriginalType()?.GetMappedType() == mappedType;
			}

			return false;
		}

		internal static void RegisterMapping(Type mappedType, Type originalType)
		{
			MappedTypeToOrignalTypeMapings[mappedType] = originalType;
			OriginalTypeToMappedType[originalType] = mappedType;
		}
	}
}
