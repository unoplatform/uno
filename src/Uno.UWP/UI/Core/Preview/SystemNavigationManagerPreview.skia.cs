using Uno.Foundation.Extensibility;
using Uno.UI.Core.Preview;

namespace Windows.UI.Core.Preview;

public partial class SystemNavigationManagerPreview
{
	private ISystemNavigationManagerPreviewExtension? _extension;

	partial void InitializePlatform()
	{
		if (ApiExtensibility.CreateInstance<ISystemNavigationManagerPreviewExtension>(this, out var extension))
		{
			_extension = extension;
		}
	}

	partial void CloseApp()
	{
		if (_extension == null)
		{
			// This platform cannot programmatically close app.
			return;
		}

		_extension.RequestNativeAppClose();
	}
}
