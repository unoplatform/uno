#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.Storage.Streams;
using Windows.UI;

namespace Windows.ApplicationModel.DataTransfer
{
	/// <summary>
	/// Gets the set of properties of a DataPackageView object.
	/// </summary>
	public partial class DataPackagePropertySetView : IReadOnlyDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
	{
		private DataPackagePropertySet _propertySet;

		internal DataPackagePropertySetView(DataPackagePropertySet propertySet)
		{
			_propertySet = propertySet ?? throw new ArgumentNullException(nameof(propertySet));
		}

		/// <summary>
		/// Gets the Uniform Resource Identifier (URI) of the app's location in the Microsoft Store.
		/// </summary>
		public Uri? ApplicationListingUri => _propertySet.ApplicationListingUri;

		/// <summary>
		/// Gets the name of the app that created the DataPackage object.
		/// </summary>
		public string ApplicationName => _propertySet.ApplicationName;

		/// <summary>
		/// Gets the text that describes the contents of the DataPackage.
		/// </summary>
		public string Description => _propertySet.Description;

		/// <summary>
		/// Gets a vector object that contains the types of files stored in the DataPackage object.
		/// </summary>
		public IReadOnlyList<string> FileTypes => (IReadOnlyList<string>)_propertySet.FileTypes;

		/// <summary>
		/// Gets the thumbnail image for the DataPackageView.
		/// </summary>
		/// <remarks>
		/// In UWP this property actually is a RandomAccessStreamReference even though the other properties use IRandomAccessStreamReference.
		/// </remarks>
		public RandomAccessStreamReference? Thumbnail => _propertySet.Thumbnail as RandomAccessStreamReference; 

		/// <summary>
		/// Gets the text that displays as a title for the contents of the DataPackagePropertySetView object.
		/// </summary>
		public string Title => _propertySet.Title;

		/// <summary>
		/// Gets the application link to the content from the source app.
		/// </summary>
		public Uri? ContentSourceApplicationLink => _propertySet.ContentSourceApplicationLink;

		/// <summary>
		/// Gets a web link to shared content that's currently displayed in the app.
		/// </summary>
		public Uri? ContentSourceWebLink => _propertySet.ContentSourceWebLink;

		/// <summary>
		/// Gets a background color for the sharing app's Square30x30Logo.
		/// </summary>
		public Color LogoBackgroundColor => _propertySet.LogoBackgroundColor;

		/// <summary>
		/// Gets the package family name of the source app.
		/// </summary>
		public string PackageFamilyName => _propertySet.PackageFamilyName;

		/// <summary>
		/// Gets the source app's logo.
		/// </summary>
		public IRandomAccessStreamReference? Square30x30Logo => _propertySet.Square30x30Logo;

		/// <summary>
		/// Gets or sets the enterprise Id.
		/// </summary>
		public string EnterpriseId => _propertySet.EnterpriseId;

		/// <summary>
		/// Gets the UserActivity in serialized JSON format to be shared with another app.
		/// </summary>
		public string ContentSourceUserActivityJson => _propertySet.ContentSourceUserActivityJson;

		/// <summary>
		/// Gets a value that indicates whether the shared content in the DataPackageView comes
		/// from clipboard data that was synced from another device for the current user.
		/// </summary>
		/// <remarks>Always false in Uno Platform.</remarks>
		public bool IsFromRoamingClipboard => false;

		/// <summary>
		/// Gets the number of items that are contained in the property set.
		/// </summary>
		public uint Size => _propertySet.Size;

		public bool ContainsKey(string key) => _propertySet.ContainsKey(key);

		public bool TryGetValue(string key, out object value) => _propertySet.TryGetValue(key, out value);

		public object this[string key]
		{
			get => _propertySet[key];
			set => throw new InvalidOperationException("Cannot modify data");
		}

		public IEnumerable<string> Keys
		{
			get => _propertySet.Keys;
			set => throw new InvalidOperationException("Cannot modify keys");
		}
		public IEnumerable<object> Values
		{
			get => _propertySet.Values;
			set => throw new InvalidOperationException("Cannot modify values");
		}
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _propertySet.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count
		{
			get => _propertySet.Count;
			set => throw new InvalidOperationException("Cannot set count");
		}
	}
}
