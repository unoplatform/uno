#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
	internal class RequiredMemberAttribute : Attribute;
	internal class CompilerFeatureRequiredAttribute(string name) : Attribute
	{
		internal string Name { get; } = name;
	}
}

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Constructor)]
	internal class SetsRequiredMembersAttribute : Attribute;
}
#endif
