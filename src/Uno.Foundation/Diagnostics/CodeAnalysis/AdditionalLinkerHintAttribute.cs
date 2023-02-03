namespace Uno.Foundation.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Provide the ability to include an additional type to the linker hints generator.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	internal sealed class AdditionalLinkerHintAttribute : Attribute
	{
		/// <summary>
		/// Builds a new linker hint
		/// </summary>
		/// <param name="fullTypeName">The full type name, using Cecil's formatting</param>
		public AdditionalLinkerHintAttribute(string fullTypeName)
		{
			FullTypeName = fullTypeName;
		}

		/// <summary>
		/// The linker hint target type
		/// </summary>
		public string FullTypeName { get; }
	}
}
