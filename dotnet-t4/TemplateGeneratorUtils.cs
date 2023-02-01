using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextTemplating
{
	public static class TemplateGeneratorUtils
	{
		public class TemplateGeneratorSetting
		{
			IList<string> generatorRefs;
			private IList<string> generatorImports;
			private IList<string> generatorIncludePaths;
			private IList<string> generatorReferencePaths;
			private IDictionary<string, KeyValuePair<string, string>> directiveProcessors;
			private IDictionary<Tuple<string, string, string>, string> generatorParameters;

			public IList<string> GeneratorRefs { get => generatorRefs; set => generatorRefs = value; }
			public IList<string> GeneratorImports { get => generatorImports; set => generatorImports = value; }
			public IList<string> GeneratorIncludePaths { get => generatorIncludePaths; set => generatorIncludePaths = value; }
			public IList<string> GeneratorReferencePaths { get => generatorReferencePaths; set => generatorReferencePaths = value; }
			public IDictionary<string, KeyValuePair<string, string>> DirectiveProcessors { get => directiveProcessors; set => directiveProcessors = value; }
			public IDictionary<Tuple<string, string, string>, string> GeneratorParameters { get => generatorParameters; set => generatorParameters = value; }

			public TemplateGeneratorSetting (
				IList<string> generatorRefs,
				IList<string> generatorImports,
				IList<string> generatorIncludePaths,
				IList<string> generatorReferencePaths,
				IDictionary<string, KeyValuePair<string, string>> directiveProcessors,
				IDictionary<Tuple<string, string, string>, string> generatorParameters)
			{
				this.generatorRefs = generatorRefs;
				this.generatorImports = generatorImports;
				this.generatorIncludePaths = generatorImports;
				this.generatorReferencePaths = generatorReferencePaths;
				this.directiveProcessors = directiveProcessors;
				this.generatorParameters = generatorParameters;
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