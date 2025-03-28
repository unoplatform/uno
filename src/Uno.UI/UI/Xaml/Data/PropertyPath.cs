namespace Windows.UI.Xaml
{
	public partial class PropertyPath
	{
		/// <summary>
		/// Initializes a new instance of the PropertyPath class based on a path string.
		/// </summary>
		/// <param name="path">Path.</param>
		public PropertyPath(string path)
		{
			_path = CleanupPath(path);
		}

		public static implicit operator PropertyPath(string path)
		{
			return new PropertyPath(path);
		}

		public static implicit operator string(PropertyPath path)
		{
			if (path != null)
			{
				return path.Path;
			}
			else
			{
				// An null path is an empty string, particularly when initializing a simple binding.
				// This is similar to the cleanup made in the CleanupPath method.
				return "";
			}
		}

		static string CleanupPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return "";
			}

			path = path
				.Replace("[", ".[")
				.Replace("..", ".");

			return path;
		}

		readonly string _path;

		/// <summary>
		/// Gets the path value held by this PropertyPath.
		/// </summary>
		/// <value>The path.</value>
		public string Path
		{
			get
			{
				return _path;
			}
		}

		/// <inheritdoc />
		public override string ToString()
			=> _path;
	}
}

