using System;
using System.Collections.Generic;
using System.Linq;
using Mono.TextTemplating;

namespace T4Toolbox.VSHostLites
{
	public static class TemplateGeneratorUtils
	{
		public class TemplateGeneratorSetting
		{
			IList<string> _refs;
			private IList<string> _imports;
			private IList<string> _includePaths;
			private IList<string> _referencePaths;
			private IDictionary<string, KeyValuePair<string, string>> _directiveProcessors;
			private IDictionary<Tuple<string, string, string>, string> _parameters;

			private IList<string> _hostContextAssemblies;

			public IList<string> Refs { get => _refs; set => _refs = value; }
			public IList<string> Imports { get => _imports; set => _imports = value; }
			public IList<string> IncludePaths { get => _includePaths; set => _includePaths = value; }
			public IList<string> ReferencePaths { get => _referencePaths; set => _referencePaths = value; }
			public IDictionary<string, KeyValuePair<string, string>> DirectiveProcessors { get => _directiveProcessors; set => _directiveProcessors = value; }
			public IDictionary<Tuple<string, string, string>, string> Parameters { get => _parameters; set => _parameters = value; }
            public IList<string> HostContextAssemblies { get => _hostContextAssemblies; set => _hostContextAssemblies = value; }

            public TemplateGeneratorSetting (
				IList<string> generatorRefs,
				IList<string> generatorImports,
				IList<string> generatorIncludePaths,
				IList<string> generatorReferencePaths,
				IDictionary<string, KeyValuePair<string, string>> directiveProcessors,
				IDictionary<Tuple<string, string, string>, string> generatorParameters)
			{
				this._refs = generatorRefs;
				this._imports = generatorImports;
				this._includePaths = generatorIncludePaths;
				this._referencePaths = generatorReferencePaths;
				this._directiveProcessors = directiveProcessors;
				this._parameters = generatorParameters;
			}
		}

		public static void SetTemplateGenerator (
			TemplateGeneratorSetting setting,
			TemplateGenerator generator)
		{
			SetTemplateGenerator (
				setting.Refs,
				setting.Imports,
				setting.IncludePaths,
				setting.ReferencePaths,
				setting.DirectiveProcessors,
				setting.Parameters,
				generator
			);
		}

		public static void SetTemplateGenerator (
			IList<string> generatorRefs,
		 	IList<string> generatorImports,
			IList<string> generatorIncludePaths,
			IList<string> generatorReferencePaths,
			IDictionary<string, KeyValuePair<string, string>> directiveProcessors,
			IDictionary<Tuple<string, string, string>, string> generatorParameters,
			TemplateGenerator generator)
		{
			generator.Refs.AddRange (generatorRefs);
			generator.Imports.AddRange (generatorImports);
			generator.IncludePaths.AddRange (generatorIncludePaths);
			generator.ReferencePaths.AddRange (generatorReferencePaths);
			directiveProcessors.ToList ().ForEach (kv => generator.AddDirectiveProcessor (kv.Key, kv.Value.Key, kv.Value.Value));
			generatorParameters.ToList ().ForEach (kv => generator.AddParameter (kv.Key.Item1, kv.Key.Item2, kv.Key.Item3, kv.Value));
		}
	}
}