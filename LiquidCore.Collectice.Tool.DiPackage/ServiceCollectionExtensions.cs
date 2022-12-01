namespace LiquidCore.Collectice.Tool.DiPackage;

/// <summary>
/// <see cref="IServiceCollection"/> extension
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register DI bindings for provided set of <see cref="Assembly"/>
    /// </summary>
    /// <param name="services">Instance of <see cref="IServiceCollection"/></param>
    /// <param name="assemblies">List of <see cref="Assembly"/></param>
    public static void RegisterDiPackages(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (assemblies == null)
            throw new ArgumentNullException(nameof(assemblies));

        foreach (var package in GetPackagesToRegister(assemblies))
            package.RegisterServices(services);
    }

    /// <summary>
    /// Extracts list of packages from provided list of <see cref="Assembly"/>
    /// </summary>
    /// <param name="assemblies">List of <see cref="Assembly"/></param>
    /// <returns>Array of <see cref="IDiPackage"/></returns>
    private static IEnumerable<IDiPackage> GetPackagesToRegister(IEnumerable<Assembly> assemblies)
    {
        var packageTypes = (
                from assembly in assemblies
                from type in GetExportedTypesFrom(assembly)
                where typeof(IDiPackage).Info().IsAssignableFrom(type.Info())
                where !type.Info().IsAbstract
                where !type.Info().IsGenericTypeDefinition
                select type)
            .ToArray();

        RequiresPackageTypesHaveDefaultConstructor(packageTypes);

        return packageTypes.Select(CreatePackage).ToArray();
    }

    /// <summary>
    /// Extracts exported type from provided instance of <see cref="Assembly"/>
    /// </summary>
    /// <param name="assembly">Instance of <see cref="Assembly"/></param>
    /// <returns>List of <see cref="Type"/></returns>
    private static IEnumerable<Type> GetExportedTypesFrom(Assembly assembly)
    {
        try
        {
            return assembly.DefinedTypes.Select(info => info.AsType());
        }
        catch (NotSupportedException)
        {
            // A type load exception would typically happen on an Anonymously Hosted DynamicMethods Assembly and it would be safe to skip this exception
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Verifies items in provided list of <see cref="Type"/> have default constructors
    /// </summary>
    /// <param name="packageTypes">List of <see cref="Type"/></param>
    private static void RequiresPackageTypesHaveDefaultConstructor(IEnumerable<Type> packageTypes)
    {
        var invalidPackageType = packageTypes.FirstOrDefault(type => !type.HasDefaultConstructor());

        if (invalidPackageType != null)
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The type {0} does not contain a default (public parameterless) constructor. Packages must have a default constructor.", invalidPackageType.FullName));
    }

    /// <summary>
    /// Creates a <see cref="IDiPackage"/> of given <see cref="Type"/>
    /// </summary>
    /// <param name="packageType">Instance of <see cref="Type"/></param>
    /// <returns>Instance of <see cref="IDiPackage"/></returns>
    private static IDiPackage CreatePackage(Type packageType)
    {
        try
        {
            return ((IDiPackage)Activator.CreateInstance(packageType)!)!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "The creation of package type {0} failed. {1}", packageType.FullName, ex.Message), ex);
        }
    }

    /// <summary>
    /// Extension method for <see cref="Type"/>. Verifies is <see cref="Type"/> haas default constructor
    /// </summary>
    /// <param name="type">Instance of <see cref="Type"/></param>
    /// <returns><see cref="bool"/> value.</returns>
    private static bool HasDefaultConstructor(this Type type) => type.GetConstructors().Any(ctor => !ctor.GetParameters().Any());
        
    /// <summary>
    /// Extension method for <see cref="Type"/>. Extracts <see cref="ConstructorInfo"/> from given <see cref="Type"/>
    /// </summary>
    /// <param name="type">Instance of <see cref="Type"/></param>
    /// <returns>Array of <see cref="ConstructorInfo"/></returns>
    private static IEnumerable<ConstructorInfo> GetConstructors(this Type type) => type.GetTypeInfo().DeclaredConstructors.ToArray();

    /// <summary>
    /// Extension method for <see cref="Type"/>. Extracts <see cref="TypeInfo"/> from given <see cref="Type"/>
    /// </summary>
    /// <param name="type">Instance of <see cref="Type"/></param>
    /// <returns>Instance of <see cref="TypeInfo"/></returns>
    private static TypeInfo Info(this Type type) => type.GetTypeInfo();
}