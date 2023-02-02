using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextTemplating
{
	public static class TemplateGeneratorUtils
	{
		public class TemplateGeneratorSetting
		{
			IList<string> _generatorRefs;
			private IList<string> _generatorImports;
			private IList<string> _generatorIncludePaths;
			private IList<string> _generatorReferencePaths;
			private IDictionary<string, KeyValuePair<string, string>> _directiveProcessors;
			private IDictionary<Tuple<string, string, string>, string> _generatorParameters;

			public IList<string> GeneratorRefs { get => _generatorRefs; set => _generatorRefs = value; }
			public IList<string> GeneratorImports { get => _generatorImports; set => _generatorImports = value; }
			public IList<string> GeneratorIncludePaths { get => _generatorIncludePaths; set => _generatorIncludePaths = value; }
			public IList<string> GeneratorReferencePaths { get => _generatorReferencePaths; set => _generatorReferencePaths = value; }
			public IDictionary<string, KeyValuePair<string, string>> DirectiveProcessors { get => _directiveProcessors; set => _directiveProcessors = value; }
			public IDictionary<Tuple<string, string, string>, string> GeneratorParameters { get => _generatorParameters; set => _generatorParameters = value; }

			public TemplateGeneratorSetting (
				IList<string> generatorRefs,
				IList<string> generatorImports,
				IList<string> generatorIncludePaths,
				IList<string> generatorReferencePaths,
				IDictionary<string, KeyValuePair<string, string>> directiveProcessors,
				IDictionary<Tuple<string, string, string>, string> generatorParameters)
			{
				this._generatorRefs = generatorRefs;
				this._generatorImports = generatorImports;
				this._generatorIncludePaths = generatorIncludePaths;
				this._generatorReferencePaths = generatorReferencePaths;
				this._directiveProcessors = directiveProcessors;
				this._generatorParameters = generatorParameters;
			}
		}

		public static void SetTemplateGenerator (
			TemplateGeneratorSetting setting,
			TemplateGenerator generator)
		{
			SetTemplateGenerator (
				setting.GeneratorRefs,
				setting.GeneratorImports,
				setting.GeneratorIncludePaths,
				setting.GeneratorReferencePaths,
				setting.DirectiveProcessors,
				setting.GeneratorParameters,
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