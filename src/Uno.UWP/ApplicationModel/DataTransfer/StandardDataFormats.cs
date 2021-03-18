#nullable enable

#if !NET461
namespace Windows.ApplicationModel.DataTransfer
{
	public partial class StandardDataFormats
	{
		public static string UserActivityJsonArray { get; } = "UserActivityJsonArray";
		public static string WebLink               { get; } = "UniformResourceLocatorW";
		public static string ApplicationLink       { get; } = "ApplicationLink";
		public static string Text                  { get; } = "Text";
		public static string Uri                   { get; } = "UniformResourceLocatorW";
		public static string Html                  { get; } = "HTML Format";
		public static string Rtf                   { get; } = "Rich Text Format";
		public static string Bitmap                { get; } = "Bitmap";
		public static string StorageItems		   { get; } = "Shell IDList Array";
	}
}
#endif
