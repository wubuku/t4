using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mono.TextTemplating
{
	internal static class DddmlTemplateUtils
	{
		private static IList<Regex> s_domainTemplatePatterns = new Regex[] {
				new Regex("GenerateAggregates.*\\.tt"),
				new Regex("GenerateBoundedContext.*\\.tt"),
                //new Regex("GenerateBoundedContextDomainHibernate.*\\.tt"),
                new Regex("GenerateTree.*\\.tt"),
				new Regex(".*ForeignKeyConstraints\\.tt"),
				new Regex(".*RViews\\.tt"),
				new Regex(".*RViewNameConflictedTables\\.tt"),
				new Regex(".*StateConstraints\\.tt"),
				new Regex("GenerateDomainServices.*\\.tt"),
			};

		internal static IList<Regex> GetAggregateTemplateFileNamePatterns (string aggregateName)
		{
			if (String.IsNullOrWhiteSpace (aggregateName)) {
				return null;
			}
			//GenerateXxxxDomain 开头的脚本；
			//GenerateAggregatesHbm.tt 脚本重新生成 hbm 映射文件；
			//GenerateAggregatesResources.tt 重新生成 RESTful API；
			//GenerateBoundedContextMetadata.tt 更新元数据文件
			//GenerateBoundedContextDomainAggregatesMetadata.tt 更新元数据文件
			//GenerateAggregatesConfig.tt 更新配置文件
			var patterns = new List<Regex>(new Regex[] {
				new Regex(String.Format("Generate{0}Domain.*\\.tt", aggregateName)),
			});
            patterns.AddRange(s_domainTemplatePatterns);
			return patterns;
		}
	}
}

