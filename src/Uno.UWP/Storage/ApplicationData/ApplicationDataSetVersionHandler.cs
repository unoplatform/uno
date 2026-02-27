namespace Windows.Storage;

/// <summary>
/// Represents a method that handles the request to set the version of the application data in the application data store.
/// </summary>
/// <param name="setVersionRequest">The set version request.</param>
public delegate void ApplicationDataSetVersionHandler(SetVersionRequest setVersionRequest);
