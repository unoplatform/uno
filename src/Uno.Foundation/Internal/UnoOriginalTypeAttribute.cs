using System.ComponentModel;

namespace Uno.Foundation;

/// <summary>
/// This attribute is used by XAML generator for HotReload purposes.
/// External users should not use this attribute.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class UnoOriginalTypeAttribute : Attribute
{
	public UnoOriginalTypeAttribute(Type type)
	{
		OriginalType = type;
	}

	public Type OriginalType { get; }
}
