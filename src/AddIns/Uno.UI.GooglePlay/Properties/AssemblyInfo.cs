using System.Reflection;
using Uno.Foundation.Extensibility;
using Uno.UI.GooglePlay;
using Windows.Services.Store.Internal;

[assembly: AssemblyMetadata("IsTrimmable", "True")]

[assembly: ApiExtension(typeof(IStoreContextExtension), typeof(StoreContextExtension))]
