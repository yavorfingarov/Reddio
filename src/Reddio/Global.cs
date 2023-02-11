global using System.Data;
global using System.Globalization;
global using Dapper;
global using Microsoft.AspNetCore.Mvc.RazorPages;
global using Reprise;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Logging performance is not a concern.")]

namespace Reddio
{
    public static class Build
    {
        public static string Number { get; }

        static Build()
        {
            var informationalVersion = typeof(Build).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            var tokens = informationalVersion.Split("-");
            Number = tokens.Length switch
            {
                1 => "local",
                2 => tokens[1],
                _ => throw new InvalidOperationException($"Informational version '{informationalVersion}' is invalid.")
            };
        }
    }
}
