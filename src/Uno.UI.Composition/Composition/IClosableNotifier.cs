#if HAS_UNO_WINUI

namespace Microsoft.UI;

/// <summary>
/// When implemented in a WinRT runtime class, provides notification that an object has been closed (disposed).
/// </summary>
/// <remarks>
/// <para>The purpose of this interface is to provide a reliable notification when the underlying object is closed (disposed) and also a way to check if the object is closed.</para>
/// <para>Typically, an object would need to know when another WinRT object is closed if it depends on that WinRT object to satisfy its functionality.If the WinRT object it
/// depends on is closed, you can perform cleanup operations and/or unregister from events.</para>
/// </remarks>
public interface IClosableNotifier
{
	/// <summary>
	/// Gets a value that indicates whether the object is closed (disposed).
	/// </summary>
	bool IsClosed { get; }

	/// <summary>
	/// Occurs when the object has been closed (disposed), after the <see cref="FrameworkClosed"/> event, to notify the app that the object is closed.
	/// </summary>
	event ClosableNotifierHandler Closed;

	/// <summary>
	/// Occurs when the object has been closed (disposed), before the <see cref="Closed"/> event, to notify the framework (such as XAML) that the object is closed.
	/// </summary>
	event ClosableNotifierHandler FrameworkClosed;
}

#endif
