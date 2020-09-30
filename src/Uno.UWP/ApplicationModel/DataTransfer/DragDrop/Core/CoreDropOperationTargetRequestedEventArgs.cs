#nullable enable

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial class CoreDropOperationTargetRequestedEventArgs 
	{
		internal ICoreDropOperationTarget? Target { get; private set; }

		public void SetTarget(ICoreDropOperationTarget target)
			=> Target = target;
	}
}
