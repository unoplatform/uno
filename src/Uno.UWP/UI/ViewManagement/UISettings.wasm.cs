#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	public  partial class UISettings 
	{
		public UISettings()
		{
		}

		[global::Uno.NotImplemented]
		public bool AnimationsEnabled => false;
	}
}
