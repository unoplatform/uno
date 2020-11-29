namespace Windows.UI.Core
{
	public partial class CoreCursor 
	{
		public CoreCursor(CoreCursorType type, uint id)
		{
			Type = type;
			Id = id;
		}

		public uint Id { get; }

		public CoreCursorType Type { get; }
	}
}
