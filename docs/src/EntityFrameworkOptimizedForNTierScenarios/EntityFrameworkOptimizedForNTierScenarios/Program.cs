﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Data.Contracts;
using Bit.Data.EntityFramework.Implementations;
using Bit.Model.Contracts;
using Bit.Owin;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Owin;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

namespace EntityFrameworkOptimizedForNTierScenarios
{
    #region Model

    public class Customer : IEntity
    {
        [Key]
        public virtual int Id { get; set; }

        [MaxLength(50)]
        public virtual string FirstName { get; set; }

        [MaxLength(50)]
        public virtual string LastName { get; set; }

        public virtual List<Order> Orders { get; set; } = new List<Order>();
    }

    public class Order : IEntity
    {
        [Key]
        public virtual int Id { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        public virtual int CustomerId { get; set; }

        [MaxLength(50)]
        public virtual string Description { get; set; }
    }

    #endregion

    #region DbContext + Sharp Repository + TestApiController

    public class CustomersDbContextForSharpRepository : DbContext
    {
        public CustomersDbContextForSharpRepository()
            : base(new SqlConnection("Data Source=.;Initial Catalog=CustomersDb;Integrated Security=True"), contextOwnsConnection: true)
        {

        }

        public virtual DbSet<Customer> Customers { get; set; }
    }

    public class SharpRepository<TEntity> : SharpRepository.EfRepository.EfRepository<TEntity>
        where TEntity : class
    {
        public SharpRepository(CustomersDbContextForSharpRepository dbContext)
            : base(dbContext)
        {

        }
    }

    public class CustomersForSharpRepositoryController : ApiController
    {
        private readonly SharpRepository.Repository.IRepository<Customer> _customersSharpRepository;

        public CustomersForSharpRepositoryController(SharpRepository.Repository.IRepository<Customer> customersSharpRepository)
        {
            _customersSharpRepository = customersSharpRepository;
        }

        [Route("customers/get-customers-by-sharp-repository")]
        public List<Customer> GetCustomersBySharpRepository()
        {
            return _customersSharpRepository.GetAll().ToList();
        }
    }

    #endregion

    #region Bit DbContext + Bit Repository + TestApiController

    public class CustomersDbContextForBitRepository : EfDbContextBase
    {
        public CustomersDbContextForBitRepository()
            : base(new SqlConnection("Data Source=.;Initial Catalog=CustomersDb;Integrated Security=True"), contextOwnsConnection: true)
        {

        }

        public virtual DbSet<Customer> Customers { get; set; }
    }

    public class BitRepository<TEntity> : EfRepository<TEntity>
        where TEntity : class, IEntity
    {
        public BitRepository(CustomersDbContextForBitRepository dbContext)
            : base(dbContext)
        {

        }
    }

    public class CustomersForBitRepositoryController : ApiController
    {
        private readonly IRepository<Customer> _customersBitRepository;

        public CustomersForBitRepositoryController(IRepository<Customer> customersBitRepository)
        {
            _customersBitRepository = customersBitRepository;
        }

        [Route("customers/get-customers-by-bit-repository")]
        public List<Customer> GetCustomersByBitRepository()
        {
            return _customersBitRepository.GetAll().ToList();
        }
    }

    #endregion

    public class RepositoriesBenchmarkTest
    {
        private readonly HttpClient client = new HttpClient();
        private readonly string baseAddress = "http://localhost:9000/";

        [Benchmark]
        public void BitRepository() => client.GetAsync($"{baseAddress}/api/customers/get-customers-by-bit-repository").GetAwaiter().GetResult().EnsureSuccessStatusCode();

        [Benchmark]
        public void SharpRepository() => client.GetAsync($"{baseAddress}/api/customers/get-customers-by-sharp-repository").GetAwaiter().GetResult().EnsureSuccessStatusCode();
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Database.SetInitializer<CustomersDbContextForBitRepository>(null);
            Database.SetInitializer<CustomersDbContextForSharpRepository>(null);

            string baseAddress = "http://localhost:9000/";

            using (WebApp.Start<AppStartup>(baseAddress))
            {
                BenchmarkRunner.Run<RepositoriesBenchmarkTest>();
            }
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

            dependencyManager.RegisterDefaultLogger(typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterMinimalOwinMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
                webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });
            });

            #region Register Bit Repository

            dependencyManager.RegisterEfDbContext<CustomersDbContextForBitRepository>();
            dependencyManager.RegisterGeneric(typeof(IRepository<>).GetTypeInfo(), typeof(BitRepository<>).GetTypeInfo());

            #endregion

            #region Register Sharp Repository

            dependencyManager.Register<CustomersDbContextForSharpRepository, CustomersDbContextForSharpRepository>();
            dependencyManager.RegisterGeneric(typeof(SharpRepository.Repository.IRepository<>).GetTypeInfo(), typeof(SharpRepository<>).GetTypeInfo());

            #endregion
        }
    }
}
