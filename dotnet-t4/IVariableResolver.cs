using System;
using System.Collections.Generic;

namespace Mono.TextTemplating
{

	/// <summary>
	/// Interface to resolve 'msbuild' variables.
	/// </summary>
	public interface IVariableResolver
	{
		/// <summary>
		/// Try to resolve the given variable (return a list of possible paths)
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		IEnumerable<string> ResolveVariable (string variable);
	}

}

