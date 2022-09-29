#nullable disable

#if !NET6_0_OR_GREATER
namespace System.Reflection.Metadata
{
	/// <remarks>
	/// Compatibility attribute for original added in .NET 6
	/// </remarks>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class MetadataUpdateHandlerAttribute : Attribute
	{
		public Type HandlerType { get; }

		public MetadataUpdateHandlerAttribute(Type handlerType)
		{
			HandlerType = handlerType;
		}
	}

}
#endif
