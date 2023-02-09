//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using T4Toolbox;
using Mono.TextTemplating.Properties;
using System.Text.RegularExpressions;

namespace Mono.TextTemplating
{
	class VisualStudioTextTemplateHost : TemplateGenerator, IServiceProvider, ITextTemplatingComponents
	{

        private const string DefaultFileExtension = ".txt";
        private const string FileProtocol = "file:///";
        private static readonly TraceSource Source = new TraceSource("Mono.TextTemplating.VisualStudioTextTemplateHost");
       
        // ////////////////////////////////////////
        private static readonly IList<Regex> SpecificAssemblyReferenceRegexList = new Regex[] {
            new Regex(@"^Dddml\.[^\\\/]*\.dll$"),
            new Regex(@"^YamlDotNet.*\.dll$"),
        };
        // ////////////////////////////////////////

        //private readonly string _templateFile; //base.TemplateFile
        private readonly string _templateDir;
        private readonly DTE2 _dte;
        private readonly IVariableResolver _resolver;
        //private CompilerErrorCollection _errors; //base.Errors
        private string _fileExtension = DefaultFileExtension;
        private Encoding _outputEncoding = Encoding.UTF8;

        private Project _project;

        //public ITextTemplatingEngine Engine { get; set; } //base.Engine;

        /// <summary>
        /// Default fall-back if not specified by the file
        /// </summary>
        public string FileExtension
        {
            get { return _fileExtension; }
        }

        /// <summary>
        /// Encoding of the Output file
        /// </summary>
        public Encoding FileEncoding
        {
            get { return _outputEncoding; }
        }

        internal string ProjectFullPath 
        { 
            get { return _project.FullName; } 
            //set; 
        }

        private ITransformationContextProvider _transformationContextProvider;

        internal ITransformationContextProvider TransformationContextProvider 
        {
            get 
            {
                if (_transformationContextProvider == null)
			    {                    
			        _transformationContextProvider = new global::T4Toolbox.VisualStudio.TransformationContextProvider(this);
			    }
			    return _transformationContextProvider;
            }
        }

        IDictionary<string, Assembly> _hostContextAssemblies = null;

        public IDictionary<string, Assembly> HostContextAssemblies { get => _hostContextAssemblies; }

        public VisualStudioTextTemplateHost(string templateFile, DTE2 dte, IVariableResolver resolver, Project project)
        {
            Refs.Add (typeof (CompilerErrorCollection).Assembly.Location);
            InitHostContextAssemblies();
            AddDirectiveProcessor(
                "T4Toolbox.TransformationContextProcessor", 
                "T4Toolbox.DirectiveProcessors.TransformationContextProcessor",
                typeof (global::T4Toolbox.DirectiveProcessors.TransformationContextProcessor).Assembly.FullName
            );

            if (string.IsNullOrEmpty(templateFile))
            {
                throw new ArgumentNullException("templateFile");
            }

            if (dte == null)
            {
                throw new ArgumentNullException("dte");
            }
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }
            this.TemplateFile = templateFile;
            _dte = dte;
            _resolver = resolver;
            var directoryName = Path.GetDirectoryName(templateFile);
            Debug.Assert(directoryName != null, "directoryName != null, don't expect templateFile to be a root directory!");
            _templateDir = Path.GetFullPath(directoryName);
            _project = project;
        }

        private void InitHostContextAssemblies() 
        {
            var assemblies = new List<Assembly>();
            assemblies.Add (typeof (global::Microsoft.VisualStudio.TextTemplating.VSHost.TextTemplatingCallback).Assembly);
            assemblies.Add (typeof (global::T4Toolbox.Template).Assembly);
            assemblies.Add (typeof (global::T4Toolbox.DirectiveProcessors.TransformationContextProcessor).Assembly);
            assemblies.Add (typeof (global::T4Toolbox.EnvDteLites.DTELite).Assembly);
            assemblies.Add (typeof (global::T4Toolbox.VisualStudio.TransformationContextProvider).Assembly);
            
            _hostContextAssemblies = new Dictionary<string, Assembly>();
            assemblies.ForEach(a => _hostContextAssemblies.Add(a.GetName().Name, a));
            Refs.AddRange(assemblies.Select(a => a.Location));
        }

		protected override ITextTemplatingSession CreateSession () => new ToolTemplateSession (this);

		public object GetService(Type serviceType) 
		{           
			Source.TraceEvent(TraceEventType.Verbose, 0, Resources.VisualStudioTextTemplateHost_GetService_Service_request_of_type___0_, serviceType);
			if (serviceType == typeof (DTE) || serviceType == typeof (DTE2))
			{
			    Source.TraceEvent(TraceEventType.Verbose, 0, Resources.VisualStudioTextTemplateHost_GetService_Returning_DTE_instance_);
			    return _dte;
			}
            /*
            * needed services:
            * ITextTemplatingEngineHost,
            * ITextTemplatingComponents,
            * ITransformationContextProvider
            */
            if (serviceType == typeof(ITextTemplatingEngineHost))
			{
			    return (ITextTemplatingEngineHost)this;
			} 
            if (serviceType == typeof(ITextTemplatingComponents))
			{
			    return (ITextTemplatingComponents)this;
			}		   
            if (serviceType == typeof(ITransformationContextProvider))//TextTemplateHostSettings.Default.GetType("T4Toolbox.ITransformationContextProvider"))
			{
			    return TransformationContextProvider;
			}
			
			return null;
		}

        protected override string ResolvePath (string path)
		{
            if (!String.IsNullOrEmpty(path) && path.TrimStart().StartsWith("$("))
            {
                var varEndIdx = path.IndexOf(")");
                var varStartIdx = path.IndexOf("$(") + 2;
                var varName = path.Substring(varStartIdx, varEndIdx - varStartIdx);
                var varVal = this._resolver.ResolveVariable(varName).FirstOrDefault();
                var remaining = path.Substring(varEndIdx + 1);
                if (!String.IsNullOrEmpty(varVal)) 
                {
                    remaining = remaining.Replace("\\", Path.DirectorySeparatorChar.ToString());
                    if (remaining.StartsWith(Path.DirectorySeparatorChar)) 
                    {
                        remaining = remaining.Substring(1);
                    }
                    path = Path.Combine(varVal, remaining);
                }
            }
            return base.ResolvePath(path);
        }

        protected override string ResolveAssemblyReference (string assemblyReference)
		{
            assemblyReference = assemblyReference.Replace("\\", Path.DirectorySeparatorChar.ToString());
            var resolvedRef = base.ResolveAssemblyReference(assemblyReference);
            // replace some specific references...
            if (!String.IsNullOrEmpty(resolvedRef)) 
            {
                if (!File.Exists(resolvedRef))
                {
                    var fileName = Path.GetFileName(resolvedRef);
                    var regex = SpecificAssemblyReferenceRegexList.Where(r => r.Match(fileName).Success).FirstOrDefault();
                    if (regex != null) 
                    {
                        //try again
                        resolvedRef = base.ResolveAssemblyReference(fileName);
                    }
                }
            }
            return resolvedRef;
        }

        #region Implements ITextTemplatingComponents

        private TextTemplatingCallback _textTemplatingCallback;

        ITextTemplatingCallback ITextTemplatingComponents.Callback
        {
            get
            {
                if (_textTemplatingCallback == null)
                {
                    var callback = CreateTextTemplatingCallback();
                    _textTemplatingCallback = callback;
                }
                return _textTemplatingCallback;
            }
            set
            {
                _textTemplatingCallback = (TextTemplatingCallback)value;
            }
        }

        private TextTemplatingCallback CreateTextTemplatingCallback()
        {
            var callback = new TextTemplatingCallback();
            callback.Initialize();
            if (this._outputEncoding != null)
            {
                callback.OutputEncoding = this._outputEncoding;
            }
            else
            {
                callback.SetOutputEncoding(Encoding.UTF8, false);
            }
            if (this._fileExtension != null)
            {
                callback.SetFileExtension(this._fileExtension);
            }
            return callback;
        }

        ITextTemplatingEngine ITextTemplatingComponents.Engine
        {
            get 
            {
                return this.Engine;    
            }
        }

        private VsHierarchyLite _vsHierarchyLite;

        object ITextTemplatingComponents.Hierarchy
        {
            get
            {
                if (_vsHierarchyLite == null)
                {
                    _vsHierarchyLite = new VsHierarchyLite(this._resolver, this._project);
                }
                return _vsHierarchyLite;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        ITextTemplatingEngineHost ITextTemplatingComponents.Host
        {
            get 
            {
                return this;
            }
        }

        string ITextTemplatingComponents.InputFile
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        
        #endregion

	}

}
