using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.UI.Xaml.Markup
{
	public sealed partial class ProvideValueTargetProperty
	{
		internal const DynamicallyAccessedMemberTypes TypeRequirements = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

		public Type DeclaringType { get; internal set; }

		public string Name { get; internal set; }

		[DynamicallyAccessedMembers(TypeRequirements)]
		public Type Type { get; internal set; }
	}
}
