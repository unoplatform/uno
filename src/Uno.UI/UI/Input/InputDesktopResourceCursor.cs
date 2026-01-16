using Uno;

namespace Microsoft.UI.Input
{
	public sealed partial class InputDesktopResourceCursor : InputCursor
	{
		private InputDesktopResourceCursor(string moduleName, uint resourceId)
		{
			ModuleName = moduleName;
			ResourceId = resourceId;
		}

		[NotImplemented]
		public string ModuleName { get; }

		[NotImplemented]
		public uint ResourceId { get; }

		public static InputDesktopResourceCursor Create(uint resourceId)
			=> new InputDesktopResourceCursor(null, resourceId);

		public static InputDesktopResourceCursor CreateFromModule(string moduleName, uint resourceId)
			=> new InputDesktopResourceCursor(moduleName, resourceId);
	}
}
