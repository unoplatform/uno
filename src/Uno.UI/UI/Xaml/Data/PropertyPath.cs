namespace Microsoft.UI.Xaml
{
	public sealed partial class PropertyPath
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

