global using Xunit;

using System.Diagnostics.CodeAnalysis;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names can contain underscores.")]
