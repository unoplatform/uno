#if !NET461
namespace Windows.ApplicationModel.DataTransfer
{
	public partial class StandardDataFormats 
	{
		public static string Text => "Text";

#if __ANDROID__
		public static string Html => "HTML Format";
		public static string Uri => "UniformResourceLocatorW";
		public static string WebLink => "UniformResourceLocatorW";
#endif
	}
}
#endif
