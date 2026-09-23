using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SharedKernel.Mediator;

/// <summary>
/// Provides extension methods to register MediatR request handlers and pipeline behaviors into the DI container.
/// Handles discovery and registration of open and closed generic request handler implementations from specified assemblies,
/// with support for registration limits and timeouts.
/// </summary>
public static class ServiceRegistrar
{
    /// <summary>
    /// Adds MediatR request handler classes from specified assemblies, with a timeout to prevent long registration times.
    /// Throws TimeoutException if registration exceeds the configured timeout.
    /// </summary>
    /// <param name="services">The DI service collection to add to.</param>
    /// <param name="configuration">Configuration specifying assemblies and registration options.</param>
    public static void AddMediatRClassesWithTimeout(IServiceCollection services, MediatRServiceConfiguration configuration)
    {
        // A timeout of 0 is documented as "disabled"; CancellationTokenSource(0) would fire immediately.
        var timeout = configuration.RegistrationTimeout > 0 ? configuration.RegistrationTimeout : Timeout.Infinite;

        using var cts = new CancellationTokenSource(timeout);

        try
        {
            AddMediatRClasses(services, configuration, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException("The generic handler registration process timed out.");
        }
    }

    /// <summary>
    /// Scans and registers MediatR request handler classes from the configured assemblies.
    /// Registers both open and closed generic handlers for IRequestHandler&lt;,&gt; and IRequestHandler&lt;&gt;.
    /// </summary>
    /// <param name="services">The DI service collection to add to.</param>
    /// <param name="configuration">Configuration specifying assemblies and registration options.</param>
    /// <param name="cancellationToken">Cancellation token to abort scanning if needed.</param>
    public static void AddMediatRClasses(IServiceCollection services, MediatRServiceConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<>), services, assembliesToScan, false, configuration, cancellationToken);
    }

    // Connect implementations of open generic request handler interfaces to closed types, registering them with DI.
    private static void ConnectImplementationsToTypesClosing(Type openRequestInterface,
        IServiceCollection services,
        IEnumerable<Assembly> assembliesToScan,
        bool addIfAlreadyExists,
        MediatRServiceConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var concretions = new List<Type>();
        var interfaces = new List<Type>();
        var genericConcretions = new List<Type>();
        var genericInterfaces = new List<Type>();

        // Find concrete types that close the openRequestInterface and satisfy configuration filters
        var types = assembliesToScan
            .SelectMany(GetLoadableTypes)
            .Where(t => (!t.ContainsGenericParameters || configuration.RegisterGenericHandlers)
                        && t.IsConcrete()
                        && t.FindInterfacesThatClose(openRequestInterface).Any()
                        && configuration.TypeEvaluator.Invoke(t))
            .ToArray();

        foreach (var type in types)
        {
            var interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();

            if (!type.IsOpenGeneric())
            {
                concretions.Add(type);

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaces.Fill(interfaceType);
                }
            }
            else
            {
                genericConcretions.Add(type);
                foreach (var interfaceType in interfaceTypes)
                {
                    genericInterfaces.Fill(interfaceType);
                }
            }
        }

        // Register closed implementations with DI
        foreach (var @interface in interfaces)
        {
            var exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();

            if (addIfAlreadyExists)
            {
                foreach (var type in exactMatches)
                {
                    services.Add(new ServiceDescriptor(@interface, type, configuration.Lifetime));
                }
            }
            else
            {
                if (exactMatches.Count > 1)
                {
                    exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
                }

                foreach (var type in exactMatches)
                {
                    services.TryAdd(new ServiceDescriptor(@interface, type, configuration.Lifetime));
                }
            }

            if (!@interface.IsOpenGeneric())
            {
                AddConcretionsThatCouldBeClosed(@interface, concretions, services, configuration);
            }
        }

        // Register open generic implementations with DI
        foreach (var @interface in genericInterfaces)
        {
            var exactMatches = genericConcretions.Where(x => x.CanBeCastTo(@interface)).ToArray();
            AddAllConcretionsThatClose(@interface, exactMatches, services, assembliesToScan, configuration, cancellationToken);
        }
    }

    // Checks whether a handler type's generic arguments match those of the interface.
    private static bool IsMatchingWithInterface(Type? handlerType, Type handlerInterface)
    {
        if (handlerType == null || handlerInterface == null)
        {
            return false;
        }

        if (handlerType.IsInterface)
        {
            if (handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments))
            {
                return true;
            }
        }
        else
        {
            return IsMatchingWithInterface(handlerType.GetInterface(handlerInterface.Name), handlerInterface);
        }

        return false;
    }

    // Register open generic handler types that can be closed with the interface's generic arguments.
    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services, MediatRServiceConfiguration configuration)
    {
        foreach (var type in concretions.Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAdd(new ServiceDescriptor(@interface, type.MakeGenericType(@interface.GenericTypeArguments), configuration.Lifetime));
            }
            catch (ArgumentException)
            {
                // The interface's type arguments violate the concretion's generic constraints; it cannot close here.
            }
        }
    }

    // Determine the service and implementation types for DI registration from the generic handler and request types.
    private static (Type Service, Type Implementation) GetConcreteRegistrationTypes(Type openRequestHandlerInterface, Type concreteGenericTRequest, Type openRequestHandlerImplementation)
    {
        var closingTypes = concreteGenericTRequest.GetGenericArguments();

        var concreteTResponse = concreteGenericTRequest.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()
            .FirstOrDefault();

        var typeDefinition = openRequestHandlerInterface.GetGenericTypeDefinition();

        var serviceType = concreteTResponse != null ?
            typeDefinition.MakeGenericType(concreteGenericTRequest, concreteTResponse) :
            typeDefinition.MakeGenericType(concreteGenericTRequest);

        return (serviceType, openRequestHandlerImplementation.MakeGenericType(closingTypes));
    }

    // Retrieve all concrete request types that satisfy generic constraints of the open generic handler implementation.
    private static Type[]? GetConcreteRequestTypes(Type openRequestHandlerInterface, Type openRequestHandlerImplementation, IEnumerable<Assembly> assembliesToScan, MediatRServiceConfiguration configuration, CancellationToken cancellationToken)
    {
        var constraintsForEachParameter = openRequestHandlerImplementation
            .GetGenericArguments()
            .Select(x => x.GetGenericParameterConstraints())
            .ToArray();

        var typesThatCanCloseForEachParameter = constraintsForEachParameter
            .Select(constraints => assembliesToScan
                .SelectMany(GetLoadableTypes)
                .Where(type => type.IsClass && !type.IsAbstract && constraints.All(constraint => constraint.IsAssignableFrom(type))).ToArray()
            ).ToArray();

        var requestType = openRequestHandlerInterface.GenericTypeArguments.First();

        if (requestType.IsGenericParameter)
            return null;

        var requestGenericTypeDefinition = requestType.GetGenericTypeDefinition();

        var combinations = GenerateCombinations(requestType, typesThatCanCloseForEachParameter, configuration, cancellationToken);

        return combinations.Select(types => requestGenericTypeDefinition.MakeGenericType(types.ToArray())).ToArray();
    }

    /// <summary>
    /// Generates all possible combinations of types to close generic type parameters.
    /// Validates limits for max generic parameters, max types per parameter, and max total registrations.
    /// </summary>
    /// <param name="requestType">The generic request type definition.</param>
    /// <param name="lists">Arrays of types that can close each generic parameter.</param>
    /// <param name="configuration">Configuration holding the registration limits.</param>
    /// <param name="cancellationToken">Cancellation token to stop processing.</param>
    /// <returns>Enumerable of type lists representing each combination.</returns>
    public static IEnumerable<List<Type>> GenerateCombinations(Type requestType, Type[][] lists, MediatRServiceConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration.MaxGenericTypeParameters > 0 && lists.Length > configuration.MaxGenericTypeParameters)
            throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The number of generic type parameters exceeds the maximum allowed ({configuration.MaxGenericTypeParameters}).");

        foreach (var list in lists)
        {
            if (configuration.MaxTypesClosing > 0 && list.Length > configuration.MaxTypesClosing)
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. One of the generic type parameter's count of types that can close exceeds the maximum length allowed ({configuration.MaxTypesClosing}).");
        }

        long totalCombinations = 1;
        foreach (var list in lists)
        {
            totalCombinations *= list.Length;
            if (configuration.MaxGenericTypeRegistrations > 0 && totalCombinations > configuration.MaxGenericTypeRegistrations)
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The total number of generic type registrations exceeds the maximum allowed ({configuration.MaxGenericTypeRegistrations}).");
        }

        return GenerateCombinationsCore(lists, 0, cancellationToken);
    }

    // Cartesian product of the candidate lists. The base case yields one empty combination so that each
    // level has something to prepend to; without it the whole product is empty.
    private static List<List<Type>> GenerateCombinationsCore(Type[][] lists, int depth, CancellationToken cancellationToken)
    {
        if (depth >= lists.Length)
            return [[]];

        cancellationToken.ThrowIfCancellationRequested();

        var childCombinations = GenerateCombinationsCore(lists, depth + 1, cancellationToken);
        var result = new List<List<Type>>(lists[depth].Length * childCombinations.Count);

        foreach (var item in lists[depth])
        {
            foreach (var childCombination in childCombinations)
            {
                var currentCombination = new List<Type>(childCombination.Count + 1) { item };
                currentCombination.AddRange(childCombination);
                result.Add(currentCombination);
            }
        }

        return result;
    }

    // Adds all generic concretions that can close the open request interface by generating concrete types and registering them.
    private static void AddAllConcretionsThatClose(Type openRequestInterface, Type[] concretions, IServiceCollection services, IEnumerable<Assembly> assembliesToScan, MediatRServiceConfiguration configuration, CancellationToken cancellationToken)
    {
        foreach (var concretion in concretions)
        {
            var concreteRequests = GetConcreteRequestTypes(openRequestInterface, concretion, assembliesToScan, configuration, cancellationToken);

            if (concreteRequests is null)
                continue;

            var registrationTypes = concreteRequests.Select(concreteRequest => GetConcreteRegistrationTypes(openRequestInterface, concreteRequest, concretion));

            foreach (var (Service, Implementation) in registrationTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                services.Add(new ServiceDescriptor(Service, Implementation, configuration.Lifetime));
            }
        }
    }

    // Extension to check if an open generic concretion could close to the specified closed generic interface.
    internal static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
    {
        var openInterface = closedInterface.GetGenericTypeDefinition();
        var arguments = closedInterface.GenericTypeArguments;

        var concreteArguments = openConcretion.GenericTypeArguments;
        return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
    }

    // Extension to check if a type can be assigned to another type.
    private static bool CanBeCastTo(this Type pluggedType, Type pluginType)
    {
        if (pluggedType == null) return false;

        if (pluggedType == pluginType) return true;

        return pluginType.IsAssignableFrom(pluggedType);
    }

    // Checks if a type is open generic.
    private static bool IsOpenGeneric(this Type type)
    {
        return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
    }

    // Finds interfaces implemented by a type that close a specified generic interface.
    internal static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
    {
        return FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();
    }

    private static IEnumerable<Type> FindInterfacesThatClosesCore(Type pluggedType, Type templateType)
    {
        if (pluggedType == null) yield break;

        if (!pluggedType.IsConcrete()) yield break;

        if (templateType.IsInterface)
        {
            foreach (var interfaceType in pluggedType.GetInterfaces().Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == templateType))
            {
                yield return interfaceType;
            }
        }
        else if (pluggedType.BaseType!.IsGenericType && pluggedType.BaseType!.GetGenericTypeDefinition() == templateType)
        {
            yield return pluggedType.BaseType!;
        }

        if (pluggedType.BaseType == typeof(object)) yield break;

        foreach (var interfaceType in FindInterfacesThatClosesCore(pluggedType.BaseType!, templateType))
        {
            yield return interfaceType;
        }
    }

    // Checks if a type is concrete (not abstract or interface).
    private static bool IsConcrete(this Type type)
    {
        return !type.IsAbstract && !type.IsInterface;
    }

    // Adds an item to a list if it does not already exist.
    private static void Fill<T>(this IList<T> list, T value)
    {
        if (list.Contains(value)) return;
        list.Add(value);
    }

    // Returns the types an assembly can actually load. Assembly.GetTypes() throws ReflectionTypeLoadException
    // if any single type has an unresolvable dependency; the loadable ones are still available on the exception.
    internal static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.DefinedTypes;
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    /// <summary>
    /// Adds the core MediatR services such as IMediator and pipeline behaviors to the service collection.
    /// Uses TryAdd to avoid overriding existing registrations.
    /// </summary>
    /// <param name="services">The DI service collection to add to.</param>
    /// <param name="serviceConfiguration">Configuration specifying the Mediator implementation and behaviors to register.</param>
    public static void AddRequiredServices(IServiceCollection services, MediatRServiceConfiguration serviceConfiguration)
    {
        // Singleton: the wrapper cache must outlive individual scopes or every request pays the reflection
        // cost again, but it stays owned by this container so it is collected when the container is disposed.
        services.TryAdd(new ServiceDescriptor(typeof(RequestHandlerWrapperCache), new RequestHandlerWrapperCache()));

        // Use TryAdd to preserve existing registrations
        services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfiguration.MediatorImplementationType, serviceConfiguration.Lifetime));

        foreach (var serviceDescriptor in serviceConfiguration.BehaviorsToRegister)
        {
            services.TryAddEnumerable(serviceDescriptor);
        }
    }
}
