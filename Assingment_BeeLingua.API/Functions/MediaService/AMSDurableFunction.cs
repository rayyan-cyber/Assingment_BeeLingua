using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Assingment_BeeLingua.BLL;
using Assingment_BeeLingua.BLL.MediaService;
using Assingment_BeeLingua.DAL.Models.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Assingment_BeeLingua.DAL.Repository.Repositories;

namespace Assingment_BeeLingua.API.Functions.MediaService
{
    public class AMSDurableFunction
    {
        private readonly MediaServiceService _mediaService;
        private readonly string envAmsCredential = Environment.GetEnvironmentVariable("AMSCredential");
        private readonly string encoderName = Environment.GetEnvironmentVariable("AdaptiveStreamingTransformName");
        private const string OutputFolderName = @"Output";
        private ConfigAsset _configAsset = new ConfigAsset();

        public AMSDurableFunction(CosmosClient client)
        {
            _mediaService ??= new MediaServiceService(new MediaServiceRepository(client));
        }

        [FunctionName("AMS_HttpStart")]
        public async Task<IActionResult> AMS_HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "AMS/Start")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<DFJobInputDTO>(requestBody);

            var instanceId = await starter.StartNewAsync("AMS_Orchestrator", input);
            var payload = starter.CreateHttpManagementPayload(instanceId);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return new OkObjectResult(new
            {
                instanceId,
                terminatePostUri = payload.TerminatePostUri,
                sendEventPostUri = payload.SendEventPostUri,
                purgeHistoryDeleteUri = payload.PurgeHistoryDeleteUri,
            });
        }

        [FunctionName("AMS_Orchestrator")]
        public async Task AMS_Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var input = context.GetInput<DFJobInputDTO>();
            try 
            {
                var assetAMS = await context.CallActivityAsync<AssetAMS>("AMS_GetAssetAMS", input.ResourceId);
                if (assetAMS == null)
                    throw new Exception("resource is null");

                var encode = await context.CallActivityAsync<Task>("AMS_Encode", assetAMS);

                Job job = await context.CallActivityAsync<Job>("AMS_Job", assetAMS);
                

                var param = new PropFinalize()
                {
                    AssetAMS = assetAMS,
                    JobInput = input
                };

                var manifest = await context.CallActivityAsync<Amsv3Manifest>("AMS_Finalize", param);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogInformation(ex.StackTrace);
            }
            
        }

        
        #region Get AssetAMS
        [FunctionName("AMS_GetAssetAMS")]
        public async Task<AssetAMS> AMS_GetAssetAMS(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            var resourceId = activityContext.GetInput<string>();

            log.LogInformation("--- getting resource information " + resourceId);
            var resource = await _mediaService.GetAssetAMS(resourceId); 
            return resource;
        }
        #endregion

        #region AMS Encode
        [FunctionName("AMS_Encode")]
        public async Task AMS_Encode(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            try
            {
                //var encoderName = Environment.GetEnvironmentVariable("AdaptiveStreamingTransformName");

                log.LogInformation("--- ams > prepare");
                var resource = activityContext.GetInput<AssetAMS>();

                log.LogInformation("--- ams > init Media Services for " + resource.Id);
                resource.Status = "encoding";
                await _mediaService.UpdateAssetAMS(resource.Id, resource);

                log.LogInformation("--- ams > init Media Services");
                var ams = await _mediaService.GetCredentialAsync(JsonConvert
                        .DeserializeObject<Credential>(envAmsCredential));
                IAzureMediaServicesClient client = ams.Client;

                string contentAddress = resource.ContentAddress;
                string inputAssetName = string.Format(_configAsset.InputName, contentAddress);
                string resultName = string.Format(_configAsset.OutputName, contentAddress);
                string jobName = string.Format(_configAsset.JobName, contentAddress);

                log.LogInformation("--- ams > create output asset");
                var outputAsset = new Asset(name: resultName, container: resultName, description: $"encode \"{resource.Filename}\"");
                await ams.Client.Assets.DeleteAsync(ams.ResourceGroup, ams.AccountName,
                    resultName);
                outputAsset = await ams.Client.Assets.CreateOrUpdateAsync(ams.ResourceGroup,
                    ams.AccountName, resultName, outputAsset);

                log.LogInformation("--- ams > init job");
                _ = await _mediaService.SubmitJobAsync(client, ams.ResourceGroup, ams.AccountName, encoderName, jobName, inputAssetName, resultName);
            }
            catch (Exception e)
            {
                log.LogError("===================================================");
                log.LogError(e.Message);
                log.LogError(e.StackTrace);
                log.LogError("===================================================");
                throw new FunctionFailedException(e.Message);
            }

        }
        #endregion

        #region Job
        [FunctionName("AMS_Job")]
        public async Task<Job> AMS_Job(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            var resource = activityContext.GetInput<AssetAMS>();

            string jobName = string.Format(_configAsset.JobName, resource.ContentAddress);
            string locatorName = string.Format(_configAsset.LocatorName, resource.ContentAddress);
            string outputName = string.Format(_configAsset.OutputName, resource.ContentAddress);

            var ams = await GetAmsCredential(log);
            IAzureMediaServicesClient client = ams.Client;

            Job job = await _mediaService.WaitForJobToFinishAsync(client, ams.ResourceGroup, ams.AccountName, encoderName, jobName);
            var listUrl = new List<string>();
            if (job.State == JobState.Finished)
            {
                Console.WriteLine("Job finished.");
                if (!Directory.Exists(OutputFolderName))
                    Directory.CreateDirectory(OutputFolderName);

                await _mediaService.DownloadOutputAssetAsync(client, ams.ResourceGroup, ams.AccountName, outputName, OutputFolderName);

                StreamingLocator locator = await _mediaService.CreateStreamingLocatorAsync(client, ams.ResourceGroup, ams.AccountName, outputName, locatorName);

                IList<string> urls = await _mediaService.GetStreamingUrlsAsync(client, ams.ResourceGroup, ams.AccountName, locator.Name);
                foreach (var url in urls)
                {
                    listUrl.Add(url);
                }
            }
            if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            return job;

        }
        #endregion

        #region Finalize
        [FunctionName("AMS_Finalize")]
        public async Task<Amsv3Manifest> AMS_Finalize(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            var prop = activityContext.GetInput<PropFinalize>();

            var ams = await GetAmsCredential(log);
            string outputName = string.Format(_configAsset.OutputName, prop.AssetAMS.ContentAddress); 
            
            var sas = await ams.Client.Assets.ListContainerSasAsync(
                ams.ResourceGroup,
                ams.AccountName,
                outputName, 
                permissions: AssetContainerPermission.Read,
                expiryTime: DateTime.UtcNow.AddMinutes(1).ToUniversalTime());
            Uri containerSasUrl = new Uri(sas.AssetContainerSasUrls.FirstOrDefault());
            BlobResultSegment segment = await new CloudBlobContainer(containerSasUrl)
                .ListBlobsSegmentedAsync(null);

            var getPreview = new List<Task>();
            var previewImage = new List<byte[]>();

            Amsv3Manifest manifest = null;
            foreach (IListBlobItem blobItem in segment.Results)
            {
                CloudBlockBlob blob = blobItem as CloudBlockBlob;
                if (blob == null) continue;

                var name = blob.Name.ToLower();
                if (name.Contains("_manifest.json"))
                {
                    log.LogInformation("--- ams > getting manifest");
                    manifest = JsonConvert.DeserializeObject<Amsv3Manifest>(
                        await blob.DownloadTextAsync());
                }

                if (name.Contains("jpg"))
                {
                    log.LogInformation("--- ams > getting preview " + getPreview.Count);
                    getPreview.Add(Task.Run(async () =>
                    {
                        MemoryStream _stream = new MemoryStream();
                        await blob.DownloadToStreamAsync(_stream);
                        previewImage.Add(_stream.ToArray());
                    }));
                }
            }
            await Task.WhenAll(getPreview);

            //// saving first thumbnail
            //log.LogInformation("--- saving preview image (jpg)");
            //var path = envThumbnailPath.Replace("{fileName}", $"{prop.Resource.Id}.jpg");
            //var thumb = await ResourceController.SaveThumbnail(path, previewImage.FirstOrDefault());

            log.LogInformation("--- ams > updating status data to publish");
            prop.AssetAMS.Status = "publish";
            prop.AssetAMS.Duration = ParseDuration(manifest.AssetFile.FirstOrDefault().Duration).ToString();
            await _mediaService.UpdateAssetAMS(prop.AssetAMS.Id, prop.AssetAMS);


            //using (var httpClient = new HttpClient())
            //{
            //    var content = JsonConvert.SerializeObject(new
            //    {
            //        resourceId = prop.JobInput.ResourceId,
            //        duration = prop.AssetAMS.Duration,
            //    });

            //    // new implement
            //    var url = envContentResourceUrl.Replace("{status}", "publish");

            //    // legacy
            //    if (prop.JobInput.EventPostUrl != null)
            //    {
            //        url = prop.JobInput.EventPostUrl.Replace("{eventName}", "event-job-finish");
            //    }

            //    var body = new StringContent(content, Encoding.UTF8, "application/json");
            //    await httpClient.PostAsync(url, body);
            //}

            return manifest;
        }
        #endregion

        #region Models
        public class DFJobInputDTO
        {

            [JsonProperty(propertyName: "eventPostUrl")]
            public string EventPostUrl { get; set; }

            [JsonProperty(propertyName: "resourceId")]
            public string ResourceId { get; set; }
        }

        public class PropFinalize
        {
            [JsonProperty(propertyName: "jobInput")]
            public DFJobInputDTO JobInput;

            [JsonProperty(propertyName: "assetAMS")]
            public AssetAMS AssetAMS;
        }
        #endregion

        #region Method
        public async Task<Credential> GetAmsCredential(ILogger log)
        {
            log.LogInformation("--- ams > init Media Services");
            return await _mediaService.GetCredentialAsync(JsonConvert
                        .DeserializeObject<Credential>(envAmsCredential));
        }

        public static int ParseDuration(string stdDuration)
        {
            return (int)Math.Round(XmlConvert.ToTimeSpan(stdDuration).TotalSeconds);
        }
        #endregion
    }
}