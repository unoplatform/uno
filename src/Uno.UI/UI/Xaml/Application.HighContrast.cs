#if __ANDROID__ || __IOS__ || __TVOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__

namespace Microsoft.UI.Xaml
{
	public partial class Application
	{
		private ApplicationHighContrastAdjustment _highContrastAdjustment = ApplicationHighContrastAdjustment.Auto;

		public ApplicationHighContrastAdjustment HighContrastAdjustment
		{
			get => _highContrastAdjustment;
			set
			{
				if (_highContrastAdjustment != value)
				{
					_highContrastAdjustment = value;
					OnHighContrastAdjustmentChanged();
				}
			}
		}
	}
}

#endif