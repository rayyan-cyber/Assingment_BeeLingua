using System;
using System.IO;
using System.Reflection;
using Assingment_BeeLingua.BLL;
using azure_functions_cosmosclient;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup (typeof(Startup))]
[assembly: WebJobsStartup(typeof(Assingment_BeeLingua.API.StartupSwagger))]
namespace azure_functions_cosmosclient
{
    public class Startup : FunctionsStartup
    {
        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(s => {
                var connectionString = Configuration.GetConnectionString("CosmosDB");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        "Please specify a valid CosmosDBConnection in the appSettings.json file or your Azure Functions Settings.");
                }

                return new CosmosClientBuilder(connectionString)
                    .Build();
            });

            builder.AddSwashBuckle(Assembly.GetExecutingAssembly());
            
        }
    }
}
