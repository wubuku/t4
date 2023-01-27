﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Mono.TextTemplating.Properties;
using EnvDTE80;
using Engine = Microsoft.VisualStudio.TextTemplating.Engine;
using System.Text.RegularExpressions;
using T4Toolbox.EnvDteLites;

namespace Mono.TextTemplating
{
    /// <summary>
    /// /
    /// </summary>
    public static class TemplateProcessor
    {
        private static readonly TraceSource Source = new TraceSource("Mono.TextTemplating.TemplateProcessor");

        /// <summary>
        /// /
        /// </summary>
        /// <param name="dte"></param>
        /// <param name="templateFileName"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        static Tuple<string, VisualStudioTextTemplateHost> ProcessTemplateInMemory(DTE2 dte, string templateFileName, IVariableResolver resolver)
        {
            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }
            if (string.IsNullOrEmpty(templateFileName) || !File.Exists(templateFileName))
            {
                throw new ArgumentException(Resources.Program_ProcessTemplateInMemory_String_is_null_or_empty_or_file_doesn_t_exist_, templateFileName);
            }

            //// This would be WAY more elegant, but it spawns a confirmation box...
            ////printfn "Transforming templates..."
            ////dte.ExecuteCommand("TextTransformation.TransformAllTemplates")

            Source.TraceEvent(TraceEventType.Information, 0, Resources.Program_ProcessTemplate_Processing___0_____, templateFileName);

            var templateDir = Path.GetDirectoryName(templateFileName);
            Debug.Assert(templateDir != null, "templateDir != null, don't expect templateFileName to be a root directory.");
            //  Setup Environment
            var oldDir = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = templateDir;

                // Setup NamespaceHint in CallContext
                var templateFileItem = dte.Solution.FindProjectItem(templateFileName);
                var project = templateFileItem.ContainingProject;
                var projectDir = Path.GetDirectoryName(project.FullName);
                Debug.Assert(projectDir != null, "projectDir != null, don't expect project.FullName to be a root directory.");
                string defaultNamespace = project.Properties.Item("DefaultNamespace").Value.ToString();
                Debug.Assert(templateFileName.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase), "Template file-name is not within the project directory.");

                var finalNamespace = defaultNamespace;
                if (templateDir.Length != projectDir.Length)
                {
                    var relativeNamespace =
                        templateDir.Substring(projectDir.Length + 1)
                            // BUG? Handle all namespace relevant characters
                            .Replace("\\", ".").Replace("/", ".");
                    finalNamespace =
                        string.Format(CultureInfo.InvariantCulture, "{0}.{1}", defaultNamespace, relativeNamespace);
                }

                //todo using (new LogicalCallContextChange("NamespaceHint", finalNamespace))
                {
                    
                    var host = new VisualStudioTextTemplateHost(templateFileName, dte, resolver);
                    var engine = new Engine();
                    // ////////////////////////
                    host.ProjectFullPath = project.FullName;
                    //todo ???host.Engine = engine;
                    // ////////////////////////
                    var input = File.ReadAllText(templateFileName);
                    var output = engine.ProcessTemplate(input, host);
                    // ////////////////////////
                    //host.UpdateOutputFiles();
                    // ////////////////////////
                    return Tuple.Create(output, host);
                }
            }
            finally
            {
                Environment.CurrentDirectory = oldDir;
            }
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="dte"></param>
        /// <param name="templateFileName"></param>
        /// <returns>null if we could not process the template and an error-collection of the compilation otherwise.</returns>
        public static CompilerErrorCollection ProcessTemplate(DTE2 dte, string templateFileName, string targetDir)
        {
            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }
            if (templateFileName == null)
            {
                throw new ArgumentNullException("templateFileName");
            }
            if (targetDir == null)
            {
                throw new ArgumentNullException("targetDir");
            }
            var templateDir = Path.GetDirectoryName(templateFileName);
            Debug.Assert(templateDir != null, "templateDir != null, don't expected templateFileName to be a root directory!");

            var defaultResolver = DefaultVariableResolver.CreateFromDte(dte, templateFileName);
            
            IVariableResolver resolver = defaultResolver;
            Source.TraceEvent(TraceEventType.Information, 1, "Default TargetDir {0} will be used", defaultResolver.TargetDir);
            Source.TraceEvent(TraceEventType.Information, 1, "Default SolutionDir {0} will be used", defaultResolver.SolutionDir);
            Source.TraceEvent(TraceEventType.Information, 1, "Default ProjectDir {0} will be used", defaultResolver.ProjectDir);

            if (!string.IsNullOrEmpty(targetDir))
            {
                if (Directory.Exists(targetDir))
                {
                    Source.TraceEvent(TraceEventType.Information, 1, "TargetDir {0} will be added ", targetDir);
                    resolver = new CombiningVariableResolver(new DefaultVariableResolver(null, null, targetDir), resolver);
                }
                else
                {
                    Source.TraceEvent(TraceEventType.Warning, 1, "TargetDir {0} doesn't exist and will be ignored!", targetDir);
                }
            }

            var result = ProcessTemplateInMemory(dte, templateFileName, resolver);
            var host = result.Item2;
            var output = result.Item1;

            var outFileName = Path.GetFileNameWithoutExtension(templateFileName);
            var outFilePath = Path.Combine(templateDir, outFileName + host.FileExtension);
            // Because with TFS the files could be read-only!
            if (File.Exists(outFilePath))
            {
                var attr = File.GetAttributes(outFilePath);
                File.SetAttributes(outFilePath, attr & ~FileAttributes.ReadOnly);
                File.Delete(outFilePath);
            }
            File.WriteAllText(outFilePath, output, host.FileEncoding);
            // //////////////////////
            var filteredErrors = GetFilteredHostErrors(host);
            return filteredErrors;
        }

        private static CompilerErrorCollection GetFilteredHostErrors(VisualStudioTextTemplateHost host)
        {
            var filteredErrors = new CompilerErrorCollection();
            if (host.Errors != null)
            {
                for (int i = 0; i < host.Errors.Count; i++)
                {
                    if (!host.Errors[i].IsWarning)
                    {
                        filteredErrors.Add(host.Errors[i]);
                    }
                }
            }
            return filteredErrors;
        }

        private static IEnumerable<string> FindTemplates(string p)
        {
            foreach (var template in Directory.EnumerateDirectories(p).SelectMany(FindTemplates))
            {
                yield return template;
            }
            foreach (var template in Directory.EnumerateFiles(p, "*.tt"))
            {
                yield return Path.GetFullPath(template);
            }
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="solutionFileName"></param>
        /// <returns></returns>
        public static bool ProcessSolution(string solutionFileName, string targetDir, ICollection<Regex> fileNamePatterns = null)
        {
            if (string.IsNullOrEmpty(solutionFileName) || !File.Exists(solutionFileName))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentUICulture,
                        Resources.Program_Main_the_file_path___0___is_either_invalid_or_doesn_t_exist_, solutionFileName));
            }

            solutionFileName = Path.GetFullPath(solutionFileName);
            Source.TraceEvent(TraceEventType.Information, 0, Resources.Program_Main_Creating_VS_instance___);
            //todo using (new MessageFilter())
            {
                //var result = DteHelper.CreateDteInstance();
                DTE2 dte = null;//todo ???new DTELite(result.Item2); //result.Item2;
                //var processId = result.Item1;
                try
                {
                    Source.TraceEvent(TraceEventType.Information, 0, Resources.Program_Main_Opening__0_, solutionFileName);
                    dte.Solution.Open(solutionFileName);

                    Source.TraceEvent(TraceEventType.Verbose, 0, Resources.Program_Main_Finding_and_processing___tt_templates___);
                    var firstError =
                        FindTemplates(Path.GetDirectoryName(solutionFileName))
                            .Where(t => MatchPatterns(t, fileNamePatterns))
                            .Select(t =>
                                    { 
                                        try
                                        {
                                            return Tuple.Create(t, ProcessTemplate(dte, t, targetDir));
                                        }
                                        catch (TemplateNotPartOfSolutionException)
                                        {
                                            Source.TraceEvent(TraceEventType.Warning, 2, "The template found within the solution dir was not part of the given solution ({0}): {1}", solutionFileName, t);
                                            return null;
                                        }
                                    })
                            .Where(t => t != null)
                            .FirstOrDefault(tuple => tuple.Item2.Count > 0);

                    if (firstError != null)
                    {
                        Source.TraceEvent(TraceEventType.Warning, 0, Resources.Program_Main_FAILED_to_process___0__,
                            firstError.Item1);
                        foreach (var error in firstError.Item2)
                        {
                            Source.TraceEvent(TraceEventType.Error, 0, Resources.Program_Main_, error);
                        }
                        return false;
                    }

                    Source.TraceEvent(TraceEventType.Information, 0, Resources.Program_Main_Everything_worked_);
                    return true;
                }
                finally
                {
                    //DteHelper.CleanupDteInstance(processId, dte);
                }
            }
        }

        private static bool MatchPatterns(string t, ICollection<Regex> fileNamePatterns)
        {
            if (fileNamePatterns == null)
            {
                return true;
            }
            var f = Path.GetFileName(t);
            var rx = fileNamePatterns.FirstOrDefault(r => r.IsMatch(f));
            if (rx != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
    
}