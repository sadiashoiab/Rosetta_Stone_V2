using System;
using System.Diagnostics.CodeAnalysis;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using ClearCareOnline.Api.Services;
using HealthChecks.UI.Client;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Rosetta.ActionFilters;
using Rosetta.HealthChecks;
using Rosetta.Services;

namespace Rosetta
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(AzureADDefaults.BearerAuthenticationScheme)
                .AddAzureADBearer(options => Configuration.Bind("AzureAd", options));

            services.Configure<JwtBearerOptions>(AzureADDefaults.JwtBearerAuthenticationScheme, options =>
            {
                // Reinitialize the options as this has changed to JwtBearerOptions to pick configuration values for attributes unique to JwtBearerOptions
                Configuration.Bind("AzureAd", options);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidAudience = Configuration.GetValue<string>("AzureAd:Audience"),
                    ValidIssuer = $"https://sts.windows.net/{Configuration.GetValue<string>("AzureAd:TenantId")}",
                    ValidateLifetime = true
                };

                // note: leaving this here for debugging purposes when needed.
                //options.Events = new JwtBearerEvents
                //{
                //    OnTokenValidated = context =>
                //    {
                //        var claims = context.Principal.Claims.ToList();
                //        return Task.CompletedTask;
                //    }
                //};
            });

            // note: as we are running under linux
            //////test
            // The following will configure the channel to use the given folder to temporarily
            // store telemetry items during network or Application Insights server issues.
            // User should ensure that the given folder already exists
            // and that the application has read/write permissions.
            services.AddSingleton(typeof(ITelemetryChannel), new ServerTelemetryChannel () {StorageFolder = "/tmp/rosettav2"});
            var appInsightServiceOptions = new ApplicationInsightsServiceOptions {
                EnableDebugLogger = true,
                EnableAdaptiveSampling = false                
            };
            services.AddApplicationInsightsTelemetry(appInsightServiceOptions);

            services.AddHealthChecks()
                .AddApplicationInsightsPublisher()
                .AddAzureKeyVault(setup => { setup.UseKeyVaultUrl(Configuration["KeyVaultUrl"]); })
                .AddCheck<ClearCareOnlineApiHealthCheck>("ClearCare Online API");
            //.AddCheck<RandomHealthCheck>("Random Check");

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromMinutes(3);
                options.Period = TimeSpan.FromMinutes(1);
            });

            // todo: disabling for now as need to get writable volumes working before we can do this here.
            //services.AddHealthChecksUI();

            services.AddLazyCache();

            services
                .AddHttpClient("BearerTokenHttpClient",
                    client => { client.Timeout = System.Threading.Timeout.InfiniteTimeSpan; })
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1)));

            services
                .AddHttpClient("ClearCareHttpClient",
                    client => { client.Timeout = System.Threading.Timeout.InfiniteTimeSpan; })
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1)));

            services.AddControllers(configure => { configure.Filters.Add<IpAddressCaptureActionFilter>(); });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v2", new OpenApiInfo
                {
                    Version = "v2",
                    Title = "RosettaStone API",
                    Description = "RosettaStone ASP.NET Core Web API"
                });
            });

            services.AddSingleton<IAzureKeyVaultService, AzureKeyVaultService>();
            services.AddSingleton<IAzureStorageBlobCacheService, AzureStorageBlobCacheService>();
            services.AddSingleton<IBearerTokenProvider, BearerTokenProvider>();
            services.AddSingleton<IResourceLoader, ResourceLoader>();
            services.AddSingleton<IMapper<AgencyFranchiseMap>, AgencyMapper>();
            services.AddSingleton<IRosettaStoneService, RosettaStoneService>();
            services.AddSingleton<IIpAddressCaptureService, IpAddressCaptureService>();

            services.AddHostedService<CacheRefreshService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                // todo: disabling for now as need to get writable volumes working before we can do this here.
                //endpoints.MapHealthChecksUI();

                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "RosettaStone API V2");
            });
        }
    }
}
