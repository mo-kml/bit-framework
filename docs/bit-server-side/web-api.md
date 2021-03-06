# Bit Web API

Web API is a powerful library to develop rest api in .NET world. If you're not familiar with that, you can get start [here](https://www.asp.net/web-api).

Using bit you'll get more benefits from web api. This includes following:

1. We've configured web api on top of [owin](http://owin.org). Owin stands for "Open web interface for .NET" and allows your code to work almost anywhere. We've developed extra codes to make sure your app works well on following workloads:
    - ASP.NET/IIS on windows server & azure web/app services
    - ASP.NET Core/Kestrel on Windows & Linux Servers
    - Self host windows services & azure web jobs
2. We've configured web api on top of [asp.net core/owin branching](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware). Think about something as fast as node js with power of .NET (-:
3. We've developed extensive logging infrastructure in bit framework. It logs everything for you in your app, including web api traces.
4. We've configured headers like [X-Content-Type-Options](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options), [X-CorrelationId](http://theburningmonk.com/2015/05/a-consistent-approach-to-track-correlation-ids-through-microservices/) etc. We've done this to improve logging, security etc.
5. You can protect your web api with bit identity server, a modern single sign on server based on open id/oauth 2.0

## Getting started

After installing [git for windows](https://git-scm.com/download/win) you can run following command in your command line: 
```shell
git clone https://github.com/bit-foundation/bit-framework.git
```

Then open Samples\WebApiSamples\WebApiSamples.sln, then go to 1SimpleWebApi project.

There are several classes there. Program and ValuesController are get copied from [this microsoft docs article](https://docs.microsoft.com/en-us/aspnet/web-api/overview/hosting-aspnet-web-api/use-owin-to-self-host-web-api). It's a good idea to read that article first.

For now, let's ignore the third class: "AppStartup".

Press F5, you'll see ["value1","value2"] at the end of console's output.

So, what's AppStartup class anyway? It configures your app. You'll understand its parts while you're reading docs, but for now let's focus on following codes of that only:

```csharp
dependencyManager.RegisterDefaultWebApiConfiguration();

dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
{
    webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();

    webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
    {
        // You've access to web api's http configuration here
    });
});
```

That code configures web api into your app using the default configuration. Default configuration is all about security, performance, logging etc.

Bit is a very extensible framework developed based on best practices. We've extensively used dependency injection in our code base and you can customize default behaviors based on your requirements.

In following samples, you can find out how to customize web api in bit, but feel free to [drops us an issue in github](https://github.com/bit-foundation/bit-framework/issues), ask a question on [stackoverflow.com](http://stackoverflow.com/questions/tagged/bit-framework) or use comments below if you can't find what you want in these samples.

Note that security samples can be found under [Bit Identity Server](/bit-identity-server.md) (Read it later)

## Samples:

### 1- Web API - Swagger configuration sample

Swagger is the World's Most Popular API Tooling. by Using this sample you can find out how to customize web api in bit.

Read [first part of "Swagger and ASP.NET Web API"](http://wmpratt.com/swagger-and-asp-net-web-api-part-1/)

Differences between our sample (2WebApiSwagger project) and that article:

1- There is no App_Start folder. This is a ASP.NET thing.

2- In following code:

```csharp
GlobalConfiguration.Configuration
  .EnableSwagger(c => c.SingleApiVersion("v1", "A title for your API"))
  .EnableSwaggerUi();
```

[GlobalConfiguration](https://msdn.microsoft.com/en-us/library/system.web.http.globalconfiguration.aspx) uses ASP.NET pipeline directly and it does not work on ASP.NET Core. But bit's config works on both ASP.NET & ASP.NET Core.

3- Instead of adding Swashbuckle package, you've to install Swashbuckle.Core package. Swashbuckle package relies on ASP.NET pipeline and it does not work on ASP.NET Core. But our code works on both.

4- In following code:
```csharp
c.IncludeXmlComments(string.Format(@"{0}\bin\SwaggerDemoApi.XML",           
                           System.AppDomain.CurrentDomain.BaseDirectory));
```
We use #DefaultPathProvider.Current.GetCurrentAppPath()# instead of #System.AppDomain.CurrentDomain.BaseDirectory# which works fine on both ASP.NET/ASP.NET Core. We also use Path.Combine which works fine on linux servers instead of string.Format & \ character usage.

5- We've following code which has no equivalent in the article codes:
```csharp
c.RootUrl(req => new Uri(req.RequestUri, req.GetOwinContext().Request.PathBase.Value /* /api */).ToString());
```
As you see in the article, you open swgger ui by opening http://localhost:51609/swagger/ but in bit's sample, you open http://localhost:9000/api/swagger/. You open /swagger under /api. This is a magic of owin/asp.net core's request branching. That improves your app performance a lot, because it handles request far better than non bit apps. Web api handles web api requests only, signalr handles signalr requests only etc. In non bit apps, web api as an example receives all requests, but does something for those starting with /api (Based on web api configuration you provide). But ASP.NET Core/Owin branching routes every request to its correct handler (Web API to Web API, Signalr to Signalr etc). That line of codes is telling swagger that where is web api, as swagger is not aware of that magic.

So run the second sample and you're good to go (-:

### 2- Web API file upload sample

There is a [question](https://stackoverflow.com/questions/10320232/how-to-accept-a-file-post) on stackoverflow.com about web api file upload.
The important thing you've to notice is "You don't have to use System.Web.dll classes in bit world, even when you're hosting your app on traditional asp.net

By removing usages of that dll, you're going to make sure that your code works well on asp.net core either whenever you migrate your code (which can be done very easily using bit). So drop using #HttpContext.Current and all other members of System.Web.dll#


Web API Attribute routing works fine in bit project, but instead of [Route("api/file-manager/upload")] or [RoutePrefix("api/file-manager")], you've to write [Route("file-manager/upload")] or [RoutePrefix("file-manager)], this means you should not write /api in your attribute routings. That's a side effect of branching, which improves your app performance in turn.

Remember to use async/await and CancellationToken in your Web API codes at it improves your app overall scalability.

So open 3rd sample. It contains upload methods using Web API attribute routing. It uses async/await and CancellationToken and shows you how you can upload files to folders/database.

### 3- Web API - Configuration on ASP.NET

In 4th project (4WebApiAspNetHost), you'll find a bit web api project hosted on ASP.NET/IIS.

##### Differences between this project and previews projects:

1- Instead of Microsoft.Owin.Host.HttpListener nuget package, we've installed Microsoft.Owin.Host.SystemWeb. Using first nuget package, you can "self host" bit server side apps on windows services, console apps, azure job workers etc. Using the second package, you can host bit server side apps on top of ASP.NET/IIS. All codes you've developed are the same (We've copied codes from 2WebApiSwagger project in fact).

##### Differences between this project and default traditional asp.net project: (Ignore this section if you'd like to run your app on top of ASP.NET Core)

1- There is a key to introduce AppStartup class as following:

```xml
<add key="owin:AppStartup" value="WebApiAspNetHost.AppStartup, WebApiAspNetHost" />
```

AppStartup is a class name & WebApiAspNetHost is a namespace. (Second one is assembly name)

2- To prevent asp.net web pages from being started, we've added following

```xml
<add key="webpages:Enabled" value="false" />
```

3- As we've introduced app startup class using "owin:AppStartup" config, we use following to disable automatic search:

```xml
<add key="owin:AutomaticAppStartup" value="false" />
```

4- We've added [enablePrefetchOptimization="true" optimizeCompilations="true"] to improve app overall performance.

5- We've removed all assemblies from being compiled as this is not required in bit projects:

```xml
<assemblies>
    <remove assembly="*" />
</assemblies>
```

6- We've added [enableVersionHeader="false"] to remove extra headers from responses.

7- We've removed all http handlers & modules. They're not required in bit projects.

```xml
<httpModules>
    <clear />
</httpModules>
<httpHandlers>
    <clear />
</httpHandlers>
```

8- We've disabled session as it is not required in bit projects.

```xml
<sessionState mode="Off" />
```

9- By following configs, we've removed extra modules, handlers, headers and default documents. And we've added Owin handler. That handler makes static files, web api, signalr etc work.

```xml
  <system.webServer>
    <validation validateIntegratedModeConfiguration="false" />
    <modules runAllManagedModulesForAllRequests="false">
      <remove name="Session" />
      <remove name="FormsAuthentication" />
      <remove name="DefaultAuthentication" />
      <remove name="RoleManager" />
      <remove name="FileAuthorization" />
      <remove name="AnonymousIdentification" />
      <remove name="Profile" />
      <remove name="UrlMappingsModule" />
      <remove name="ServiceModel-4.0" />
      <remove name="UrlRoutingModule-4.0" />
      <remove name="ScriptModule-4.0" />
      <remove name="WebDAVModule" />
    </modules>
    <defaultDocument>
      <files>
        <clear />
      </files>
    </defaultDocument>
    <handlers>
      <clear />
      <add name="Owin" verb="*" path="*" type="Microsoft.Owin.Host.SystemWeb.OwinHttpHandler, Microsoft.Owin.Host.SystemWeb" />
    </handlers>
    <httpProtocol>
      <customHeaders>
        <clear />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
```

### 4- Web API - Configuration on ASP.NET Core

##### Differences between this project and first project:

1- Instead of Microsoft.Owin.Host.HttpListener nuget package, we've installed Bit.OwinCore. Using Bit.OwinCore, you can host your app on top of asp.net core. ASP.NET core apps can be hosted almost anywhere.

Web API configuration and web api codes are all the same. (-:

### 5- Web API - Configuration on ASP.NET Core / .NET Core

##### Differences between this project and first project:

1- As like as ASP.NET Core with full .NET framework, we've Bit.OwinCore instead of Microsoft.Owin.Host.HttpListener

Web API configuration and web api codes are all the same. (-:

Run .Net core app stesp:

1- Download .NET Core 2 Preview from https://www.microsoft.com/net/core/preview

2- Open Samples\WebApiSamples\6WebApiDotNetCoreHost in visual studio code and press F5 or Open WebApiSamples.sln in Visual Studio 2017 Update 3 Preview

Note that upcoming articles have no .net core sample as we've not officillay supported .net core yet, but after we officially supported .net core, you can start a safe/easy migrate.

### 6- Web API - Dependency Injection samples:

Bit's dependency injection covers you anywhere, from web api to signalr, background job workers, etc. As you see in AppStartup class of samples, there is a dependencyManager variable in both ASP.NET & ASP.NET Core projects. If you register/add some thing with that, it is accessible using constructor and property injection anywhere you need it. Let's take a look at  [sample](https://github.com/bit-foundation/bit-framework/tree/master/Samples/WebApiSamples/7WebApiDependencyInjection)

As you see in that project, there is an IEmailService interface and DefaultEmailService class. We've registered that as following:

```csharp
dependencyManager.Register<IEmailService, DefaultEmailService>();
```

It uses [Autofac](https://autofac.org/) by default. Support for other IOC containers is going to be added soon.

You can also specify life cycle by calling .Register like following:

```csharp
dependencyManager.Register<IEmailService, DefaultEmailService>(lifeCycle: DependencyLifeCycle.InstancePerLifetimeScope);
```

It accepts two lifecyles: InstancePerLifetimeScope & SingleInstance. InstancePerLifetimeScope creates a new instance of your class for every web request, every background job, etc. But SingleInstance creates one instance and use that anywhere. Classes which are registered using InstancePerLifetimeScope have access to IUserInformationProvider which provides you information about the current user and some classes like Entity framework db context and repositories have to be InstancePerLifetimeScope if you intend to follow best practices. (Default lifecycle is InstancePerLifetimeScope if you call .Register without specifying lifecyle)

You've also other Register methods like RegisterGeneric, RegisterInstance, RegisterTypes and RegisterUsing.

If you've got a complex scenario, simply drops us an [issue on github](https://github.com/bit-foundation/bit-framework/issues) or ask a question on [stackoverflow](https://stackoverflow.com/questions/tagged/bit-framework).

You can also cast dependency manager to IAutofacDependencyManager, and after that, you can access [ContainerBuilder](http://docs.autofac.org/en/latest/register/registration.html) of autofac.

```csharp
ContainerBuilder autofacContainerBuilder = ((IAutofacDependencyManager)dependencyManager).GetContainerBuidler();
```

In ASP.NET core projects, you've access to [IServiceCollection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection) by default.