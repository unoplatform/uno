using System;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Foundation;
using System.Globalization;
using Uno.Foundation.Extensibility;
using Uno.Disposables;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView : IApplicationViewEvents
	{
		private readonly IApplicationViewExtension _applicationViewExtension;
		private Size _preferredMinSize = Size.Empty;

		public ApplicationView()
		{
			if (!ApiExtensibility.CreateInstance(this, out _applicationViewExtension))
			{
				throw new InvalidOperationException($"Unable to find IApplicationViewExtension extension");
			}
		}

		public string Title
		{
			get => _applicationViewExtension.Title;
			set => _applicationViewExtension.Title = value;
		}

		public bool TryEnterFullScreenMode() => _applicationViewExtension.TryEnterFullScreenMode();

		public void ExitFullScreenMode() => _applicationViewExtension.ExitFullScreenMode();

		public bool TryResizeView(Size value)
		{
			if (value.Width < _preferredMinSize.Width || value.Height < _preferredMinSize.Height)
			{
				return false;
			}
			return _applicationViewExtension.TryResizeView(value);
		}

		public void SetPreferredMinSize(Size minSize)
		{
			_applicationViewExtension.SetPreferredMinSize(minSize);
			_preferredMinSize = minSize;
		}
	}

	internal interface IApplicationViewExtension
	{
		string Title { get; set; }

		bool TryEnterFullScreenMode();

		void ExitFullScreenMode();

		bool TryResizeView(Size size);

		void SetPreferredMinSize(Size minSize);
	}

	internal interface IApplicationViewEvents
	{

	}
}
