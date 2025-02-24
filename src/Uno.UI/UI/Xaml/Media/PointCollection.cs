using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class PointCollection : IEnumerable<Point>, IList<Point>
	{
		private readonly List<Point> _points;
		private readonly List<Action> _changedCallbacks = new List<Action>(1);

		public PointCollection()
		{
			_points = new List<Point>();
		}

		public PointCollection(IEnumerable<Point> coordinates)
		{
			_points = coordinates.ToList();
		}

		// For implicit conversion from string, avoids to uselessly clone the points list.
		private PointCollection(List<Point> points)
		{
			_points = points;
		}

		public int Count => _points.Count;

		public bool IsReadOnly => false;

		public Point this[int i]
		{
			get => _points[i];
			set
			{
				_points[i] = value;
				NotifyChanged();
			}
		}

		public IEnumerator<Point> GetEnumerator()
			=> ((IEnumerable<Point>)_points).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable<Point>)_points).GetEnumerator();

		public int IndexOf(Point item)
			=> _points.IndexOf(item);

		public bool Contains(Point item)
			=> _points.Contains(item);

		public void CopyTo(Point[] array, int arrayIndex)
			=> _points.CopyTo(array, arrayIndex);

		public void Insert(int index, Point item)
		{
			_points.Insert(index, item);
			NotifyChanged();
		}

		public void RemoveAt(int index)
		{
			_points.RemoveAt(index);
			NotifyChanged();
		}

		public void Add(Point item)
		{
			_points.Add(item);
			NotifyChanged();
		}

		public void Clear()
		{
			if (_points.Count > 0)
			{
				_points.Clear();
				NotifyChanged();
			}
		}

		public bool Remove(Point item)
		{
			if (_points.Remove(item))
			{
				NotifyChanged();
				return true;
			}
			else
			{
				return false;
			}
		}

		internal void RegisterChangedListener(Action listener)
			=> _changedCallbacks.Add(listener);
		internal void UnRegisterChangedListener(Action listener)
			=> _changedCallbacks.Remove(listener);

		private void NotifyChanged()
		{
			foreach (var callback in _changedCallbacks)
			{
				callback();
			}
		}

		private static readonly char[] pointsParsingSeparators = { ',', ' ' };
		public static implicit operator PointCollection(string s)
		{
			var fields = s.Split(pointsParsingSeparators, options: StringSplitOptions.RemoveEmptyEntries);

			//Do we have the correct number of coordinate values (an even number, so that each X has a Y)?
			if (fields.Length % 2 != 0)
			{
				return null;
			}

			//Are they all readable as floats?
			bool successfulConversion = true;

			var values = fields
				.SelectToArray(strVal =>
				{
					successfulConversion &= float.TryParse(strVal, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var v);
					return v;
				});

			if (!successfulConversion)
			{
				return null;
			}

			// Construct PointCollection
			var points = new List<Point>(values.Length);

			for (int i = 0; i < values.Length; i += 2)
			{
				points.Add(new Point { X = values[i], Y = values[i + 1] });
			}

			return new PointCollection(points);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.Append('[');
			foreach (Point p in _points)
			{
				sb.Append(p.X + "," + p.Y + " ");
			}
			sb.Append(']');

			return sb.ToString();
		}
	}
}
