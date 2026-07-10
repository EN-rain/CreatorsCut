using System.Reflection;
using NetArchTest.Rules;

namespace CreatorCut.Domain.Tests;

public sealed class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.ProjectId).Assembly;

    [Fact]
    public void Domain_should_not_depend_on_Desktop()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CreatorCut.Desktop")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_should_not_depend_on_WPF()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.Windows")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_should_not_depend_on_Infrastructure()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CreatorCut.Infrastructure")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_should_not_depend_on_Media_Native()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CreatorCut.Media.Native")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_should_not_depend_on_Application()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("CreatorCut.Application")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_should_depend_on_Domain()
    {
        var assembly = typeof(Application.SplitClipCommand).Assembly;
        var result = Types
            .InAssembly(assembly)
            .That()
            .ResideInNamespace("CreatorCut.Application")
            .Should()
            .HaveDependencyOn("CreatorCut.Domain")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_should_depend_on_Domain()
    {
        var assembly = typeof(Infrastructure.ProjectRepository).Assembly;
        var result = Types
            .InAssembly(assembly)
            .That()
            .ResideInNamespace("CreatorCut.Infrastructure")
            .Should()
            .HaveDependencyOn("CreatorCut.Domain")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join("\n", result.FailingTypeNames ?? []));
    }
}
