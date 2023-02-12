using System.Collections.Generic;

namespace Mono.TextTemplating
{
	class ProcessSettings
	{
		private string outputFile;
		private string inputFile;
		private Dictionary<string, string> properties;
		private string preprocessClassName;
		private bool debug;
		private bool verbose;
		private bool noPreprocessingHelpers;
		private string inputContent;
		private bool writeToStdout;
		private bool isDefaultOutputFilename;

		public string OutputFile { get => outputFile; set => outputFile = value; }
		public string InputFile { get => inputFile; set => inputFile = value; }
		public Dictionary<string, string> Properties { get => properties; set => properties = value; }
		public string PreprocessClassName { get => preprocessClassName; set => preprocessClassName = value; }
		public bool Debug { get => debug; set => debug = value; }
		public bool Verbose { get => verbose; set => verbose = value; }
		public bool NoPreprocessingHelpers { get => noPreprocessingHelpers; set => noPreprocessingHelpers = value; }
		public string InputContent { get => inputContent; set => inputContent = value; }
		public bool WriteToStdout { get => writeToStdout; set => writeToStdout = value; }
		public bool IsDefaultOutputFilename { get => isDefaultOutputFilename; set => isDefaultOutputFilename = value; }

	}


}