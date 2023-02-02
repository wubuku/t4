using System;
using System.CodeDom.Compiler;

namespace Mono.TextTemplating
{
    public static class ErrorsUtils 
    {
        public static void LogErrors (CompilerErrorCollection errors)
		{
			foreach (CompilerError err in errors) {
				var oldColor = Console.ForegroundColor;
				Console.ForegroundColor = err.IsWarning? ConsoleColor.Yellow : ConsoleColor.Red;
				if (!string.IsNullOrEmpty (err.FileName)) {
					Console.Error.Write (err.FileName);
				}
				if (err.Line > 0) {
					Console.Error.Write ("(");
					Console.Error.Write (err.Line);
					if (err.Column > 0) {
						Console.Error.Write (",");
						Console.Error.Write (err.Column);
					}
					Console.Error.Write (")");
				}
				if (!string.IsNullOrEmpty (err.FileName) || err.Line > 0) {
					Console.Error.Write (": ");
				}
				Console.Error.Write (err.IsWarning ? "WARNING: " : "ERROR: ");
				Console.Error.WriteLine (err.ErrorText);
				Console.ForegroundColor = oldColor;
			}
		}
    }
}
