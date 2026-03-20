using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.UI.Xaml;

namespace Uno.UI
{
	internal static class ToolkitHelper
	{
		[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "[DynamicallyAccessedMembers] on ownerTypeName should ensure availability of target type.")]
		public static DependencyProperty GetProperty(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
			string ownerTypeName,
			string propertyName)
		{
			var ownerType = Type.GetType(ownerTypeName, throwOnError: false);
			if (ownerType == null)
			{
				ownerType = Type.GetType(ownerTypeName + ",Uno.UI.Toolkit");
			}
			var dp = DependencyProperty.GetProperty(ownerType, propertyName);
			return dp;
		}
	}
}
