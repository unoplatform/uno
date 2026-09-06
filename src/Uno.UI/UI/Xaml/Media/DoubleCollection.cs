using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.UI.Xaml.Media
{
	public partial class DoubleCollection : IList<double>, IEnumerable<double>
	{
		private readonly List<double> _values;

		public DoubleCollection()
		{
			_values = new List<double>();
		}

		public DoubleCollection(IEnumerable<double> collection)
		{
			_values = collection.ToList();
		}

		public int Count => _values.Count;

		public bool IsReadOnly => false;

		public double this[int index]
		{
			get => _values[index];
			set => _values[index] = value;
		}

		public IEnumerator<double> GetEnumerator()
			=> _values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> _values.GetEnumerator();

		public int IndexOf(double item)
			=> _values.IndexOf(item);

		public bool Contains(double item)
			=> _values.Contains(item);

		public void CopyTo(double[] array, int arrayIndex)
			=> _values.CopyTo(array, arrayIndex);

		public void Insert(int index, double item)
			=> _values.Insert(index, item);

		public void RemoveAt(int index)
			=> _values.RemoveAt(index);

		public void Add(double item)
			=> _values.Add(item);

		public void Clear()
			=> _values.Clear();

		public bool Remove(double item)
			=> _values.Remove(item);

		static public implicit operator DoubleCollection(string value)
		{
			return value.Split(',', ' ').Select(str => double.Parse(str, CultureInfo.InvariantCulture)).ToArray();
		}

		static public implicit operator DoubleCollection(double[] value)
		{
			return new DoubleCollection(value);
		}
	}
}
