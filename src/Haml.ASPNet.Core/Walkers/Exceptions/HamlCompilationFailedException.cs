using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NHaml.Walkers.Exceptions
{
    public class HamlCompilationFailedException : Exception
    {
        private IReadOnlyList<Diagnostic> diagnostics;

        public HamlCompilationFailedException(IReadOnlyList<Diagnostic> diagnostics)
            : base(GenerateExceptionMessage())
        {
            this.diagnostics = diagnostics;
        }

        public IReadOnlyList<Diagnostic> Diagnostics
        {
            get
            {
                return diagnostics;
            }
        }

        private static string GenerateExceptionMessage()
        {
            return string.Format("Failed to compile inline code for HAML template.");
        }
    }
}
