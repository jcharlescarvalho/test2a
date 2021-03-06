using Castle.Core.Internal;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using OpenRasta.Configuration.MetaModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
using OpenRasta.Web;

namespace OpenRasta.DI.Windsor
{
  public class WindsorDependencyResolver :
    DependencyResolverCore,
    IDependencyResolver,
    IModelDrivenDependencyRegistration,
    IRequestScopedResolver,
    IDisposable
  {
    readonly IWindsorContainer _windsorContainer;
    readonly bool _disposeContainerOnCleanup;
    static readonly object ContainerLock = new object();

    public WindsorDependencyResolver() : this(new WindsorContainer(), true)
    {
    }

    static string GetComponentName(Type serviceType)
    {
      return $"openrasta.{serviceType.FullName}.{Guid.NewGuid()}";
    }

    public WindsorDependencyResolver(IWindsorContainer container, bool disposeContainerOnCleanup = false)
    {
      _windsorContainer = container;
      _disposeContainerOnCleanup = disposeContainerOnCleanup;

      _windsorContainer.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel, true));

      if (_windsorContainer.Kernel.GetFacilities().All(x => x.GetType() != typeof(TypedFactoryFacility)))
      {
        _windsorContainer.AddFacility<TypedFactoryFacility>();
      }

      _windsorContainer.Register(
        Component
          .For<IDependencyResolver, IModelDrivenDependencyRegistration>()
          .Instance(this)
          .OnlyNewServices());

      _windsorContainer.Register(Component
        .For<ScopedInstanceStore>()
        .Named(GetComponentName(typeof(ScopedInstanceStore)))
        .ImplementedBy<ScopedInstanceStore>()
        .LifestyleScoped());

      _windsorContainer.Register(Component
        .For<ICommunicationContext>()
        .Named(GetComponentName(typeof(ICommunicationContext)))
        .UsingFactoryMethod(kernel => kernel.Resolve<ScopedInstanceStore>().Context)
        .LifestyleScoped());
      
      _windsorContainer.Register(Component
        .For<IRequest>()
        .Named(GetComponentName(typeof(IRequest)))
        .UsingFactoryMethod(kernel => kernel.Resolve<ScopedInstanceStore>().Context.Request)
        .LifestyleScoped());
      
      _windsorContainer.Register(Component
        .For<IResponse>()
        .Named(GetComponentName(typeof(IResponse)))
        .UsingFactoryMethod(kernel => kernel.Resolve<ScopedInstanceStore>().Context.Response)
        .LifestyleScoped());
    }

    public bool HasDependency(Type serviceType)
    {
      return serviceType != null && _windsorContainer.Kernel.GetHandlers(serviceType).Any();
    }

    public bool HasDependencyImplementation(Type serviceType, Type concreteType)
    {
      return
        _windsorContainer.Kernel.GetHandlers(serviceType)
          .Any(h => h.ComponentModel.Implementation == concreteType);
    }

    public void HandleIncomingRequestProcessed()
    {
      throw new NotSupportedException("Unsupported, are you sure you're using OpenRasta 2.6?");
    }

    static readonly ConcurrentDictionary<Type, Type> EnumMappings = new ConcurrentDictionary<Type, Type>();

    protected override object ResolveCore(Type serviceType)
    {
      var enumType = EnumMappings.GetOrAdd(serviceType, type => serviceType.GetCompatibleArrayItemType());

      return enumType != null
        ? _windsorContainer.ResolveAll(enumType)
        : _windsorContainer.Resolve(serviceType);
    }

    protected override IEnumerable<TService> ResolveAllCore<TService>()
    {
      return _windsorContainer.ResolveAll<TService>();
    }

    protected override void AddDependencyCore(Type dependent, Type concrete, DependencyLifetime lifetime)
    {
      string componentName = GetComponentName(dependent);
      lock (ContainerLock)
      {
        _windsorContainer.Register(Component.For(dependent).ImplementedBy(concrete).Named(componentName).LifeStyle
          .Is(ConvertLifestyles.ToLifestyleType(lifetime)));
      }
    }

    static readonly Type CommContext = typeof(ICommunicationContext);
    static readonly Type Request = typeof(IRequest);
    static readonly Type Response = typeof(IResponse);

    protected override void AddDependencyInstanceCore(Type serviceType, object instance, DependencyLifetime lifetime)
    {
      if (serviceType == Request || serviceType == Response) return;
      if (serviceType == CommContext)
      {
        _windsorContainer.Resolve<ScopedInstanceStore>().Context = (ICommunicationContext) instance;
        return;
      }

      lock (ContainerLock)
      {
        if (lifetime == DependencyLifetime.PerRequest)
        {
          // try to see if we have a registration already
          if (!_windsorContainer.Kernel.HasComponent(serviceType))
            // if there's already an instance registration we update the store with the correct reg.
            _windsorContainer.Register(Component
              .For(serviceType)
              .Named(GetComponentName(serviceType))
              .UsingFactoryMethod(kernel => kernel.Resolve<ScopedInstanceStore>().GetInstance(serviceType))
              .LifestyleScoped());

          _windsorContainer.Resolve<ScopedInstanceStore>().SetInstance(serviceType, instance);
        }
        else
        {
          _windsorContainer.Register(Component
            .For(serviceType)
            .Instance(instance)
            .Named(GetComponentName(serviceType))
            .IsDefault()
            .LifeStyle.Is(ConvertLifestyles.ToLifestyleType(lifetime)));
        }
      }
    }

    protected override void AddDependencyCore(Type handlerType, DependencyLifetime lifetime)
    {
      AddDependencyCore(handlerType, handlerType, lifetime);
    }

    public void Dispose()
    {
      if (_disposeContainerOnCleanup)
      {
        _windsorContainer?.Dispose();
      }
    }

    public void Register(DependencyFactoryModel registration)
    {
      var genericTypeDef = registration.GetType().GetGenericTypeDefinition();
      var args = registration.GetType().GenericTypeArguments;
      var allArgs = new[] {registration.ServiceType}.Concat(args).ToArray();
      Type registrarType;
      if (genericTypeDef == typeof(DependencyFactoryModel<>))
        registrarType = typeof(FactoryRegistration<,>).MakeGenericType(allArgs);
      else if (genericTypeDef == typeof(DependencyFactoryModel<,>))
        registrarType = typeof(FactoryRegistration<,,>).MakeGenericType(allArgs);
      else if (genericTypeDef == typeof(DependencyFactoryModel<,,>))
        registrarType = typeof(FactoryRegistration<,,,>).MakeGenericType(allArgs);
      else if (genericTypeDef == typeof(DependencyFactoryModel<,,,>))
        registrarType = typeof(FactoryRegistration<,,,,>).MakeGenericType(allArgs);
      else if (genericTypeDef == typeof(DependencyFactoryModel<,,,,>))
        registrarType = typeof(FactoryRegistration<,,,,,>).MakeGenericType(allArgs);
      else
        throw new NotSupportedException();
      var registrar = (IRegisterFactories) Activator.CreateInstance(registrarType);
      registrar.Register(GetComponentName(registration.ServiceType), _windsorContainer, registration);
    }

    public IDisposable CreateRequestScope()
    {
      return _windsorContainer.BeginScope();
    }

    interface IRegisterFactories
    {
      void Register(string componentName, IWindsorContainer container, DependencyFactoryModel model);
    }

    class FactoryRegistration<TService, TConcrete> : IRegisterFactories
      where TConcrete : TService where TService : class
    {
      public void Register(string componentName, IWindsorContainer container, DependencyFactoryModel registration)
      {
        if (registration.Factory == null)
        {
          container.Register(Component
            .For<TService>()
            .Named(componentName)
            .ImplementedBy<TConcrete>()
            .LifeStyle.Is(ConvertLifestyles.ToLifestyleType(registration.Lifetime)));
        }
        else
        {
          var factoryMethod = ((Expression<Func<TConcrete>>) registration.Factory).Compile();
          container.Register(
            Component.For<TService>()
              .Named(componentName)
              .UsingFactoryMethod(factoryMethod)
              .LifeStyle.Is(ConvertLifestyles.ToLifestyleType(registration.Lifetime)));
        }
      }
    }

    static Func<IKernel, T> GetResolver<T>()
    {
      var serviceType = typeof(T);
      var isEnumerable = serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
      // todo maybe add array too?
      var itemType = isEnumerable ? serviceType.GetGenericArguments()[0] : null;
      return kernel => isEnumerable ? (T) (object) kernel.ResolveAll(itemType) : kernel.Resolve<T>();
    }

    class FactoryRegistration<TService, TArg, TConcrete> : IRegisterFactories
      where TService : class where TConcrete : TService
    {
      public void Register(string componentName, IWindsorContainer container, DependencyFactoryModel registration)
      {
        var resolver = GetResolver<TArg>();
        var factoryMethod = ((Expression<Func<TArg, TConcrete>>) registration.Factory).Compile();
        container.Register(
          Component.For<TService>()
            .Named(componentName)
            .UsingFactoryMethod(kernel => factoryMethod(resolver(kernel)))
            .LifeStyle.Is(ConvertLifestyles.ToLifestyleType(registration.Lifetime)));
      }
    }

    class FactoryRegistration<TService, TArg1, TArg2, TConcrete> : IRegisterFactories
      where TService : class where TConcrete : TService
    {
      public void Register(string componentName, IWindsorContainer container, DependencyFactoryModel registration)
      {
        var arg1 = GetResolver<TArg1>();
        var arg2 = GetResolver<TArg2>();

        var factoryMethod = ((Expression<Func<TArg1, TArg2, TConcrete>>) registration.Factory).Compile();
        container.Register(
          Component.For<TService>()
            .Named(componentName)
            .UsingFactoryMethod(kernel => factoryMethod(
              arg1(kernel),
              arg2(kernel)))
            .LifeStyle.Is(ConvertLifestyles.ToLifestyleType(registration.Lifetime)));
      }
    }

    class FactoryRegistration<TService, TArg1, TArg2, TArg3, TConcrete> : IRegisterFactories
      where TService : class where TConcrete : TService
    {
      public void Register(string componentName, IWindsorContainer container, DependencyFactoryModel registration)
      {
        var arg1 = GetResolver<TArg1>();
        var arg2 = GetResolver<TArg2>();
        var arg3 = GetResolver<TArg3>();
        var factoryMethod = ((Expression<Func<TArg1, TArg2, TArg3, TConcrete>>) registration.Factory).Compile();
        container.Register(
          Component.For<TService>()
            .Named(componentName)
            .UsingFactoryMethod(kernel => factoryMethod(
              arg1(kernel),
              arg2(kernel),
              arg3(kernel)))
            .LifeStyle.Is(ConvertLifestyles.ToLifestyleType(registration.Lifetime)));
      }
    }

    class FactoryRegistration<TService, TArg1, TArg2, TArg3, TArg4, TConcrete> : IRegisterFactories
      where TService : class where TConcrete : TService
    {
      public void Register(string componentName, IWindsorContainer container, DependencyFactoryModel registration)
      {
        var arg1 = GetResolver<TArg1>();
        var arg2 = GetResolver<TArg2>();
        var arg3 = GetResolver<TArg3>();
        var arg4 = GetResolver<TArg4>();
        var factoryMethod = ((Expression<Func<TArg1, TArg2, TArg3, TArg4, TConcrete>>) registration.Factory).Compile();
        container.Register(
          Component.For<TService>()
            .Named(componentName)
            .UsingFactoryMethod(kernel => factoryMethod(
              arg1(kernel),
              arg2(kernel),
              arg3(kernel),
              arg4(kernel)))
            .LifeStyle.Is(ConvertLifestyles.ToLifestyleType(registration.Lifetime)));
      }
    }
  }
}