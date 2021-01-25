using System;
using System.IO;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;
using System.Reflection;

namespace CustomHost
{
	//The text template transformation engine is responsible for running   
	//the transformation process.  
	//The host is responsible for all input and output, locating files,   
	//and anything else related to the external environment.  
	//-------------------------------------------------------------------------
	class CustomCmdLineHost : MarshalByRefObject, ITextTemplatingEngineHost
	{
		//the path and file name of the text template that is being processed  
		//---------------------------------------------------------------------  
		internal string TemplateFileValue;
		public string TemplateFile
		{
			get { return TemplateFileValue; }
		}
		//This will be the extension of the generated text output file.  
		//The host can provide a default by setting the value of the field here.  
		//The engine can change this value based on the optional output directive  
		//if the user specifies it in the text template.  
		//---------------------------------------------------------------------  
		private string fileExtensionValue = ".txt";
		public string FileExtension
		{
			get { return fileExtensionValue; }
		}
		//This will be the encoding of the generated text output file.  
		//The host can provide a default by setting the value of the field here.  
		//The engine can change this value based on the optional output directive  
		//if the user specifies it in the text template.  
		//---------------------------------------------------------------------  
		private Encoding fileEncodingValue = Encoding.UTF8;
		public Encoding FileEncoding
		{
			get { return fileEncodingValue; }
		}
		//These are the errors that occur when the engine processes a template.  
		//The engine passes the errors to the host when it is done processing,  
		//and the host can decide how to display them. For example, the host   
		//can display the errors in the UI or write them to a file.  
		//---------------------------------------------------------------------  
		private CompilerErrorCollection errorsValue;
		public CompilerErrorCollection Errors
		{
			get { return errorsValue; }
		}
		//The host can provide standard assembly references.  
		//The engine will use these references when compiling and  
		//executing the generated transformation class.  
		//--------------------------------------------------------------  
		public IList<string> StandardAssemblyReferences
		{
			get
			{
				return new string[]
				{  
					//If this host searches standard paths and the GAC,  
					//we can specify the assembly name like this.  
					//---------------------------------------------------------  
					//"System"  

					//Because this host only resolves assemblies from the   
					//fully qualified path and name of the assembly,  
					//this is a quick way to get the code to give us the  
					//fully qualified path and name of the System assembly.  
					//---------------------------------------------------------  
					typeof(System.Uri).Assembly.Location,
					typeof(System.Linq.Enumerable).Assembly.Location,
				};
			}
		}
		//The host can provide standard imports or using statements.  
		//The engine will add these statements to the generated   
		//transformation class.  
		//--------------------------------------------------------------  
		public IList<string> StandardImports
		{
			get
			{
				return new string[]
				{
					"System"
				};
			}
		}
		//The engine calls this method based on the optional include directive  
		//if the user has specified it in the text template.  
		//This method can be called 0, 1, or more times.  
		//---------------------------------------------------------------------  
		//The included text is returned in the context parameter.  
		//If the host searches the registry for the location of include files,  
		//or if the host searches multiple locations by default, the host can  
		//return the final path of the include file in the location parameter.  
		//---------------------------------------------------------------------  
		public bool LoadIncludeText(string requestFileName, out string content, out string location)
		{
			content = System.String.Empty;
			location = System.String.Empty;

			if(!Path.IsPathRooted(requestFileName))
			{
				requestFileName = Path.Combine(Path.GetDirectoryName(TemplateFile), requestFileName.Replace('\\', Path.DirectorySeparatorChar));
			}

			//If the argument is the fully qualified path of an existing file,  
			//then we are done.  
			//----------------------------------------------------------------  
			if (File.Exists(requestFileName))
			{
				content = T4Generator.Tools.ConvertLineEndings(File.ReadAllText(requestFileName));

				return true;
			}
			//This can be customized to search specific paths for the file.  
			//This can be customized to accept paths to search as command line  
			//arguments.  
			//----------------------------------------------------------------  
			else
			{
				return false;
			}
		}
		//Called by the Engine to enquire about   
		//the processing options you require.   
		//If you recognize that option, return an   
		//appropriate value.   
		//Otherwise, pass back NULL.  
		//--------------------------------------------------------------------  
		public object GetHostOption(string optionName)
		{
			object returnObject;
			switch (optionName)
			{
				case "CacheAssemblies":
					returnObject = true;
					break;
				default:
					returnObject = null;
					break;
			}
			return returnObject;
		}
		//The engine calls this method to resolve assembly references used in  
		//the generated transformation class project and for the optional   
		//assembly directive if the user has specified it in the text template.  
		//This method can be called 0, 1, or more times.  
		//---------------------------------------------------------------------  
		public string ResolveAssemblyReference(string assemblyReference)
		{
			//If the argument is the fully qualified path of an existing file,  
			//then we are done. (This does not do any work.)  
			//----------------------------------------------------------------  
			if (File.Exists(assemblyReference))
			{
				return assemblyReference;
			}
			//Maybe the assembly is in the same folder as the text template that   
			//called the directive.  
			//----------------------------------------------------------------  
			string candidate = Path.Combine(Path.GetDirectoryName(this.TemplateFile), assemblyReference);
			if (File.Exists(candidate))
			{
				return candidate;
			}

			//This can be customized to search specific paths for the file  
			//or to search the GAC.  
			//----------------------------------------------------------------  
			//This can be customized to accept paths to search as command line  
			//arguments.  
			//----------------------------------------------------------------  
			//If we cannot do better, return the original file name.  
			return null;
		}
		//The engine calls this method based on the directives the user has   
		//specified in the text template.  
		//This method can be called 0, 1, or more times.  
		//---------------------------------------------------------------------  
		public Type ResolveDirectiveProcessor(string processorName)
		{
			//This host will not resolve any specific processors.  
			//Check the processor name, and if it is the name of a processor the   
			//host wants to support, return the type of the processor.  
			//---------------------------------------------------------------------  
			if (string.Compare(processorName, "XYZ", StringComparison.OrdinalIgnoreCase) == 0)
			{
				//return typeof();  
			}
			//This can be customized to search specific paths for the file  
			//or to search the GAC  
			//If the directive processor cannot be found, throw an error.  
			throw new Exception("Directive Processor not found");
		}
		//A directive processor can call this method if a file name does not   
		//have a path.  
		//The host can attempt to provide path information by searching   
		//specific paths for the file and returning the file and path if found.  
		//This method can be called 0, 1, or more times.  
		//---------------------------------------------------------------------  
		public string ResolvePath(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException("the file name cannot be null");
			}
			//If the argument is the fully qualified path of an existing file,  
			//then we are done  
			//----------------------------------------------------------------  
			if (File.Exists(fileName))
			{
				return fileName;
			}
			//Maybe the file is in the same folder as the text template that   
			//called the directive.  
			//----------------------------------------------------------------  
			string candidate = Path.Combine(Path.GetDirectoryName(this.TemplateFile), fileName.Replace('\\', Path.DirectorySeparatorChar));
			if (File.Exists(candidate))
			{
				return candidate;
			}
			//Look more places.  
			//----------------------------------------------------------------  
			//More code can go here...  
			//If we cannot do better, return the original file name.  
			return fileName;
		}
		//If a call to a directive in a text template does not provide a value  
		//for a required parameter, the directive processor can try to get it  
		//from the host by calling this method.  
		//This method can be called 0, 1, or more times.  
		//---------------------------------------------------------------------  
		public string ResolveParameterValue(string directiveId, string processorName, string parameterName)
		{
			if (directiveId == null)
			{
				throw new ArgumentNullException("the directiveId cannot be null");
			}
			if (processorName == null)
			{
				throw new ArgumentNullException("the processorName cannot be null");
			}
			if (parameterName == null)
			{
				throw new ArgumentNullException("the parameterName cannot be null");
			}
			//Code to provide "hard-coded" parameter values goes here.  
			//This code depends on the directive processors this host will interact with.  
			//If we cannot do better, return the empty string.  
			return String.Empty;
		}
		//The engine calls this method to change the extension of the   
		//generated text output file based on the optional output directive   
		//if the user specifies it in the text template.  
		//---------------------------------------------------------------------  
		public void SetFileExtension(string extension)
		{
			//The parameter extension has a '.' in front of it already.  
			//--------------------------------------------------------  
			fileExtensionValue = extension;
		}
		//The engine calls this method to change the encoding of the   
		//generated text output file based on the optional output directive   
		//if the user specifies it in the text template.  
		//----------------------------------------------------------------------  
		public void SetOutputEncoding(System.Text.Encoding encoding, bool fromOutputDirective)
		{
			fileEncodingValue = encoding;
		}
		//The engine calls this method when it is done processing a text  
		//template to pass any errors that occurred to the host.  
		//The host can decide how to display them.  
		//---------------------------------------------------------------------  
		public void LogErrors(CompilerErrorCollection errors)
		{
			errorsValue = errors;
		}
		//This is the application domain that is used to compile and run  
		//the generated transformation class to create the generated text output.  
		//----------------------------------------------------------------------  
		public AppDomain ProvideTemplatingAppDomain(string content)
		{
			//This host will provide a new application domain each time the   
			//engine processes a text template.  
			//-------------------------------------------------------------  
			return AppDomain.CreateDomain("Generation App Domain");
			//This could be changed to return the current appdomain, but new   
			//assemblies are loaded into this AppDomain on a regular basis.  
			//If the AppDomain lasts too long, it will grow indefinitely,   
			//which might be regarded as a leak.  
			//This could be customized to cache the application domain for   
			//a certain number of text template generations (for example, 10).  
			//This could be customized based on the contents of the text   
			//template, which are provided as a parameter for that purpose.  
		}
	}
}
