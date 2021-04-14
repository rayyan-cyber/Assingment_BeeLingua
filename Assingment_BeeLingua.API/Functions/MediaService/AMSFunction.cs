using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Assingment_BeeLingua.API.Functions.MediaService.DTO;
using Assingment_BeeLingua.BLL;
using Assingment_BeeLingua.BLL.MediaService;
using Assingment_BeeLingua.DAL.Models.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Assingment_BeeLingua.DAL.Repository.Repositories;

namespace Assingment_BeeLingua.API.Functions.MediaService
{
    public class AMSFunction
    {
        private readonly MediaServiceService _mediaService;

        public static readonly string envAmsCredential = Environment.GetEnvironmentVariable("AMSCredential");
        public static readonly string envPhysicalFilesDir = @"Z:\Ecomindo\BeeLingua\MediaService\";

        public AMSFunction(CosmosClient client)
        {
            _mediaService ??= new MediaServiceService(new MediaServiceRepository(client));
        }
        public async Task<Credential> GetAmsCredential(ILogger log)
        {
            log.LogInformation("--- ams > init Media Services");
            return await _mediaService.GetCredentialAsync(JsonConvert
                        .DeserializeObject<Credential>(envAmsCredential));
        }

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

                    asset.Id = asset.ContentId = Guid.NewGuid().ToString();
                    asset.Duration = "0";
                    asset.Status = "draft";
                    asset.Filename = input.FileName;
                    asset.Subject = input.Subject ?? input.FileName;
                    asset.CreatedDateUtc = DateTime.UtcNow;
                    asset.CreatedDate = DateTime.UtcNow.AddHours(7);
                    asset.CreatedBy = input.Author;
                    asset.IsAvailable = "Y";
                    asset.Category = "Additional";

                    asset.Type = AssetAMSType.Video;
                    asset.ContentAddress = asset.Id;

                    result.UploadUrl = (await _mediaService.InitVideoContainer(credential,
                        asset.ContentAddress, asset.Filename)).ToString();

                    // save metadata to database
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

    }
}

