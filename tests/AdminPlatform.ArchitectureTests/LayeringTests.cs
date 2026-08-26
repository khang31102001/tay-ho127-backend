using System.Reflection;
using NetArchTest.Rules;

namespace AdminPlatform.ArchitectureTests;

/// <summary>Enforces the internal Domain/Application/Infrastructure/Api layering inside every module
/// assembly via a static check, since each module is one project (architecture assumption #3) rather than
/// four separate assemblies with hard project-reference boundaries.</summary>
public class LayeringTests
{
    public static IEnumerable<object[]> ModuleAssemblies => new[]
    {
        typeof(Modules.Identity.IdentityModule).Assembly,
        typeof(Modules.AccessControl.AccessControlModule).Assembly,
        typeof(Modules.Organization.OrganizationModule).Assembly,
        typeof(Modules.Navigation.NavigationModule).Assembly,
        typeof(Modules.Platform.PlatformModule).Assembly,
    }.Select(a => new object[] { a });

    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void Domain_types_do_not_depend_on_Infrastructure_or_Api(Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespaceContaining(".Domain")
            .ShouldNot().HaveDependencyOnAny(NamespaceContaining(assembly, ".Infrastructure"), NamespaceContaining(assembly, ".Api"))
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void Application_types_do_not_depend_on_Infrastructure_or_Api(Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespaceContaining(".Application")
            .ShouldNot().HaveDependencyOnAny(NamespaceContaining(assembly, ".Infrastructure"), NamespaceContaining(assembly, ".Api"))
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(ModuleAssemblies))]
    public void Domain_types_do_not_depend_on_EntityFrameworkCore(Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespaceContaining(".Domain")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string NamespaceContaining(Assembly assembly, string suffix) => assembly.GetName().Name + suffix;

    private static string Describe(TestResult result) =>
        result.IsSuccessful ? string.Empty : "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
