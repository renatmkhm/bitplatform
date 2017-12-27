﻿using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace EntityFrameworkSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9000/";

            using (WebApp.Start<AppStartup>(url: baseAddress))
            {
                HttpClient client = new HttpClient();

                HttpResponseMessage response = client.GetAsync($"{baseAddress}api/customers/get-customers").GetAwaiter().GetResult();

                Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                Console.WriteLine("Press any key to exit...");

                Console.ReadKey();
            }
        }
    }

    public class Customer : IEntity
    {
        public virtual int Id { get; set; }

        public virtual string FirstName { get; set; }

        public virtual string LastName { get; set; }

        public virtual List<Order> Orders { get; set; } = new List<Order>();
    }

    public class Order : IEntity
    {
        public virtual int Id { get; set; }

        public virtual string Description { get; set; }

        public virtual int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }
    }

    [DbConfigurationType(typeof(UseDefaultModelStoreDbConfiguration))]
    public class MyAppDbContext : EfDbContextBase
    {
        public MyAppDbContext()
            : base(DefaultAppEnvironmentProvider.Current.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"))
        {

        }

        public MyAppDbContext(IAppEnvironmentProvider appEnvironmentProvider, IDbConnectionProvider dbConnectionProvider)
            : base(appEnvironmentProvider.GetActiveAppEnvironment().GetConfig<string>("AppConnectionString"), dbConnectionProvider)
        {

        }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Order> Orders { get; set; }
    }

    public class MyAppRepository<TEntity> : EfRepository<TEntity>
        where TEntity : class, IEntity
    {

    }

    public class CustomersController : ApiController
    {
        public virtual IRepository<Customer> CustomersRepository { get; set; }

        [Route("customers/get-customers")]
        public virtual async Task<List<Customer>> GetCustomers(CancellationToken cancellationToken)
        {
            return await (await CustomersRepository.GetAllAsync(cancellationToken)).ToListAsync(cancellationToken);
        }

        public virtual async Task AddNewCustomer(CancellationToken cancellationToken, Customer customer)
        {
            await CustomersRepository.AddAsync(customer, cancellationToken);
        }
    }

    public interface IOrdersRepository : IRepository<Order>
    {
        Task<long> GetOrdersCount(CancellationToken cancellationToken);
    }

    public class OrdersRepository : MyAppRepository<Order>, IOrdersRepository
    {
        public virtual async Task<long> GetOrdersCount(CancellationToken cancellationToken)
        {
            return await (await GetAllAsync(cancellationToken)).LongCountAsync(cancellationToken);
        }
    }

    public class OrdersController : ApiController
    {
        public virtual IOrdersRepository OrdersRepository { get; set; }

        [Route("orders/get-orders-count")]
        public virtual async Task<long> GetOrdersCount(CancellationToken cancellationToken)
        {
            return await OrdersRepository.GetOrdersCount(cancellationToken);
        }
    }

    public class MyAppDbContextInitializer : IAppEvents
    {
        public virtual void OnAppStartup()
        {
            using (MyAppDbContext dbContext = new MyAppDbContext())
            {
                dbContext.Database.Initialize(force: true);
            }
        }

        public virtual void OnAppEnd()
        {

        }
    }

    public class AppStartup : OwinAppStartup, IOwinDependenciesManager, IDependenciesManagerProvider
    {
        public override void Configuration(IAppBuilder owinApp)
        {
            DefaultDependenciesManagerProvider.Current = this;

            base.Configuration(owinApp);
        }

        public IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }

        public void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterMinimalOwinMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
            });

            dependencyManager.Register<IDbConnectionProvider, DefaultDbConnectionProvider<SqlConnection>>();
            dependencyManager.RegisterEfDbContext<MyAppDbContext>();
            dependencyManager.RegisterAppEvents<MyAppDbContextInitializer>();
            dependencyManager.RegisterRepository(typeof(MyAppRepository<>).GetTypeInfo());
            dependencyManager.RegisterRepository(typeof(OrdersRepository).GetTypeInfo());
        }
    }
}
