using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Task = System.Threading.Tasks.Task;

namespace UnoSolutionTemplate
{
	// https://github.com/microsoft/VSProjectSystem/blob/4ad5716f8fca76403699b818c3d907ce8a8b9a38/doc/extensibility/IProjectGlobalPropertiesProvider.md
	[Export(typeof(IProjectGlobalPropertiesProvider))]
	[AppliesTo("")]
	public class UnoGlobalPropertiesProvider :
		ProjectValueDataSourceBase<IImmutableDictionary<string, string>>,
		IProjectGlobalPropertiesProvider
	{
#if DEBUG
		static UnoGlobalPropertiesProvider()
		{
			// Note that the type ctor is invoked, but not the ctor, this generally means that the IProjectService
			// version referenced by the package is not using the proper version (has to match vs16 or vs15)
		}
#endif

		/// <summary>
		/// A value that increments with each new map of properties.
		/// </summary>
		private volatile IComparable version = 0L;

		/// <summary>
		/// The block to post to when publishing new values.
		/// </summary>
		private ITargetBlock<IProjectVersionedValue<IImmutableDictionary<string, string>>>? targetBlock;

		/// <summary>
		/// The backing field for the <see cref="SourceBlock"/> property.
		/// </summary>
		private IReceivableSourceBlock<IProjectVersionedValue<IImmutableDictionary<string, string>>>? publicBlock;

		/// <summary>
		/// Initializes a new instance of the <see cref="MyProjectGlobalPropertiesProvider"/> class.
		/// </summary>
		/// <param name="commonServices">The CPS common services.</param>
		[ImportingConstructor]
		public UnoGlobalPropertiesProvider(IProjectService service)
			: base(service.Services)
		{
		}

		/// <inheritdoc />
		public override NamedIdentity DataSourceKey { get; } = new NamedIdentity("UnoGlobalProperties");

		/// <inheritdoc />
		public override IComparable DataSourceVersion => this.version;

		/// <inheritdoc />
		public override IReceivableSourceBlock<IProjectVersionedValue<IImmutableDictionary<string, string>>> SourceBlock
		{
			get
			{
				this.EnsureInitialized();
				return this.publicBlock!;
			}
		}

		/// <summary>
		/// See <see cref="IProjectGlobalPropertiesProvider"/>
		/// </summary>
		public async Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering GetGlobalPropertiesAsync for: {0}", this.ToString()));

			var current = Empty.PropertiesMap;

			if (UnoPlatformPackage.GlobalFunctionProvider != null)
			{
				foreach (var props in await UnoPlatformPackage.GlobalFunctionProvider())
				{
					current = current.Add(props.Key, props.Value);
				}
			}

			return current;
		}

		/// <inheritdoc />
		protected override void Initialize()
		{
			base.Initialize();
			var broadcastBlock = new BroadcastBlock<IProjectVersionedValue<IImmutableDictionary<string, string>>>(
				null,
				new DataflowBlockOptions() { NameFormat = "UnoGlobalProperties: {1}" });

			this.publicBlock = broadcastBlock.SafePublicize();
			this.targetBlock = broadcastBlock;

			// Hook up some events, or dependencies, that calculate new properties and post to the target block as needed.
			// Posting to the target block with an incremented DataSourceVersion will trigger a new project evaluation with
			// your new properties.
			this.targetBlock.Post(
				new ProjectVersionedValue<IImmutableDictionary<string, string>>(
					ImmutableDictionary<string, string>.Empty,
					ImmutableDictionary<NamedIdentity, IComparable>.Empty.Add(this.DataSourceKey, this.DataSourceVersion)));
		}
	}
}
