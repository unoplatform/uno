using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.UI.Xaml.Data
{
	public partial interface IValueConverter
	{
		internal const DynamicallyAccessedMemberTypes TargetTypeRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

		object Convert(object value, [DynamicallyAccessedMembers(TargetTypeRequirements)] Type targetType, object parameter, string language);
		object ConvertBack(object value, [DynamicallyAccessedMembers(TargetTypeRequirements)] Type targetType, object parameter, string language);
	}
}
