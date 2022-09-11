#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.Utilities;
using UnoSolutionTemplate.Wizard.Forms;

namespace UnoSolutionTemplate.Wizard
{
	public class UnoSolutionWizard : IWizard
	{
		private const string ProjectKinds_vsProjectKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
		private readonly bool _enableNuGetConfig;
		private readonly string _vsSuffix;
		private string? _targetPath;
		private string? _projectName;
		private DTE2? _dte;
		private IServiceProvider? _visualStudioServiceProvider;
		private bool _useWebAssembly;
		private bool _useiOS;
		private bool _useAndroid;
		private bool _useCatalyst;
		private bool _useAppKit;
		private bool _useGtk;
		private bool _useFramebuffer;
		private bool _useWpf;
		private bool _useWinUI;
		private IDictionary<string, string>? _replacementDictionary;

		public UnoSolutionWizard(bool enableNuGetConfig, string vsSuffix)
		{
			_enableNuGetConfig = enableNuGetConfig;
			_vsSuffix = vsSuffix;
		}

		protected IServiceProvider VisualStudioServiceProvider
		{
			get
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				if (_visualStudioServiceProvider == null)
				{
					_visualStudioServiceProvider = _dte as IServiceProvider;
					if (_visualStudioServiceProvider == null)
					{
						var serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider?)((_dte is Microsoft.VisualStudio.OLE.Interop.IServiceProvider) ? _dte : null);
						_visualStudioServiceProvider = (IServiceProvider)new Microsoft.VisualStudio.Shell.ServiceProvider(serviceProvider);
					}
				}
				return _visualStudioServiceProvider;
			}
		}

		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_dte?.Solution is Solution2 solution)
			{
				var platforms = solution.Projects.OfType<Project>().FirstOrDefault(p => p.Name == "Platforms");

				if (platforms.Object is SolutionFolder platformsFolder)
				{
					if (_useWebAssembly)
					{
						GenerateProject(solution, platformsFolder, $"{_projectName}.Wasm", "Wasm.winui.net6.vstemplate");
					}

					if (_useiOS || _useCatalyst || _useAndroid || _useAppKit)
					{
						GenerateProject(solution, platformsFolder, $"{_projectName}.Mobile", "Mobile.winui.net6.vstemplate");
					}

					if (_useGtk)
					{
						GenerateProject(solution, platformsFolder, $"{_projectName}.Skia.Gtk", "SkiaGtk.winui.net6.vstemplate");
					}

					if (_useFramebuffer)
					{
						GenerateProject(solution, platformsFolder, $"{_projectName}.Skia.Linux.FrameBuffer", "SkiaLinuxFrameBuffer.winui.net6.vstemplate");
					}

					if (_useWinUI)
					{
						GenerateProject(solution, platformsFolder, $"{_projectName}.Windows", "WinUI.net6.vstemplate");
					}

					if (_useWpf)
					{
						GenerateProject(solution, platformsFolder, $"{_projectName}.Skia.Wpf", "SkiaWpf.winui.net6.vstemplate");
					}
				}
				else
				{
					throw new InvalidOperationException("Unable to find the Platforms solution folder");
				}
			}
		}

		private void GenerateProject(Solution2 solution, SolutionFolder platformsFolder, string projectFullName, string templateName)
		{
			if(_projectName.Contains(" "))
			{
				throw new Exception("The project name should not contain spaces");
			}

			if (_projectName != null)
			{
				var targetPath = Path.Combine(_targetPath, projectFullName);

				// Duplicate the template to add custom parameters
				var workTemplateFilePath = DuplicateTemplate(solution, templateName);
				AdjustCustomParameters(workTemplateFilePath, _projectName);

				platformsFolder.AddFromTemplate(workTemplateFilePath, targetPath, projectFullName);
			}
			else
			{
				throw new InvalidOperationException("Project name is not set");
			}
		}

		private void AdjustCustomParameters(string templateFilePath, string projectName)
		{
			const string defaultNS = "http://schemas.microsoft.com/developer/vstemplate/2005";

			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(templateFilePath));
			var nsManager = new XmlNamespaceManager(doc.NameTable);
			nsManager.AddNamespace("d", defaultNS);

			var customParametersNode = doc.SelectSingleNode("//d:CustomParameters", nsManager);
			if(customParametersNode == null)
			{
				var templateContent = doc.SelectSingleNode("//d:TemplateContent", nsManager);
				customParametersNode = doc.CreateElement("CustomParameters", defaultNS);
				templateContent.AppendChild(customParametersNode);
			}

			if (_replacementDictionary != null)
			{
				foreach (var replacement in _replacementDictionary)
				{
					var safeProjectNameNode = doc.CreateElement("CustomParameter", defaultNS);
					safeProjectNameNode.SetAttribute("Name", replacement.Key);
					safeProjectNameNode.SetAttribute("Value", replacement.Value);
					customParametersNode.AppendChild(safeProjectNameNode);
				}
			}

			doc.Save(templateFilePath);
		}

		private static string DuplicateTemplate(Solution2 solution, string TemplateName)
		{
			var templateFilePath = solution.GetProjectTemplate(TemplateName, "CSharp");
			var templatePath = Path.GetDirectoryName(templateFilePath);
			var workTemplatePath = Path.Combine(Path.GetTempPath(), $"uno-template-" + Guid.NewGuid().ToString());

			foreach (var file in Directory.GetFiles(templatePath, "*.*", SearchOption.AllDirectories))
			{
				var targetFilePath = file.Replace(templatePath, workTemplatePath);

				Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
				File.Copy(file, targetFilePath);
			}

			return Path.Combine(workTemplatePath, Path.GetFileName(templateFilePath));
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

		public void RunFinished()
		{
			var vsConfigPath = Path.Combine(_targetPath, "..\\.vsconfig");

			if (!File.Exists(vsConfigPath))
			{
				using var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream(FindManifestFileName($".vsconfig.{_vsSuffix}")));
				File.WriteAllText(vsConfigPath, reader.ReadToEnd());
			}

			var nugetConfigPath = Path.Combine(_targetPath, "..\\NuGet.config");

			if (_enableNuGetConfig && !File.Exists(nugetConfigPath))
			{
				using var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream(FindManifestFileName("NuGet-netcore.config")));
				File.WriteAllText(nugetConfigPath, reader.ReadToEnd());
			}

			OpenWelcomePage();
			SetStartupProject();
			SetBuildableAndDeployable();
			SetDefaultConfiguration();
		}

		private string FindManifestFileName(string fileName)
			=> GetType().Assembly.GetManifestResourceNames().FirstOrDefault(f => f.EndsWith("." + fileName))
				?? throw new InvalidOperationException($"Unable to find [{fileName}] in the assembly");

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			_targetPath = replacementsDictionary["$destinationdirectory$"];
			_projectName = replacementsDictionary["$projectname$"];

			ValidateProjectName();

			if (runKind == WizardRunKind.AsMultiProject)
			{
				_dte = (DTE2)automationObject;
			}

			using (DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware))
			{
				using DialogParentWindow owner = new DialogParentWindow(IntPtr.Zero, enableModeless: true, VisualStudioServiceProvider);
				using UnoOptions targetPlatformWizardPicker = new UnoOptions(VisualStudioServiceProvider);

				switch (targetPlatformWizardPicker.ShowDialog(owner))
				{
					case DialogResult.OK:
						_useWebAssembly = targetPlatformWizardPicker.UseWebAssembly;
						_useiOS = targetPlatformWizardPicker.UseiOS;
						_useAndroid = targetPlatformWizardPicker.UseAndroid;
						_useCatalyst = targetPlatformWizardPicker.UseCatalyst;
						_useAppKit = targetPlatformWizardPicker.UseAppKit;
						_useGtk = targetPlatformWizardPicker.UseGtk;
						_useFramebuffer = targetPlatformWizardPicker.UseFramebuffer;
						_useWpf = targetPlatformWizardPicker.UseWpf;
						_useWinUI = targetPlatformWizardPicker.UseWinUI;

						replacementsDictionary["$UseWebAssembly$"] = _useWebAssembly.ToString();
						replacementsDictionary["$UseIOS$"] = _useiOS.ToString();
						replacementsDictionary["$UseAndroid$"] = _useAndroid.ToString();
						replacementsDictionary["$UseCatalyst$"] = _useCatalyst.ToString();
						replacementsDictionary["$UseAppKit$"] = _useAppKit.ToString();
						replacementsDictionary["$UseGtk$"] = _useGtk.ToString();
						replacementsDictionary["$UseFrameBuffer$"] = _useFramebuffer.ToString();
						replacementsDictionary["$UseWPF$"] = _useWpf.ToString();
						replacementsDictionary["$UseWinUI$"] = _useWinUI.ToString();
						replacementsDictionary["$ext_safeprojectname$"] = replacementsDictionary["$safeprojectname$"];

						_replacementDictionary = replacementsDictionary.ToDictionary(p => p.Key, p => p.Value);

						var version = GetVisualStudioReleaseVersion();

						if(version < new Version(17, 3) && (_useiOS || _useAndroid || _useCatalyst || _useAppKit))
						{
							MessageBox.Show("iOS, Android, Mac Catalyst, and mac AppKit are only supported starting from Visual Studio 17.3 Preview 1 or later.", "Unable to create the solution");
							throw new WizardCancelledException();
						}

						break;

					case DialogResult.Abort:
						MessageBox.Show("Aborted"/*targetPlatformWizardPicker.Error*/);
						throw new WizardCancelledException();

					default:
						throw new WizardBackoutException();
				}
			}
		}

		private void ValidateProjectName()
		{
			const string ErrorCaption = "Error creating the project";

			if (_projectName != null)
			{
				if (_projectName.Contains(" "))
				{
					MessageBox.Show($"The project name should not be containing spaces", ErrorCaption);
					throw new WizardBackoutException();
				}

				if (_projectName.Contains("-"))
				{
					MessageBox.Show($"The project name should not be containing '-'", ErrorCaption);
					throw new WizardBackoutException();
				}

				if (char.IsDigit(_projectName.First()))
				{
					MessageBox.Show($"The project name should not start with a number", ErrorCaption);
					throw new WizardBackoutException();
				}
			}
		}

		public bool ShouldAddProjectItem(string filePath) => true;

		public Project[] GetAllProjects()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var list = new List<Project>();
			if (_dte != null)
			{
				var projects = _dte.Solution.Projects;
				var item = projects.GetEnumerator();
				while (item.MoveNext())
				{
					var project = item.Current as Project;
					if (project == null)
					{
						continue;
					}

					if (project.Kind == ProjectKinds_vsProjectKindSolutionFolder)
					{
						list.AddRange(GetSolutionFolderProjects(project));
					}
					else
					{
						list.Add(project);
					}
				}
			}

			return list.ToArray();
		}

		private void OpenWelcomePage()
			=> _dte?.ItemOperations.Navigate("https://platform.uno/docs/articles/get-started-wizard.html", vsNavigateOptions.vsNavigateOptionsNewWindow);

		private void SetStartupProject()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				if (_dte?.Solution.SolutionBuild is SolutionBuild2 val)
				{
					var startupProject = GetAllProjects().FirstOrDefault(s => s.Name.EndsWith(".Windows", StringComparison.OrdinalIgnoreCase));

					if (startupProject is { })
					{
						val.StartupProjects = startupProject.UniqueName;
					}

					var x86Config = val.SolutionConfigurations
						.Cast<SolutionConfiguration2>()
						.FirstOrDefault(c => c.Name == "Debug" && c.PlatformName == "x86");

					x86Config?.Activate();
				}
				else
				{
					// Unable to set the startup project when running from RunFinished since VS 2022 17.2 Preview 2
					// throw new InvalidOperationException();
				}
			}
			catch (Exception)
			{
			}
		}

		private void SetBuildableAndDeployable()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_dte?.Solution.SolutionBuild is SolutionBuild2 solutionBuild)
			{
				SetConfiurationDeployable(solutionBuild, "Debug", "Any CPU", ".Windows.csproj");
				SetConfiurationDeployable(solutionBuild, "Release", "Any CPU", ".Windows.csproj");

				SetConfiurationDeployable(solutionBuild, "Debug", "Any CPU", ".Mobile.csproj");
				SetConfiurationDeployable(solutionBuild, "Release", "Any CPU", ".Mobile.csproj");
			}
		}

		private static void SetConfiurationDeployable(SolutionBuild2 val, string configuration, string platformName, string projectSuffix)
		{
			try
			{
				var anyCpuConfig = val.SolutionConfigurations
										.Cast<SolutionConfiguration2>()
										.FirstOrDefault(c => c.Name == configuration && c.PlatformName == platformName);

				if (anyCpuConfig != null)
				{
					foreach (SolutionConfiguration2 solutionConfiguration2 in val.SolutionConfigurations)
					{
						foreach (SolutionContext solutionContext in anyCpuConfig.SolutionContexts)
						{
							if (solutionContext.ProjectName.EndsWith(projectSuffix))
							{
								solutionContext.ShouldBuild = true;
								solutionContext.ShouldDeploy = true;
							}
						}
					}
				}
			}
			catch (Exception)
			{
			}
		}

		private void SetDefaultConfiguration()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				if (_dte?.Solution.SolutionBuild is SolutionBuild2 val)
				{
						var x86Config = val.SolutionConfigurations
							.Cast<SolutionConfiguration2>()
							.FirstOrDefault(c => c.Name == "Debug" && c.PlatformName == "x86");

						x86Config?.Activate();
				}
				else
				{
					// Unable to set the startup project when running from RunFinished since VS 2022 17.2 Preview 2
					// throw new InvalidOperationException();
				}
			}
			catch (Exception)
			{
			}
		}

		private Project[] GetSolutionFolderProjects(Project solutionFolder)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var list = new List<Project>();
			for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
			{
				var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
				if (subProject == null)
				{
					continue;
				}

				// If this is another solution folder, do a recursive call, otherwise add
				if (subProject.Kind == ProjectKinds_vsProjectKindSolutionFolder)
				{
					list.AddRange(GetSolutionFolderProjects(subProject));
				}
				else
				{
					list.Add(subProject);
				}
			}

			return list.ToArray();
		}

		private Version? GetVisualStudioReleaseVersion()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_visualStudioServiceProvider?.GetService(typeof(SVsShell)) is IVsShell service)
			{
				if (service.GetProperty(-9068, out var releaseVersion) != 0)
				{
					return null;
				}
				if (releaseVersion is string releaseVersionAsText && Version.TryParse(releaseVersionAsText.Split(' ')[0], out var result))
				{
					return result;
				}
			}

			return null;
		}

	}
}
