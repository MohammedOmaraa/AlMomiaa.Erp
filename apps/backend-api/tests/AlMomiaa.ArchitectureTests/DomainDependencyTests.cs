using AlMomiaa.Domain;
using Xunit;

namespace AlMomiaa.ArchitectureTests;

public sealed class DomainDependencyTests
{
    [Fact]
    public void Domain_assembly_does_not_reference_application_or_frameworks()
    {
        var references = typeof(DomainAssemblyMarker).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference => reference.Name is not null &&
            (reference.Name.StartsWith("AlMomiaa.", StringComparison.Ordinal) ||
             reference.Name.StartsWith("Microsoft.", StringComparison.Ordinal) ||
             reference.Name.StartsWith("MediatR", StringComparison.Ordinal) ||
             reference.Name.StartsWith("FluentValidation", StringComparison.Ordinal)));
    }
}
