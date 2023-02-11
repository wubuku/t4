//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TextTemplating;
using Mono.Options;
using T4Toolbox.VSHostLites;

namespace Mono.TextTemplating
{
	class TextTransform
	{
		static OptionSet optionSet;

		public static int Main (string [] args)
		{
			try {
				return MainInternal (args);
			}
			catch (Exception e) {
				Console.Error.WriteLine (e);
				return -1;
			}
		}

		sealed class CustomOption : Option
		{
			readonly Action<OptionValueCollection> action;
			public CustomOption (string prototype, string description, int count, Action<OptionValueCollection> action, bool hidden = false)
				: base (prototype, description, count, hidden)
				=> this.action = action ?? throw new ArgumentNullException (nameof (action));
			protected override void OnParseComplete (OptionContext c) => action (c.OptionValues);
		}

		static int MainInternal (string [] args)
		{
			if (args.Length == 0 && !Console.IsInputRedirected) {
				ShowHelp (true);
			}

			string outputFile = null, inputFile = null;
			var properties = new Dictionary<string, string> ();
			string preprocessClassName = null;
			bool debug = false;
			bool verbose = false;

			bool noPreprocessingHelpers = false;

			List<string> generatorRefs = new ();
			List<string> generatorImports = new ();
			List<string> generatorIncludePaths = new ();
			List<string> generatorReferencePaths = new ();
			Dictionary<string, KeyValuePair<string, string>> directiveProcessors = new ();
			Dictionary<Tuple<string, string, string>, string> generatorParameters = new ();
			List<string> hostContextAssemblies = new ();
			List<string> templateFileNamePatterns = new ();
			string targetDir = null;

			optionSet = new OptionSet {
				{
					"o=|out=",
					"Set the name or path of the output <file>. It defaults to the input filename with its extension changed to `.txt`, " +
					"or to match the generated code when preprocessing, and may be overridden by template settings. " +
					"Use `-` instead of a filename to write to stdout.",
					s => outputFile = s
				},
				{
					"r=",
					"Add an {<assembly>} reference by path or assembly name. It will be resolved from the " +
					"framework and assembly directories.",
					s => generatorRefs.Add (s)
				},
				{
					"u=|using=",
					"Import a {<namespace>} by generating a using statement.",
					s => generatorImports.Add (s)
				},
				{
					"I=",
					"Add a {<directory>} to be searched when resolving included files.",
					s => generatorIncludePaths.Add (s)
				},
				{
					"P=",
					"Add a {<directory>} to be searched when resolving assemblies.",
					s => generatorReferencePaths.Add (s)
				},
				{
					"HostContextAssembly=",
					"Add a {<HostContextAssembly>} to be shared by templates.",
					s => hostContextAssemblies.Add (s)
				},
				{
					"TargetDir=",
					"Set variable {<TargetDir>} to be used by templates.",
					s => targetDir = s
				},
				{
					"TemplateFileNamePattern=",
					"Add a regex {<TemplateFileNamePattern>} to filter the templates in the solution.",
					s => templateFileNamePatterns.Add (s)
				},
				{
					"c=|class=",
					"Preprocess the template into class {<name>} for use as a runtime template. The class name may include a namespace.",
					(s) => preprocessClassName = s
				},
				{
					"p==|parameter==",
					"Set session parameter {0:<name>} to {1:<value>}. " +
					"The value is accessed from the template's Session dictionary, " +
					"or from a property declared with a parameter directive: <#@ parameter name='<name>' type='<type>' #>. " +
					"If the <name> matches a parameter directive, the <value> will be converted to that parameter's type.",
					(k,v) => properties[k]=v
				},
				{
					"debug",
					"Generate debug symbols and keep temporary files.",
					s => debug = true
				},
				{
					"v|verbose",
					"Output additional diagnostic information to stdout.",
					s => verbose = true
				},
				{
					"NoPreprocessingHelpers",
					"NO Preprocessing-Helpers will be added in preprocessed runtime template.",
					s => noPreprocessingHelpers = true
				},
				{
					"h|?|help",
					"Show help",
					s => ShowHelp (false)
				},
				new CustomOption (
					"dp=!",
					"Set {0:<directive>} to be handled by directive processor {1:<class>} in {2:<assembly>}.",
					3,
					//a => generator.AddDirectiveProcessor(a[0], a[1], a[2])
					a => directiveProcessors.Add(a[0], new KeyValuePair<string, string>(a[1], a[2]))
				),
				new CustomOption (
					"a=!=",
					"Set host parameter {2:<name>} to {3:<value>}. It may optionally be scoped to a {1:<directive>} and/or {0:<processor>}. " +
					"The value is accessed from the host's ResolveParameterValue() method " +
					"or from a property declared with a parameter directive: <#@ parameter name='<name>' #>. ",
					4,
					a => {
						if (a.Count == 2) {
							//generator.AddParameter (null, null, a[0], a[1]);
							generatorParameters.Add (new (null, null, a[0]), a[1]);
						} else if (a.Count == 3) {
							//generator.AddParameter (null, a[0], a[1], a[2]);
							generatorParameters.Add (new (null, a[0], a[1]), a[2]);
						} else {
							generatorParameters.Add (new (a[0], a[1], a[2]), a[3]);
						}
					}
				)
			};
		
			var remainingArgs = optionSet.Parse (args);
			
			string inputContent = null;
			bool inputIsFromStdin = false;

			if (remainingArgs.Count == 0) { //if (remainingArgs.Count != 1) {
				if (Console.IsInputRedirected) {
					inputContent = Console.In.ReadToEnd ();
					inputIsFromStdin = true;
				} else {
					Console.Error.WriteLine ("No input file specified.");
					return 1;
				}
			} else {
				inputFile = remainingArgs[0];
				if (!File.Exists (inputFile)) {
					Console.Error.WriteLine ("Input file '{0}' does not exist.", inputFile);
					return 1;
				}
			}

			bool writeToStdout = outputFile == "-" || (inputIsFromStdin && string.IsNullOrEmpty (outputFile));
			bool isDefaultOutputFilename = false;

			if (!writeToStdout && string.IsNullOrEmpty (outputFile)) {
				outputFile = inputFile;
				isDefaultOutputFilename = true;
				if (Path.HasExtension (outputFile)) {
					var dir = Path.GetDirectoryName (outputFile);
					var fn = Path.GetFileNameWithoutExtension (outputFile);
					outputFile = Path.Combine (dir, fn + ".txt");
				} else {
					outputFile = outputFile + ".txt";
				}
			}

			var generatorSetting = new TemplateGeneratorUtils.TemplateGeneratorSetting(
				generatorRefs, generatorImports, generatorIncludePaths, generatorReferencePaths,
				directiveProcessors, generatorParameters
			);
			generatorSetting.HostContextAssemblies = hostContextAssemblies;

			if (inputFile != null) {
				// ///////////////////////////////////////////////
				if (inputFile.EndsWith(".sln")) 
				{
					if (remainingArgs.Count == 2) 
					{
						var templateFile = remainingArgs[1];
						return TemplateProcessor.ProcessOneFileInSolution(inputFile, targetDir, templateFile, generatorSetting) ? 0 : 1;
					}
					var templateFileNameRegexList = templateFileNamePatterns.Select(p => new Regex(p)).ToList();
					return TemplateProcessor.ProcessSolution(inputFile, targetDir, templateFileNameRegexList, generatorSetting) ? 0 : 1;
				}
				// ///////////////////////////////////////////////
				try {
					inputContent = File.ReadAllText (inputFile);
				}
				catch (IOException ex) {
					Console.Error.WriteLine ("Could not read input file '" + inputFile + "':\n" + ex);
					return 1;
				}
			}

			if (inputContent.Length == 0) {
				Console.Error.WriteLine ("Input is empty");
				return 1;
			}

			var generator = new ToolTemplateGenerator ();		
			TemplateGeneratorUtils.SetTemplateGenerator (generatorSetting, generator);

			var pt = generator.ParseTemplate (inputFile, inputContent);

			TemplateSettings settings = TemplatingEngine.GetSettings (generator, pt);
			if (debug) {
				settings.Debug = true;
			}
			if (verbose) {
				settings.Log = Console.Out;
			}

			if (pt.Errors.Count > 0) {
				generator.Errors.AddRange (pt.Errors);
			}

			string outputContent = null;
			if (!generator.Errors.HasErrors) {
				AddCoercedSessionParameters (generator, pt, properties);
			}

			if (!generator.Errors.HasErrors) {
				if (preprocessClassName == null) {
					(outputFile, outputContent) = generator.ProcessTemplateAsync (pt, inputFile, inputContent, outputFile, settings).Result;
				} else {
					SplitClassName (preprocessClassName, settings);
					outputContent = generator.PreprocessTemplate (pt, inputFile, inputContent, settings, out _, noPreprocessingHelpers);
					if (isDefaultOutputFilename) {
						outputFile = Path.ChangeExtension (outputFile, settings.Provider.FileExtension);
					}
				}
			}

			if (generator.Errors.HasErrors) {
				Console.Error.WriteLine (inputFile == null ? "Processing failed." : $"Processing '{inputFile}' failed.");
			}

			try {
				if (!generator.Errors.HasErrors) {
					if (writeToStdout) {
						Console.WriteLine (outputContent);
					} else {
						File.WriteAllText (outputFile, outputContent, new UTF8Encoding (encoderShouldEmitUTF8Identifier: false));
					}
				}
			}
			catch (IOException ex) {
				Console.Error.WriteLine ("Could not write output file '" + outputFile + "':\n" + ex);
				return 1;
			}

			LogErrors (generator);

			return generator.Errors.HasErrors ? 1 : 0;
		}

		static void SplitClassName (string className, TemplateSettings settings)
		{
			int s = className.LastIndexOf ('.');
			if (s > 0) {
				settings.Namespace = className.Substring (0, s);
				settings.Name = className.Substring (s + 1);
			}
		}

		static void AddCoercedSessionParameters (TemplateGenerator generator, ParsedTemplate pt, Dictionary<string, string> properties)
		{
			if (properties.Count == 0) {
				return;
			}

			var session = generator.GetOrCreateSession ();

			foreach (var p in properties) {
				var directive = pt.Directives.FirstOrDefault (d =>
					d.Name == "parameter" &&
					d.Attributes.TryGetValue ("name", out string attVal) &&
					attVal == p.Key);

				if (directive != null) {
					directive.Attributes.TryGetValue ("type", out string typeName);
					var mappedType = ParameterDirectiveProcessor.MapTypeName (typeName);
					if (mappedType != "System.String") {
						if (ConvertType (mappedType, p.Value, out object converted)) {
							session [p.Key] = converted;
							continue;
						}

						generator.Errors.Add (
							new CompilerError (
								null, 0, 0, null,
								$"Could not convert property '{p.Key}'='{p.Value}' to parameter type '{typeName}'"
							)
						);
					}
				}
				session [p.Key] = p.Value;
			}
		}

		static bool ConvertType (string typeName, string value, out object converted)
		{
			converted = null;
			try {
				var type = Type.GetType (typeName);
				if (type == null) {
					return false;
				}
				Type stringType = typeof (string);
				if (type == stringType) {
					return true;
				}
				var converter = System.ComponentModel.TypeDescriptor.GetConverter (type);
				if (converter == null || !converter.CanConvertFrom (stringType)) {
					return false;
				}
				converted = converter.ConvertFromString (value);
				return true;
			}
			catch {
			}
			return false;
		}

		static void LogErrors (TemplateGenerator generator)
		{
			ErrorsUtils.LogErrors(generator.Errors);
		}

		static void ShowHelp (bool concise)
		{
			var name = Path.GetFileNameWithoutExtension (Assembly.GetExecutingAssembly ().Location);
			Console.WriteLine ("T4 text template processor version {0}", ThisAssembly.AssemblyInformationalVersion);
			Console.WriteLine ("Usage: {0} [options] [template-file]", name);
			if (concise) {
				Console.WriteLine ("Use --help to display options.");
			} else {
				Console.WriteLine ();
				Console.WriteLine ("The template-file argument is required unless the template text is piped in via stdin.");
				Console.WriteLine ();
				Console.WriteLine ("Options:");
				Console.WriteLine ();
				optionSet.WriteOptionDescriptions (Console.Out);
				Console.WriteLine ();
				Environment.Exit (0);
			}
		}
	}
}