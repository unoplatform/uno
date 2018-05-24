#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreCursor 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint CoreCursor.Id is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreCursorType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreCursorType CoreCursor.Type is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public CoreCursor( global::Windows.UI.Core.CoreCursorType type,  uint id) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreCursor", "CoreCursor.CoreCursor(CoreCursorType type, uint id)");
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreCursor.CoreCursor(Windows.UI.Core.CoreCursorType, uint)
		// Forced skipping of method Windows.UI.Core.CoreCursor.Id.get
		// Forced skipping of method Windows.UI.Core.CoreCursor.Type.get
	}
}
