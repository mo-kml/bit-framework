﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.OData.Batch;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Query;
using System.Web.OData.Routing.Conventions;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.OData.Contracts;
using Bit.Owin.Contracts;
using Bit.WebApi.ActionFilters;
using Bit.WebApi.Contracts;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Owin;

namespace Bit.OData
{
    public class WebApiODataMiddlewareConfiguration : IOwinMiddlewareConfiguration, IDisposable
    {
        private readonly AppEnvironment _activeAppEnvironment;
        private readonly IEnumerable<IEdmModelProvider> _emdEdmModelProviders;
        private readonly IEnumerable<IWebApiConfigurationCustomizer> _webApiConfgurationCustomizers;
        private readonly System.Web.Http.Dependencies.IDependencyResolver _webApiDependencyResolver;
        private readonly IODataModelBuilderProvider _oDataModelBuilderProvider;
        private HttpConfiguration _webApiConfig;
        private HttpServer _server;
        private ODataBatchHandler _odataBatchHandler;
        private readonly IODataContainerBuilderCustomizer _oDataContainerBuilderCustomizer;
        private readonly IWebApiOwinPipelineInjector _webApiOwinPipelineInjector;

#if DEBUG
        protected WebApiODataMiddlewareConfiguration()
        {
        }
#endif

        public WebApiODataMiddlewareConfiguration(IAppEnvironmentProvider appEnvironmentProvider,
            IEnumerable<IEdmModelProvider> emdEdmModelProviders, IEnumerable<IWebApiConfigurationCustomizer> webApiConfgurationCustomizers, System.Web.Http.Dependencies.IDependencyResolver webApiDependencyResolver, IODataModelBuilderProvider oDataModelBuilderProvider, IODataContainerBuilderCustomizer oDataContainerBuilderCustomizer, IWebApiOwinPipelineInjector webApiOwinPipelineInjector)
        {
            if (emdEdmModelProviders == null)
                throw new ArgumentNullException(nameof(emdEdmModelProviders));

            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            if (webApiConfgurationCustomizers == null)
                throw new ArgumentNullException(nameof(webApiConfgurationCustomizers));

            if (webApiDependencyResolver == null)
                throw new ArgumentNullException(nameof(webApiDependencyResolver));

            if (oDataModelBuilderProvider == null)
                throw new ArgumentNullException(nameof(oDataModelBuilderProvider));

            if (oDataContainerBuilderCustomizer == null)
                throw new ArgumentNullException(nameof(oDataContainerBuilderCustomizer));

            if (webApiOwinPipelineInjector == null)
                throw new ArgumentNullException(nameof(webApiOwinPipelineInjector));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
            _emdEdmModelProviders = emdEdmModelProviders;
            _webApiConfgurationCustomizers = webApiConfgurationCustomizers;
            _webApiDependencyResolver = webApiDependencyResolver;
            _oDataModelBuilderProvider = oDataModelBuilderProvider;
            _oDataContainerBuilderCustomizer = oDataContainerBuilderCustomizer;
            _webApiOwinPipelineInjector = webApiOwinPipelineInjector;
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _webApiConfig = new HttpConfiguration();
            _webApiConfig.SuppressHostPrincipal();

            _webApiConfig.SetTimeZoneInfo(TimeZoneInfo.Utc);

            _webApiConfig.Formatters.Clear();

            _webApiConfig.IncludeErrorDetailPolicy = _activeAppEnvironment.DebugMode ? IncludeErrorDetailPolicy.LocalOnly : IncludeErrorDetailPolicy.Never;

            _webApiConfgurationCustomizers.ToList()
                .ForEach(webApiConfigurationCustomizer =>
                {
                    webApiConfigurationCustomizer.CustomizeWebApiConfiguration(_webApiConfig);
                });

            _webApiConfig.DependencyResolver = _webApiDependencyResolver;

            _server = new HttpServer(_webApiConfig);

            foreach (IGrouping<string, IEdmModelProvider> edmModelProviders in _emdEdmModelProviders.GroupBy(mp => mp.GetEdmName()))
            {
                ODataModelBuilder modelBuilder = _oDataModelBuilderProvider.GetODataModelBuilder(_webApiConfig, containerName: $"{edmModelProviders.Key}Context", @namespace: edmModelProviders.Key);

                foreach (IEdmModelProvider edmModelProvider in edmModelProviders)
                {
                    edmModelProvider.BuildEdmModel(modelBuilder);
                }

                string routeName = $"{edmModelProviders.Key}-odata";

                _odataBatchHandler = new DefaultODataBatchHandler(_server);

                _odataBatchHandler.MessageQuotas.MaxOperationsPerChangeset = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxPartsPerBatch = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxNestingDepth = int.MaxValue;

                _odataBatchHandler.MessageQuotas.MaxReceivedMessageSize = long.MaxValue;

                _odataBatchHandler.ODataRouteName = routeName;

                IEnumerable<IODataRoutingConvention> conventions = ODataRoutingConventions.CreateDefault();

                IEdmModel edmModel = modelBuilder.GetEdmModel();

                _webApiConfig.MapODataServiceRoute(routeName, edmModelProviders.Key, builder =>
                {
                    builder.AddService(ServiceLifetime.Singleton, sp => conventions);
                    builder.AddService(ServiceLifetime.Singleton, sp => edmModel);
                    builder.AddService(ServiceLifetime.Singleton, sp => _odataBatchHandler);
                    builder.AddService(ServiceLifetime.Singleton, sp => _webApiDependencyResolver);
                    _oDataContainerBuilderCustomizer.Customize(builder);
                });
            }

            owinApp.UseAutofacWebApi(_webApiConfig);

            _webApiOwinPipelineInjector.UseWebApiOData(owinApp, _server);

            _webApiConfig.EnsureInitialized();
        }

        public virtual void Dispose()
        {
            _odataBatchHandler?.Dispose();
            _webApiConfig?.Dispose();
            _server?.Dispose();
        }
    }
}
