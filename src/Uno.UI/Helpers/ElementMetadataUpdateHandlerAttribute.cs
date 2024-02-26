namespace System.Reflection.Metadata;

/// <summary>
/// Defines a handler for an type of element that is found in the visual tree
/// which will be invoked if there are metadata updates
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ElementMetadataUpdateHandlerAttribute : Attribute
{
	public Type ElementType { get; }
	public Type HandlerType { get; }

	public ElementMetadataUpdateHandlerAttribute(Type elementType, Type handlerType)
	{
		ElementType = elementType;
		HandlerType = handlerType;
	}

	public ElementMetadataUpdateHandlerAttribute(Type handlerType) : this(typeof(object), handlerType)
	{
	}
}
