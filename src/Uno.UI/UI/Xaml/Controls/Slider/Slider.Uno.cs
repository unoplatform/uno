using Uno.Disposables;

namespace Windows.UI.Xaml.Controls;

public partial class Slider
{
	private readonly SerialDisposable _prepareStateToken = new SerialDisposable();
	private readonly SerialDisposable _sliderContainerToken = new SerialDisposable();
	private bool _isTemplateApplied;

	private void Slider_Loaded(object sender, RoutedEventArgs e)
	{
		if (_prepareStateToken.Disposable is null)
		{
			PrepareState();
		}

		if (_sliderContainerToken.Disposable is null && _isTemplateApplied)
		{
			AttachSliderContainerEvents();
		}

		if (_tpElementVerticalThumb is not null && _elementVerticalThumbDragCompletedToken.Disposable is null)
		{
			AttachVerticalThumbSubscriptions();
		}

		if (_tpElementHorizontalThumb is not null && _elementHorizontalThumbDragCompletedToken.Disposable is null)
		{
			AttachHorizontalThumbSubscriptions();
		}
	}

	private void Slider_Unloaded(object sender, RoutedEventArgs e)
	{
		_prepareStateToken.Disposable = null;
		_sliderContainerToken.Disposable = null;

		_elementVerticalThumbDragCompletedToken.Disposable = null;
		_elementVerticalThumbDragDeltaToken.Disposable = null;
		_elementVerticalThumbDragStartedToken.Disposable = null;
		_elementVerticalThumbSizeChangedToken.Disposable = null;
		_elementHorizontalThumbDragCompletedToken.Disposable = null;
		_elementHorizontalThumbDragDeltaToken.Disposable = null;
		_elementHorizontalThumbDragStartedToken.Disposable = null;
		_elementHorizontalThumbSizeChangedToken.Disposable = null;
	}
}
