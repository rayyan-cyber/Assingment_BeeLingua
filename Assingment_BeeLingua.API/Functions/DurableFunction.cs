using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Assingment_BeeLingua.API.Functions
{
    public static class DurableFunction
    {
        [FunctionName("CakeFunction")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            
            outputs.Add(await context.CallActivityAsync<string>("OrderingCake_Flow", "Ordering"));
            outputs.Add(await context.CallActivityAsync<string>("OrderingCake_Flow", "Making"));
            outputs.Add(await context.CallActivityAsync<string>("OrderingCake_Flow", "Payment"));

            // returns ["Ordering Cake!", "Making Cake!", "Payment Cake!"]
            return outputs;
        }

        [FunctionName("OrderingCake_Flow")]
        public static string SayHello([ActivityTrigger] string flow, ILogger log)
        {
            log.LogInformation($"---- getting flow {flow}");
            return $"{flow} Cake!";
        }

        [FunctionName("CakeFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("CakeFunction", null);

            //log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}