using FluentAssertions;
using FinancePlatform.Models.Entities;
using ModelsMarker = FinancePlatform.Models.AssemblyMarker;
using ServicesMarker = FinancePlatform.Services.AssemblyMarker;

namespace FinancePlatform.UnitTests;

public class SolutionSmokeTests
{
    [Fact]
    public void Models_assembly_is_loadable()
    {
        typeof(ModelsMarker).Assembly.GetName().Name
            .Should().Be("FinancePlatform.Models");
    }

    [Fact]
    public void Services_assembly_is_loadable()
    {
        typeof(ServicesMarker).Assembly.GetName().Name
            .Should().Be("FinancePlatform.Services");
    }

    [Fact]
    public void Domain_entities_are_defined()
    {
        typeof(SystemEventTrigger).Should().NotBeNull();
        typeof(CashBalance).Should().NotBeNull();
    }
}
