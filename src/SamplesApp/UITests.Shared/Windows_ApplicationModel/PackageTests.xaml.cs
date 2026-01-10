using System;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel;
using Windows.System;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_ApplicationModel
{
	[Sample("Windows.ApplicationModel", "Package", ignoreInSnapshotTests: true, description: "Tests the Package and PackageId properties")]

	public sealed partial class PackageTests : UserControl
	{
		public PackageTests()
		{
			this.InitializeComponent();
			DisplayName = SafeSet(() => Package.Current.DisplayName);
			InstalledPath = SafeSet(() => Package.Current.InstalledPath);
			var packageId = Package.Current.Id;
			Architecture = SafeSet(() => packageId.Architecture);
			FamilyName = SafeSet(() => packageId.FamilyName);
			FullName = SafeSet(() => packageId.FullName);
			Name = SafeSet(() => packageId.Name);
			Publisher = SafeSet(() => packageId.Publisher);
			PublisherId = SafeSet(() => packageId.PublisherId);
			ResourceId = SafeSet(() => packageId.ResourceId);
			Version = SafeSet(() => $"{packageId.Version.Major}.{packageId.Version.Minor}.{packageId.Version.Revision}.{packageId.Version.Build}");
		}

		private string SafeSet(Func<object> propertyGetter)
		{
			try
			{
				return propertyGetter()?.ToString() ?? "(null)";
			}
			catch (NotImplementedException)
			{
				return "Not implemented";
			}
			catch (Exception ex)
			{
				return $"Exception thrown - {ex.Message}";
			}
		}

		public string DisplayName { get; }

		public string InstalledPath { get; }

		public string Architecture { get; }

		public string Author { get; }

		public string FamilyName { get; }

		public string FullName { get; }

		public string ProductId { get; }

		public string Publisher { get; }

		public string PublisherId { get; }

		public string ResourceId { get; }

		public string Version { get; }
	}
}
