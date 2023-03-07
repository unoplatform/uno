using System.ComponentModel;

namespace Windows.Foundation;

/// <summary>
/// Defines a method to release allocated resources.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IClosable
{
	/// <summary>
	/// Releases system resources that are exposed by a Windows Runtime object.
	/// </summary>
	void Close();
}
