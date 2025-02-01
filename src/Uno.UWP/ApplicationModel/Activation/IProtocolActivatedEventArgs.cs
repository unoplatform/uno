using System;

namespace Windows.ApplicationModel.Activation;

/// <summary>
/// Provides data when an app is activated because it is the program associated with a protocol.
/// </summary>
public partial interface IProtocolActivatedEventArgs : IActivatedEventArgs
{
	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) for which the app was activated.
	/// </summary>
	Uri Uri { get; }
}
