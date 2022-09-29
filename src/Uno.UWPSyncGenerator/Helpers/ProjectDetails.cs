#nullable disable

using System;
using System.IO;
using Microsoft.Build.Execution;
using System.Linq;
using Microsoft.Build.Evaluation;
using Uno.Extensions;

namespace Uno.SourceGeneration.Host
{
	public class ProjectDetails
	{
		private Tuple<string, DateTime>[] _timeStamps;

		public string Configuration { get; internal set; }
		public ProjectInstance ExecutedProject { get; internal set; }

		public string IntermediatePath { get; internal set; }
		public Project LoadedProject { get; internal set; }
		public string[] References { get; internal set; }


		public void BuildImportsMap()
		{
			_timeStamps = LoadedProject
				.Imports
				.Select(i => Tuple.Create(i.ImportedProject.FullPath, File.GetLastWriteTime(i.ImportedProject.FullPath)))
				.Concat(new Tuple<string, DateTime>(ExecutedProject.FullPath, File.GetLastWriteTime(ExecutedProject.FullPath)))
				.OrderBy(t => t.Item1)
				.ToArray();
		}

		public bool HasChanged()
		{
			var updatedStamps = _timeStamps
			.Select(t => File.Exists(t.Item1) ? File.GetLastWriteTime(t.Item1) : default(DateTime));

			return !updatedStamps.SequenceEqual(_timeStamps.Select(t => t.Item2));
		}
	}

}
