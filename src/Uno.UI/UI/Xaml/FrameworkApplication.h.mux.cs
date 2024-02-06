using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

partial class Application
{
	internal AppPolicyWindowingModel AppPolicyWindowingModel => _appPolicyWindowingModel;

	private AppPolicyWindowingModel _appPolicyWindowingModel;
}
