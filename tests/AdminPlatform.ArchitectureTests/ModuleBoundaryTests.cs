using System.Reflection;
using NetArchTest.Rules;

namespace AdminPlatform.ArchitectureTests;

/// <summary>Verifies architecture assumption #6 in code, not just in the plan: no module's assembly
/// references another module's namespace. Cross-module communication only happens through a port defined
/// in the dependent module (or in Common) and implemented by an adapter registered at the Host composition
/// root — see AdminPlatform.Api.CrossModuleAdapters.</summary>
public class ModuleBoundaryTests
{
    private static readonly (string Namespace, Assembly Assembly)[] Modules =
    [
        ("AdminPlatform.Modules.Identity", typeof(Modules.Identity.IdentityModule).Assembly),
        ("AdminPlatform.Modules.AccessControl", typeof(Modules.AccessControl.AccessControlModule).Assembly),
        ("AdminPlatform.Modules.Organization", typeof(Modules.Organization.OrganizationModule).Assembly),
        ("AdminPlatform.Modules.Navigation", typeof(Modules.Navigation.NavigationModule).Assembly),
        ("AdminPlatform.Modules.Platform", typeof(Modules.Platform.PlatformModule).Assembly),
    ];

    public static IEnumerable<object[]> ModulePairs
    {
        get
        {
            foreach (var (@namespace, assembly) in Modules)
            {
                var otherNamespaces = Modules
                    .Where(m => m.Namespace != @namespace)
                    .Select(m => m.Namespace)
                    .ToArray();

                yield return [assembly, @namespace, otherNamespaces];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void Module_does_not_reference_any_other_modules_namespace(Assembly assembly, string ownNamespace, string[] otherModuleNamespaces)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot().HaveDependencyOnAny(otherModuleNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"{ownNamespace} has a forbidden dependency on another module. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
