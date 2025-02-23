namespace Uno.Foundation.Extensibility;

/// <summary>
/// ApiExtension registration for the <see cref="ApiExtensibility"/> class.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class ApiExtensionAttribute : Attribute
{
	/// <summary>
	/// Creates an instance.
	/// </summary>
	/// <param name="extendedType">The type to extend</param>
	/// <param name="extensionType">The type to create an instance from</param>
	public ApiExtensionAttribute(Type extendedType, Type extensionType)
	{
		ExtensionType = extensionType;
		ExtendedType = extendedType;
		OwnerType = null;
	}

	/// <summary>
	/// Creates an instance.
	/// </summary>
	/// <param name="extendedType">The type to extend</param>
	/// <param name="extensionType">The type to create an instance from</param>
	/// <param name="ownerType">The owner type</param>
	public ApiExtensionAttribute(Type extendedType, Type extensionType, Type ownerType)
	{
		ExtensionType = extensionType;
		ExtendedType = extendedType;
		OwnerType = ownerType;
	}

	/// <summary>
	/// Creates an instance.
	/// </summary>
	/// <param name="extendedType">The type to extend</param>
	/// <param name="extensionType">The type to create an instance from</param>
	/// <param name="ownerType">The owner type</param>
	/// <param name="operatingSystemCondition">a string that is supplied as an argument to <see cref="System.OperatingSystem.IsOSPlatform"/></param>
	public ApiExtensionAttribute(Type extendedType, Type extensionType, string operatingSystemCondition, Type ownerType = null)
	{
		ExtensionType = extensionType;
		ExtendedType = extendedType;
		OwnerType = ownerType;
		OperatingSystemCondition = operatingSystemCondition;
	}

	/// <summary>
	/// The type to extend
	/// </summary>
	public Type ExtensionType { get; }

	/// <summary>
	/// The Type to create
	/// </summary>
	public Type ExtendedType { get; }

	/// <summary>
	/// The Type to create
	/// </summary>
	public Type OwnerType { get; }

	/// <summary>
	/// The Type to create
	/// </summary>
	public string OperatingSystemCondition { get; }
}
