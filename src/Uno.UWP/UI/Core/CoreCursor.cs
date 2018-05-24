#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	public  partial class CoreCursor 
	{
		public CoreCursor(global::Windows.UI.Core.CoreCursorType type, uint id)
		{
			Type = type;
			Id = id;
		}

		public uint Id { get; }

		public global::Windows.UI.Core.CoreCursorType Type { get; }
	}
}
