using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Assingment_BeeLingua.BLL;
using Assingment_BeeLingua.BLL.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Assingment_BeeLingua.API.Functions
{
    public class MediaServiceFunction
    {
       
        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .Build();
        public ConfigWrapper config = new ConfigWrapper(Configuration);

        private readonly MediaServiceService _mediaService = new MediaServiceService();
        private const string AdaptiveStreamingTransformName = "Transform-AdaptiveStreaming-BL-Yaya";
        private const string InputMP4FileName = @"Nature.mp4";
        private const string OutputFolderName = @"Output";


        [FunctionName("UploadEncodeAsset")]
        public async Task<IActionResult> UploadEncodeAsset(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var uniqueness = Guid.NewGuid().ToString("N");
                string inputAssetName = $"Input-{InputMP4FileName}-{uniqueness}-BL-Yaya";
                string outputAssetName = $"Output-{InputMP4FileName}-{uniqueness}-BL-Yaya";
                string jobName = $"Job-{InputMP4FileName}-{uniqueness}-BL-Yaya";
                string locatorName = $"Locator-{InputMP4FileName}-{uniqueness}-BL-Yaya";

                IAzureMediaServicesClient client = await _mediaService.CreateMediaServicesClientAsync(config);
                _ = await _mediaService.GetOrCreateTransformAsync(client, config.ResourceGroup, config.AccountName, AdaptiveStreamingTransformName);
                _ = await _mediaService.CreateInputAssetAsync(client, config.ResourceGroup, config.AccountName, inputAssetName, InputMP4FileName);

                _ = new JobInputAsset(assetName: inputAssetName);

                Asset outputAsset = await _mediaService.CreateOutputAssetAsync(client, config.ResourceGroup, config.AccountName, outputAssetName);
                _ = await _mediaService.SubmitJobAsync(client, config.ResourceGroup, config.AccountName, AdaptiveStreamingTransformName, jobName, inputAssetName, outputAsset.Name);

                Job job = await _mediaService.WaitForJobToFinishAsync(client, config.ResourceGroup, config.AccountName, AdaptiveStreamingTransformName, jobName);
                var listUrl = new List<string>();
                if (job.State == JobState.Finished)
                {
                    Console.WriteLine("Job finished.");
                    if (!Directory.Exists(OutputFolderName))
                        Directory.CreateDirectory(OutputFolderName);

                    await _mediaService.DownloadOutputAssetAsync(client, config.ResourceGroup, config.AccountName, outputAsset.Name, OutputFolderName);

                    StreamingLocator locator = await _mediaService.CreateStreamingLocatorAsync(client, config.ResourceGroup, config.AccountName, outputAsset.Name, locatorName);

                    IList<string> urls = await _mediaService.GetStreamingUrlsAsync(client, config.ResourceGroup, config.AccountName, locator.Name);
                    foreach (var url in urls)
                    {
                        listUrl.Add(url);
                    }
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

