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
    private static int MaxGenericTypeParameters;
    private static int MaxTypesClosing;
    private static int MaxGenericTypeRegistrations;
    private static int RegistrationTimeout;

    /// <summary>
    /// Sets limits for generic request handler registration such as maximum number of generic parameters,
    /// types that can close those parameters, total registrations allowed, and registration timeout.
    /// </summary>
    /// <param name="configuration">Configuration containing the registration limits.</param>
    public static void SetGenericRequestHandlerRegistrationLimitations(MediatRServiceConfiguration configuration)
    {
        MaxGenericTypeParameters = configuration.MaxGenericTypeParameters;
        MaxTypesClosing = configuration.MaxTypesClosing;
        MaxGenericTypeRegistrations = configuration.MaxGenericTypeRegistrations;
        RegistrationTimeout = configuration.RegistrationTimeout;
    }

    /// <summary>
    /// Adds MediatR request handler classes from specified assemblies, with a timeout to prevent long registration times.
    /// Throws TimeoutException if registration exceeds the configured timeout.
    /// </summary>
    /// <param name="services">The DI service collection to add to.</param>
    /// <param name="configuration">Configuration specifying assemblies and registration options.</param>
    public static void AddMediatRClassesWithTimeout(IServiceCollection services, MediatRServiceConfiguration configuration)
    {
        using (var cts = new CancellationTokenSource(RegistrationTimeout))
        {
            try
            {
                AddMediatRClasses(services, configuration, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("The generic handler registration process timed out.");
            }
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
            .SelectMany(a => a.DefinedTypes)
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
                    services.AddScoped(@interface, type);
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
                    services.TryAddScoped(@interface, type);
                }
            }

            if (!@interface.IsOpenGeneric())
            {
                AddConcretionsThatCouldBeClosed(@interface, concretions, services);
            }
        }

        // Register open generic implementations with DI
        foreach (var @interface in genericInterfaces)
        {
            var exactMatches = genericConcretions.Where(x => x.CanBeCastTo(@interface)).ToArray();
            AddAllConcretionsThatClose(@interface, exactMatches, services, assembliesToScan, cancellationToken);
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
    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
    {
        foreach (var type in concretions.Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAddScoped(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
            }
            catch (Exception)
            {
                // Ignore failures for generic type construction
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
    private static Type[]? GetConcreteRequestTypes(Type openRequestHandlerInterface, Type openRequestHandlerImplementation, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        var constraintsForEachParameter = openRequestHandlerImplementation
            .GetGenericArguments()
            .Select(x => x.GetGenericParameterConstraints())
            .ToArray();

        var typesThatCanCloseForEachParameter = constraintsForEachParameter
            .Select(constraints => assembliesToScan
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && constraints.All(constraint => constraint.IsAssignableFrom(type))).ToArray()
            ).ToArray();

        var requestType = openRequestHandlerInterface.GenericTypeArguments.First();

        if (requestType.IsGenericParameter)
            return null;

        var requestGenericTypeDefinition = requestType.GetGenericTypeDefinition();

        var combinations = GenerateCombinations(requestType, typesThatCanCloseForEachParameter, 0, cancellationToken);

        return combinations.Select(types => requestGenericTypeDefinition.MakeGenericType(types.ToArray())).ToArray();
    }

    /// <summary>
    /// Recursively generates all possible combinations of types to close generic type parameters.
    /// Validates limits for max generic parameters, max types per parameter, and max total registrations.
    /// </summary>
    /// <param name="requestType">The generic request type definition.</param>
    /// <param name="lists">Arrays of types that can close each generic parameter.</param>
    /// <param name="depth">Current recursion depth.</param>
    /// <param name="cancellationToken">Cancellation token to stop processing.</param>
    /// <returns>Enumerable of type lists representing each combination.</returns>
    public static IEnumerable<List<Type>> GenerateCombinations(Type requestType, Type[][] lists, int depth = 0, CancellationToken cancellationToken = default)
    {
        if (depth == 0)
        {
            if (MaxGenericTypeParameters > 0 && lists.Length > MaxGenericTypeParameters)
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The number of generic type parameters exceeds the maximum allowed ({MaxGenericTypeParameters}).");

            foreach (var list in lists)
            {
                if (MaxTypesClosing > 0 && list.Length > MaxTypesClosing)
                    throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. One of the generic type parameter's count of types that can close exceeds the maximum length allowed ({MaxTypesClosing}).");
            }

            long totalCombinations = 1;
            foreach (var list in lists)
            {
                totalCombinations *= list.Length;
                if (MaxGenericTypeParameters > 0 && totalCombinations > MaxGenericTypeRegistrations)
                    throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The total number of generic type registrations exceeds the maximum allowed ({MaxGenericTypeRegistrations}).");
            }
        }

        if (depth >= lists.Length)
        {
            Enumerable.Empty<List<Type>>();
            yield break;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var currentList = lists[depth];
        var childCombinations = GenerateCombinations(requestType, lists, depth + 1, cancellationToken);

        foreach (var item in currentList)
        {
            foreach (var childCombination in childCombinations)
            {
                var currentCombination = new List<Type> { item };
                currentCombination.AddRange(childCombination);
                yield return currentCombination;
            }
        }
    }

    // Adds all generic concretions that can close the open request interface by generating concrete types and registering them.
    private static void AddAllConcretionsThatClose(Type openRequestInterface, Type[] concretions, IServiceCollection services, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        foreach (var concretion in concretions)
        {
            var concreteRequests = GetConcreteRequestTypes(openRequestInterface, concretion, assembliesToScan, cancellationToken);

            if (concreteRequests is null)
                continue;

            var registrationTypes = concreteRequests.Select(concreteRequest => GetConcreteRegistrationTypes(openRequestInterface, concreteRequest, concretion));

            foreach (var (Service, Implementation) in registrationTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                services.AddScoped(Service, Implementation);
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

    /// <summary>
    /// Adds the core MediatR services such as IMediator and pipeline behaviors to the service collection.
    /// Uses TryAdd to avoid overriding existing registrations.
    /// </summary>
    /// <param name="services">The DI service collection to add to.</param>
    /// <param name="serviceConfiguration">Configuration specifying the Mediator implementation and behaviors to register.</param>
    public static void AddRequiredServices(IServiceCollection services, MediatRServiceConfiguration serviceConfiguration)
    {
        // Use TryAdd to preserve existing registrations
        services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfiguration.MediatorImplementationType, serviceConfiguration.Lifetime));

        foreach (var serviceDescriptor in serviceConfiguration.BehaviorsToRegister)
        {
            services.TryAddEnumerable(serviceDescriptor);
        }
    }
}
