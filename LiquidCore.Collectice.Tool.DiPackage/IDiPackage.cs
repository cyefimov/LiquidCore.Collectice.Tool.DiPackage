namespace LiquidCore.Collectice.Tool.DiPackage;

/// <summary>
/// Interface for Dependency injection package
/// </summary>
public interface IDiPackage
{
    /// <summary>
    /// Register DI bindings for a given package
    /// </summary>
    /// <param name="services">Instance of <see cref="IServiceCollection"/>.</param>
    void RegisterServices(IServiceCollection services);
}