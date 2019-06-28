namespace Windows.UI.Input
{
	public partial class PointerPointProperties 
	{
		internal PointerPointProperties()
		{
		}

		public bool IsBarrelButtonPressed { get; internal set; }

		public bool IsEraser { get; internal set; }

		public bool IsHorizontalMouseWheel { get; internal set; }

		public bool IsLeftButtonPressed { get; internal set; }

		public bool IsMiddleButtonPressed { get; internal set; }

		public bool IsPrimary { get; internal set; }

		public bool IsRightButtonPressed { get; internal set; }

		public bool IsXButton1Pressed { get; internal set; }

		public bool IsXButton2Pressed { get; internal set; }

		public bool IsInRange { get; internal set; }

		public PointerUpdateKind PointerUpdateKind { get; internal set; }

		[global::Uno.NotImplemented]
		public int MouseWheelDelta => 0;
	}
}
