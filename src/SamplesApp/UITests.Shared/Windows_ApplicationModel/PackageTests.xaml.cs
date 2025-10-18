using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel;
using Windows.System;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_ApplicationModel
{
	[SampleControlInfo("Windows.ApplicationModel", "Package", ignoreInSnapshotTests: true, description: "Tests the Package and PackageId properties")]

	public sealed partial class PackageTests : UserControl
	{
		public PackageTests()
		{
			this.InitializeComponent();

			AddProperties(Package.Current);
		}

		public void AddProperties(Package package)
		{
			SafeAdd("DisplayName", () => package.DisplayName);
			SafeAdd("Description", () => package.Description);
			SafeAdd("EffectiveExternalLocation", () => package.EffectiveExternalLocation?.Path);
			SafeAdd("EffectiveExternalPath", () => package.EffectiveExternalPath);
			SafeAdd("EffectiveLocation", () => package.EffectiveLocation.Path);
			SafeAdd("EffectivePath", () => package.EffectivePath);
			SafeAdd("Status", () => package.Status);
			SafeAdd("PublisherDisplayName", () => package.PublisherDisplayName);
			SafeAdd("InstallDate", () => package.InstallDate);
			SafeAdd("InstalledDate", () => package.InstalledDate);
			SafeAdd("InstalledLocation", () => package.InstalledLocation.Path);
			SafeAdd("InstalledPath", () => package.InstalledPath);
			SafeAdd("IsBundle", () => package.IsBundle);
			SafeAdd("IsDevelopmentMode", () => package.IsDevelopmentMode);
			SafeAdd("IsFramework", () => package.IsFramework);
			SafeAdd("IsOptional", () => package.IsOptional);
			SafeAdd("IsResourcePackage", () => package.IsResourcePackage);
			SafeAdd("IsStub", () => package.IsStub);
			SafeAdd("Logo", () => package.Logo);
			SafeAdd("MachineExternalLocation", () => package.MachineExternalLocation?.Path);
			SafeAdd("MachineExternalPath", () => package.MachineExternalPath);
			SafeAdd("MutableLocation", () => package.MutableLocation);
			SafeAdd("MutablePath", () => package.MutablePath);
			SafeAdd("SignatureKind", () => package.SignatureKind);
			SafeAdd("UserExternalLocation", () => package.UserExternalLocation?.Path);
			SafeAdd("UserExternalPath", () => package.UserExternalPath);
			SafeAdd("Id.Architecture", () => package.Id.Architecture);
			SafeAdd("Id.FamilyName", () => package.Id.FamilyName);
			SafeAdd("Id.FullName", () => package.Id.FullName);
			SafeAdd("Id.Name", () => package.Id.Name);
			SafeAdd("Id.Publisher", () => package.Id.Publisher);
			SafeAdd("Id.PublisherId", () => package.Id.PublisherId);
			SafeAdd("Id.ResourceId", () => package.Id.ResourceId);
			SafeAdd("Id.Version", () => $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Revision}.{package.Id.Version.Build}");			
		}

		public ObservableCollection<PackageProperty> Items { get; } = new ObservableCollection<PackageProperty>();

		private void SafeAdd(string propertyName, Func<object> propertyGetter)
		{
			try
			{
				var value = propertyGetter()?.ToString() ?? "(null)";
				Items.Add(new PackageProperty(propertyName, value));
			}
			catch (NotImplementedException)
			{
				Items.Add(new PackageProperty(propertyName, "Not implemented"));
			}
			catch (NotSupportedException)
			{
				Items.Add(new PackageProperty(propertyName, "Not supported"));
			}
			catch (Exception ex)
			{
				Items.Add(new PackageProperty(propertyName, $"Exception thrown - {ex.Message}"));
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

	public class PackageProperty
	{
		public PackageProperty(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public string Name { get; }

		public string Value { get; }
	}
}
