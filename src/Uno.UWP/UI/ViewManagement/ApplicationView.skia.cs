#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Uno.Foundation.Extensibility;
using Windows.Foundation;

namespace Windows.UI.ViewManagement
{
	partial class ApplicationView
	{
		private Lazy<IApplicationViewExtension> _applicationViewExtension;

		private Size _preferredMinSize;

		partial void InitializePlatform()
		{
			_applicationViewExtension = new Lazy<IApplicationViewExtension?>(() =>
			{
				ApiExtensibility.CreateInstance<IApplicationViewExtension>(this, out var extension);
				return extension;
			});
		}

		internal Size PreferredMinSize
		{
			get => _preferredMinSize;
			set
			{
				_preferredMinSize = value;
				OnPropertyChanged();
			}
		}

		internal PropertyChangedEventHandler? PropertyChanged;

		public bool TryResizeView(Size value)
		{
			if (value.Width < _preferredMinSize.Width || value.Height < _preferredMinSize.Height)
			{
				return false;
			}
			return _applicationViewExtension.Value?.TryResizeView(value) ?? false;
		}

		public void SetPreferredMinSize(Size minSize) => PreferredMinSize = minSize;

		private void OnPropertyChanged([CallerMemberName] string? name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}

	internal interface IApplicationViewExtension
	{
		bool TryResizeView(Size size);
	}
}
