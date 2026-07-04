using SharedKernel.DomainEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SharedKernel.Utilities;

/// <summary>
/// Provides utility methods for platform detection and domain event type discovery.
/// </summary>
/// <remarks>
/// This class is intended to support environment-specific behavior and reflection-based type scanning.
/// </remarks>
public static class Util
{
    /// <summary>
    /// Determines whether the current operating system is Windows.
    /// </summary>
    /// <returns><c>true</c> if the OS is Windows; otherwise, <c>false</c>.</returns>
    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Determines whether the current operating system is Linux.
    /// </summary>
    /// <returns><c>true</c> if the OS is Linux; otherwise, <c>false</c>.</returns>
    public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Determines whether the current operating system is macOS.
    /// </summary>
    /// <returns><c>true</c> if the OS is macOS; otherwise, <c>false</c>.</returns>
    public static bool IsMac() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Retrieves all non-abstract, concrete domain event types that inherit from <see cref="DomainEventBase"/> within the specified assemblies.
    /// </summary>
    /// <param name="assemblies">An array of assemblies to scan for domain event types.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> containing all types that inherit from <see cref="DomainEventBase"/>,
    /// excluding <see cref="DomainEventBase"/> itself.
    /// </returns>
    /// <example>
    /// <code>
    /// var domainEventTypes = Util.GetAllDomainEventTypes(AppDomain.CurrentDomain.GetAssemblies());
    /// </code>
    /// </example>
    /// <remarks>
    /// This method is useful for registering or processing all domain events dynamically, such as in a reflection-based dispatcher or event bus.
    /// </remarks>
    public static IEnumerable<Type> GetAllDomainEventTypes(Assembly[] assemblies)
    {
        return assemblies
            .SelectMany(m => m.GetTypes())
            .Where(m =>
                m is { IsClass: true, IsAbstract: false } &&
                typeof(DomainEventBase).IsAssignableFrom(m) &&
                m != typeof(DomainEventBase));
    }
}
