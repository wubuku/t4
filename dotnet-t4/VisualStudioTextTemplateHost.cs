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

using Microsoft.VisualStudio.TextTemplating;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using T4Toolbox;

namespace Mono.TextTemplating
{
	class VisualStudioTextTemplateHost : TemplateGenerator, IServiceProvider, ITextTemplatingComponents
	{
        private ITransformationContextProvider _transformationContextProvider;

		public VisualStudioTextTemplateHost ()
		{
			Refs.Add (typeof (CompilerErrorCollection).Assembly.Location);
		}

		protected override ITextTemplatingSession CreateSession () => new ToolTemplateSession (this);

		public object GetService(Type serviceType) 
		{
			// Source.TraceEvent(TraceEventType.Verbose, 0, Resources.VisualStudioTextTemplateHost_GetService_Service_request_of_type___0_, serviceType);
			// if (serviceType == typeof (DTE) || serviceType == typeof (DTE2))
			// {
			//     Source.TraceEvent(TraceEventType.Verbose, 0, Resources.VisualStudioTextTemplateHost_GetService_Returning_DTE_instance_);
			//     return _dte;
			// }
			// // ///////////////////////////////////
			// if (serviceType == typeof(T4Toolbox.ITransformationContextProvider))//TextTemplateHostSettings.Default.GetType("T4Toolbox.ITransformationContextProvider"))
			// {
			//     if (_transformationContextProvider == null)
			//     {
			//         var cp = new TransformationContextProvider(this);
			//         cp.ProjectFullPath = this.ProjectFullPath;
			//         _transformationContextProvider = cp;
			//     }
			//     return _transformationContextProvider;
			//     // -----------------------------------
			//     //var serviceImplType = typeof(T4Toolbox.VisualStudio.ScriptFileGenerator).Assembly
			//     //    .GetType("T4Toolbox.VisualStudio.TransformationContextProvider");
			//     //ConstructorInfo ctr = serviceImplType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(IServiceProvider) }, new ParameterModifier[0]);
			//     //var _transformationContextProvider = (T4Toolbox.ITransformationContextProvider)ctr.Invoke(new object[] { this });
			//     //return _transformationContextProvider;
			//     // -------------------------------------
			//     //Type t4packType = typeof(T4Toolbox.VisualStudio.ScriptFileGenerator).Assembly.GetType("T4Toolbox.VisualStudio.T4ToolboxPackage");
			//     //var container = (System.ComponentModel.Design.IServiceContainer)Activator.CreateInstance(t4packType);
			//     //MethodInfo initMethod = t4packType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			//     //initMethod.Invoke(container, new object[0]);
			//     //var service = container.GetService(serviceType);
			//     //return serviceType;
			// }
			// if (serviceType == typeof(STextTemplating))
			// {
			//     return this;
			// }
			// ///////////////////////////////////
			return null;
		}


        #region Implements ITextTemplatingComponents

        // private TextTemplatingCallback _textTemplatingCallback;

        ITextTemplatingCallback ITextTemplatingComponents.Callback
        {
            get
            {
                // if (_textTemplatingCallback == null)
                // {
                //     var callback = CreateTextTemplatingCallback();
                //     _textTemplatingCallback = callback;
                // }
                // return _textTemplatingCallback;
				throw new NotImplementedException();
            }
            set
            {
                // _textTemplatingCallback = (TextTemplatingCallback)value;
				throw new NotImplementedException();
            }
        }

        // private TextTemplatingCallback CreateTextTemplatingCallback()
        // {
        //     var callback = new TextTemplatingCallback();
        //     callback.Initialize();
        //     if (this._outputEncoding != null)
        //     {
        //         callback.OutputEncoding = this._outputEncoding;
        //     }
        //     else
        //     {
        //         callback.SetOutputEncoding(Encoding.UTF8, false);
        //     }
        //     if (this._fileExtension != null)
        //     {
        //         callback.SetFileExtension(this._fileExtension);
        //     }
        //     return callback;
        // }

        ITextTemplatingEngine ITextTemplatingComponents.Engine
        {
            get 
            {
                return this.Engine;    
            }
        }

        // private VsHierarchyLite _vsHierarchyLite;

        object ITextTemplatingComponents.Hierarchy
        {
            get
            {
                // if (_vsHierarchyLite == null)
                // {
                //     _vsHierarchyLite = new VsHierarchyLite(this._resolver);
                // }
                // return _vsHierarchyLite;
				throw new NotImplementedException();
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
