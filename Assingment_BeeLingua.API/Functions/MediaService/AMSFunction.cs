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
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Assingment_BeeLingua.DAL.Repository.Repositories;
using static Assingment_BeeLingua.BLL.MediaService.AzureStorageAccountService;
using Assingment_BeeLingua.API.Functions.MediaService.DTO;

namespace Assingment_BeeLingua.API.Functions.MediaService
{
    public class AMSFunction
    {
        private readonly MediaServiceService _mediaService;

        private readonly string envAmsCredential = Environment.GetEnvironmentVariable("AMSCredential");
        public static readonly string envPhysicalFilesDir = @"Upload\";//Z:\Ecomindo\BeeLingua\MediaService\";
        private readonly string envEncoderName = Environment.GetEnvironmentVariable("StandardStreamingTransformName");
        private readonly string envThumbnailPath = Environment.GetEnvironmentVariable("ResourceThumbnailPath");
        public static readonly string envCAccountName = "stbltutorial";
        public static readonly string envCAccountKey = "7adoccKevApsFz88+kc50Q85R42EAAxhU78ITpoEM6s18Q0iXgOLagfPgRJgummPs5cTS6Y/WTVqCKvSm5rfgA==";


        private ConfigAsset _configAsset = new ConfigAsset();

        public AMSFunction(CosmosClient client)
        {
            _mediaService ??= new MediaServiceService(new MediaServiceRepository(client));
        }

        [FunctionName("AMS_HttpStart")]
        public async Task<IActionResult> AMS_HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "AMS/StartEncode")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var reqBody = JsonConvert.DeserializeObject<DFJobInputDTO>(requestBody);

            var assetAMS = await _mediaService.GetAssetAMS(reqBody.AssetId);
            var configAsset = new ConfigAssetDTO()
            {
                 AssetID = reqBody.AssetId,
                 InputName = string.Format(_configAsset.InputName, assetAMS.ContentAddress),
                 OutputName = string.Format(_configAsset.OutputName, assetAMS.ContentAddress),
                 JobName = string.Format(_configAsset.JobName, assetAMS.ContentAddress),
                 LocatorName = string.Format(_configAsset.LocatorName, assetAMS.ContentAddress)
            };
            var inputConfigAsset = JsonConvert.SerializeObject(configAsset);

            var instanceId = await starter.StartNewAsync("AMS_RunEncode", input:inputConfigAsset);
            var payload = starter.CreateHttpManagementPayload(instanceId);

            return new OkObjectResult(new
            {
                instanceId,
                terminatePostUri = payload.TerminatePostUri,
                sendEventPostUri = payload.SendEventPostUri,
                purgeHistoryDeleteUri = payload.PurgeHistoryDeleteUri,
            });
        }

        [FunctionName("AMS_RunEncode")]
        public async Task<List<string>> AMS_RunEncode(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var input = context.GetInput<string>();
            var outputs = new List<string>();
            try 
            {
                var encode = await context.CallActivityAsync<Task>("AMS_Encode", input);

                Job job = await context.CallActivityAsync<Job>("AMS_Job", input);
                
                var streamingUrl = await context.CallActivityAsync<string>("AMS_StreamingURL", input);
                outputs.Add($"Streaming URL : {streamingUrl}");

                var manifest = await context.CallActivityAsync<Amsv3Manifest>("AMS_Finalize", input);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogInformation(ex.StackTrace);
            }

            return outputs;
        }

        #region Durable Function
        #region Get AssetAMS
        [FunctionName("AMS_GetAssetAMS")]
        public async Task<AssetAMS> AMS_GetAssetAMS(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            var resourceId = activityContext.GetInput<string>();
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
                string inputValue = activityContext.GetInput<string>();
                var data = JsonConvert.DeserializeObject<ConfigAssetDTO>(inputValue);
                var assetAMS = await _mediaService.GetAssetAMS(data.AssetID);
                assetAMS.Status = "encoding";
                await _mediaService.UpdateAssetAMS(assetAMS.Id, assetAMS);

                var ams = await _mediaService.GetCredentialAsync(JsonConvert
                        .DeserializeObject<Credential>(envAmsCredential));
                IAzureMediaServicesClient client = ams.Client;

                await _mediaService.GetOrCreateTransformAsync(client, ams.ResourceGroup, ams.AccountName, envEncoderName);

                _ = await _mediaService.CreateOutputAssetAsync(client, ams.ResourceGroup, ams.AccountName, data.OutputName, assetAMS.Filename);
                _ = await _mediaService.SubmitJobAsync(client, ams.ResourceGroup, ams.AccountName, envEncoderName, data.JobName, data.InputName, data.OutputName);
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
            string inputValue = activityContext.GetInput<string>();
            var data = JsonConvert.DeserializeObject<ConfigAssetDTO>(inputValue);
            var ams = await GetAmsCredential(log);
            IAzureMediaServicesClient client = ams.Client;

            Job job = await _mediaService.WaitForJobToFinishAsync(client, ams.ResourceGroup, ams.AccountName, envEncoderName, data.JobName);
            if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            return job;

        }
        #endregion

        #region Streaming URL
        [FunctionName("AMS_StreamingURL")]
        public async Task<string> AMS_StreamingURL(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            string inputValue = activityContext.GetInput<string>();
            var data = JsonConvert.DeserializeObject<ConfigAssetDTO>(inputValue);
            var assetAMS = await _mediaService.GetAssetAMS(data.AssetID);

            var ams = await GetAmsCredential(log);
            IAzureMediaServicesClient client = ams.Client;
            
            var listUrl = new List<string>();
            StreamingLocator locator = await _mediaService.CreateStreamingLocatorAsync(client, ams.ResourceGroup, ams.AccountName, data.OutputName, data.LocatorName);

            IList<string> urls = await _mediaService.GetStreamingUrlsAsync(client, ams.ResourceGroup, ams.AccountName, locator.Name);
            foreach (var url in urls)
            {
                listUrl.Add(url);
            }

            string listStreamingURL = string.Join(",", listUrl.ToArray());
            assetAMS.StreamingUrl = listStreamingURL;
            await _mediaService.UpdateAssetAMS(data.AssetID, assetAMS);
            return listStreamingURL;

        }
        #endregion

        #region Finalize
        [FunctionName("AMS_Finalize")]
        public async Task<Amsv3Manifest> AMS_Finalize(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            string inputValue = activityContext.GetInput<string>();
            var data = JsonConvert.DeserializeObject<ConfigAssetDTO>(inputValue);
            var assetAMS = await _mediaService.GetAssetAMS(data.AssetID);

            var ams = await GetAmsCredential(log);
            
            var sas = await ams.Client.Assets.ListContainerSasAsync(
                ams.ResourceGroup,
                ams.AccountName,
                data.OutputName, 
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
                    manifest = JsonConvert.DeserializeObject<Amsv3Manifest>(
                        await blob.DownloadTextAsync());
                }

                if (name.Contains("jpg"))
                {
                    getPreview.Add(Task.Run(async () =>
                    {
                        MemoryStream _stream = new MemoryStream();
                        await blob.DownloadToStreamAsync(_stream);
                        previewImage.Add(_stream.ToArray());
                    }));
                }
            }
            await Task.WhenAll(getPreview);

            // saving first thumbnail
            if (previewImage.Any())
            {
                var path = envThumbnailPath.Replace("{fileName}", $"{assetAMS.Id}.jpg");
                var thumb = await SaveThumbnail(path, previewImage.FirstOrDefault());
            }

            
            assetAMS.Status = "encode finished";
            await _mediaService.UpdateAssetAMS(assetAMS.Id, assetAMS);

            return manifest;
        }
        #endregion
        #endregion

        #region Function
        [FunctionName("GetUploadURL")]
        public async Task<IActionResult> GetUploadURL(
            [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "MediaService/GetUploadURL")] HttpRequest req,
            ILogger log)
        {
            try
            {
                try
                {
                    var credential = await GetAmsCredential(log);
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var input = JsonConvert.DeserializeObject<MetadataInputDTO>(requestBody);

                    var result = new MetadataOutputDTO();
                    var asset = new AssetAMS();

                    asset.Id = Guid.NewGuid().ToString();
                    asset.Filename = input.FileName;
                    asset.Subject = input.Subject ?? input.FileName;
                    asset.Status = "draft";
                    asset.CreatedDateUtc = DateTime.UtcNow;
                    asset.CreatedDate = DateTime.UtcNow.AddHours(7);
                    asset.CreatedBy = input.Author;
                    asset.ContentAddress = asset.Id;

                    asset.UploadUrl = result.UploadUrl = (await _mediaService.InitVideoContainer(credential,
                        asset.ContentAddress, asset.Filename)).ToString();

                    await _mediaService.CreateAssetAMS(asset);

                    result.ResourceId = asset.Id;
                    result.ContentAddress = asset.ContentAddress;

                    return new OkObjectResult(result);
                }
                catch (Exception e)
                {
                    log.LogError(e.ToString());
                    return new BadRequestResult();
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [FunctionName("UploadAsset")]
        public async Task<IActionResult> UploadAsset(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var file = req.Form.Files["file"];
            string fileLocation = envPhysicalFilesDir + file.FileName;
            var reqBody = await req.ReadFormAsync();
            string url = reqBody["UploadURL"];
            Uri reqUploadURL = new Uri(url);
            bool fileExists = File.Exists(fileLocation);

            try
            {
                if (fileExists)
                {
                    await _mediaService.UploadFile(reqUploadURL, fileLocation);
                }

                return new OkObjectResult("");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
        #endregion

        #region Models
        public class DFJobInputDTO
        {
            [JsonProperty(propertyName: "assetId")]
            public string AssetId { get; set; }
        }
        #endregion

        #region Method
        public async Task<Credential> GetAmsCredential(ILogger log)
        {
            return await _mediaService.GetCredentialAsync(JsonConvert
                        .DeserializeObject<Credential>(envAmsCredential));
        }

        public async Task<CloudBlockBlob> SaveThumbnail(string path, byte[] file)
        {
            AzureStorageAccountService storageAccount = new AzureStorageAccountService(new Config
            {
                ContainerName = "thumbnails",
                AccountName = envCAccountName,
                AccountKey = envCAccountKey,
            }, path.Trim('/'));

            using (Stream stream = new MemoryStream(file))
            {
                return await storageAccount.Upload(stream);
            }
        }
        #endregion
    }
}