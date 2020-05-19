using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	public partial class PointCollection : IEnumerable<Point>, IList<Point>
	{
		private List<Point> _coords;

		public PointCollection()
		{
			_coords = new List<Point>();
		}

		public PointCollection(IEnumerable<Point> coordinates)
		{
			_coords = coordinates.ToList();
		}

		public int Count => _coords.Count;

		public bool IsReadOnly => false;

		public Point this[int i]
		{
			get => _coords[i];
			set => _coords[i] = value;
		}

		private static readonly char[] pointsParsingSeparators = new char[] { ',', ' ' };

		public static implicit operator PointCollection(string s)
		{
			var fields = s.Split(pointsParsingSeparators, options: StringSplitOptions.RemoveEmptyEntries);

			//Do we have the correct number of coordinate values (an even number, so that each X has a Y)?
			if (fields.Length % 2 != 0)
			{
				return null;
			}

			//Are they all readable as floats?
			bool succesfulConversion = true;

			var values = fields
				.Select(strVal =>
				{
					succesfulConversion &= float.TryParse(strVal, out var v);
					return v;
				})
				.ToArray();

			if (!succesfulConversion)
			{
				return null;
			}

			// Construct PointCollection
			var points = new List<Point>();

			for (int i = 0; i < values.Length; i += 2)
			{
				points.Add(new Point { X = values[i], Y = values[i + 1] });
			}

			return new PointCollection(points);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("[");
			foreach (Point p in _coords)
			{
				sb.Append(p.X + "," + p.Y + " ");
			}
			sb.Append("]");

			return sb.ToString();
		}

		public IEnumerator<Point> GetEnumerator()
		{
			return ((IEnumerable<Point>)_coords).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Point>)_coords).GetEnumerator();
		}

		public int IndexOf(Point item) => _coords.IndexOf(item);

		public void Insert(int index, Point item) => _coords.Insert(index, item);

		public void RemoveAt(int index) => _coords.RemoveAt(index);

		public void Add(Point item) => _coords.Add(item);

		public void Clear() => _coords.Clear();

		public bool Contains(Point item) => _coords.Contains(item);

		public void CopyTo(Point[] array, int arrayIndex) => _coords.CopyTo(array, arrayIndex);

		public bool Remove(Point item) => _coords.Remove(item);
	}
}
