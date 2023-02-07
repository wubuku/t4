using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace Mono.TextTemplating
{
    public class VsHierarchyLite : T4Toolbox.EnvDteLites.VsShellInterop.VsHierarchyLite, IVsBuildPropertyStorage
    {
        private readonly IVariableResolver _resolver;

        public VsHierarchyLite(IVariableResolver resolver, Project project) : base(project)
        {
            _resolver = resolver;
        }

		int IVsBuildPropertyStorage.GetItemAttribute (uint item, string pszAttributeName, out string pbstrAttributeValue)
		{
			return base.GetItemAttribute(item, pszAttributeName, out pbstrAttributeValue);
		}

		int IVsBuildPropertyStorage.GetPropertyValue(string pszPropName, string pszConfigName, uint storage, out string pbstrPropValue)
        {
            if (pszPropName.Equals("MSBuildProjectFullPath", StringComparison.InvariantCultureIgnoreCase))
            {
                pbstrPropValue = this._resolver.ResolveVariable("ProjectDir").FirstOrDefault();
                return 0;
            }
            return base.GetPropertyValue(pszPropName, pszConfigName, storage, out pbstrPropValue);
        }

		int IVsBuildPropertyStorage.RemoveProperty (string pszPropName, string pszConfigName, uint storage)
		{
			throw new NotImplementedException ();
		}

		int IVsBuildPropertyStorage.SetItemAttribute (uint item, string pszAttributeName, string pszAttributeValue)
		{
			throw new NotImplementedException ();
		}

		int IVsBuildPropertyStorage.SetPropertyValue (string pszPropName, string pszConfigName, uint storage, string pszPropValue)
		{
			throw new NotImplementedException ();
		}
	}
}
