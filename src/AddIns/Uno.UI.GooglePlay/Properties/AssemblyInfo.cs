using System.Reflection;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Media.Imaging.Svg;

[assembly: AssemblyMetadata("IsTrimmable", "True")]

[assembly: ApiExtension(typeof(IInAppReviewExtension), typeof(InAppReviewExtension))]
