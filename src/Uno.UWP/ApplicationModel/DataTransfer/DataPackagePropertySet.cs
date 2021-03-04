#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI;

namespace Windows.ApplicationModel.DataTransfer
{
	/// <summary>
	/// Defines a set of properties to use with a DataPackage object.
	/// </summary>
	public partial class DataPackagePropertySet : IDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
		private readonly Lazy<IList<string>> _fileTypes;
		
		internal DataPackagePropertySet()
		{
			_fileTypes = new Lazy<IList<string>>(() =>
			{
				var fileTypes = new NonNullList<string>();
				SetValue(fileTypes, false, nameof(FileTypes));
				return fileTypes;
			});
		}

		/// <summary>
		/// Gets or sets the text that displays as a title for the contents of the DataPackage object.
		/// </summary>
		public string Title
		{
			get => GetValue<string?>() ?? string.Empty;
			set => SetValue(value, false);
		}

		/// <summary>
		/// Gets or sets a thumbnail image for the DataPackage.
		/// </summary>
		public IRandomAccessStreamReference? Thumbnail
		{
			get => GetValue<IRandomAccessStreamReference?>();
			set => SetValue(value);
		}

		/// <summary>
		/// Gets or sets text that describes the contents of the DataPackage.
		/// </summary>
		public string Description
		{
			get => GetValue<string?>() ?? string.Empty;
			set => SetValue(value, false);
		}

		/// <summary>
		/// Gets or sets the name of the app that created the DataPackage object.
		/// </summary>
		public string ApplicationName
		{
			get => GetValue<string?>() ?? string.Empty;
			set => SetValue(value, false);
		}

		/// <summary>
		/// Gets or sets the Uniform Resource Identifier (URI) of the app's location in the store.
		/// </summary>
		public Uri? ApplicationListingUri
		{
			get => GetValue<Uri?>();
			set => SetValue(value);
		}

		/// <summary>
		/// Specifies a vector object that contains the types of files stored in the DataPackage object.
		/// </summary>
		public IList<string> FileTypes => _fileTypes.Value;

		/// <summary>
		/// Gets or sets the source app's logo.
		/// </summary>
		public IRandomAccessStreamReference? Square30x30Logo
		{
			get => GetValue<IRandomAccessStreamReference?>();
			set => SetValue(value);
		}

		/// <summary>
		/// Gets or sets the package family name of the source app.
		/// </summary>
		public string PackageFamilyName
		{
			get => GetValue<string?>() ?? string.Empty;
			set => SetValue(value, false);
		}

		/// <summary>
		/// Gets or sets a background color for the sharing app's Square30x30Logo.
		/// </summary>
		public Color LogoBackgroundColor
		{
			get => GetValue<Color?>() ?? Colors.Black;
			set => SetValue(value);
		}

		/// <summary>
		/// Provides a web link to shared content that's currently displayed in the app.
		/// </summary>
		public Uri? ContentSourceWebLink
		{
			get => GetValue<Uri?>();
			set => SetValue(value);
		}

		/// <summary>
		/// Gets or sets the application link to the content from the source app.
		/// </summary>
		public Uri? ContentSourceApplicationLink
		{
			get => GetValue<Uri?>();
			set => SetValue(value);
		}

		/// <summary>
		/// Gets or sets the enterprise identity (see Enterprise data protection).
		/// </summary>
		public string EnterpriseId
		{
			get => GetValue<string?>() ?? string.Empty;
			set => SetValue(value, false);
		}

		/// <summary>
		/// Gets or sets the UserActivity in serialized JSON format to be shared with another app.
		/// </summary>
		public string ContentSourceUserActivityJson
		{
			get => GetValue<string?>() ?? string.Empty;
			set => SetValue(value, false);
		}

		/// <summary>
		/// Gets the number of items that are contained in the property set.
		/// </summary>
		public uint Size => (uint)_values.Count;

		public void Add(string key, object value) => _values.Add(key, value);

		public bool ContainsKey(string key) => _values.ContainsKey(key);

		public bool Remove(string key) => _values.Remove(key);

		public bool TryGetValue(string key, out object value) => _values.TryGetValue(key, out value!);

		public object this[string key]
		{
			get => _values[key];
			set => _values[key] = value;
		}

		public ICollection<string> Keys
		{
			get => _values.Keys;
			set => throw new InvalidOperationException("Setting Keys is not allowed.");
		}

		public ICollection<object> Values
		{
			get => _values.Values;
			set => throw new InvalidOperationException("Setting Values is not allowed.");
		}

		public void Add(KeyValuePair<string, object> item) => _values.Add(item.Key, item.Value);

		/// <summary>
		/// Removes all items from the property set.
		/// </summary>
		public void Clear() => _values.Clear();

		public bool Contains(KeyValuePair<string, object> item) => _values.TryGetValue(item.Key, out var value) && value == item.Value;

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (arrayIndex + _values.Count > array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			}

			var currentIndex = arrayIndex;
			foreach (var item in _values)
			{
				array[currentIndex] = item;
				currentIndex++;
			}
		}

		public bool Remove(KeyValuePair<string, object> item) => _values.Remove(item.Key);

		public int Count
		{
			get => _values.Count;
			set => throw new InvalidOperationException("Setting Count is not allowed.");
		}

		public bool IsReadOnly
		{
			get => false;
			set => throw new InvalidOperationException("Setting IsReadOnly is not allowed");
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private T GetValue<T>([CallerMemberName] string key = "")
		{
			if (_values.TryGetValue(key, out var value))
			{
				return (T)value;
			}
			return default!;
		}

		private void SetValue(object? value, bool allowNull = true, [CallerMemberName] string key = "")
		{
			if (!allowNull && value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (value != null)
			{
				_values[key] = value;
			}
			else
			{
				_values.Remove(key);
			}
		}
	}
}
