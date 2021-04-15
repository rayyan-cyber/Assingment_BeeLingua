using Assingment_BeeLingua.DAL.Models.MediaService;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage.Blob;
using Nexus.Base.CosmosDBRepository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assingment_BeeLingua.DAL.Repository.Repositories;

namespace Assingment_BeeLingua.BLL.MediaService
{
    public class MediaServiceService
    {
        private readonly IDocumentDBRepository<AssetAMS> _repository;
        private ConfigAsset _configAsset = new ConfigAsset();
        public MediaServiceService(IDocumentDBRepository<AssetAMS> repository)
        {
            if (this._repository == null)
            {
                this._repository = repository;
            }
        }

        #region Repository Service
        public async Task<AssetAMS> CreateAssetAMS(AssetAMS dataToBeInserted)
        {
            return await _repository.CreateAsync(dataToBeInserted);
        }

        public async Task<AssetAMS> UpdateAssetAMS(string id, AssetAMS dataToBeUpdated)
        {
            return await _repository.UpdateAsync(id, dataToBeUpdated);
        }

        public async Task<AssetAMS> GetAssetAMS(string id)
        {
           var data = (await _repository.GetAsync(e =>
                e.Id == id)).Items.FirstOrDefault();
            return data;
        }
        #endregion

        public async Task<Credential> GetCredentialAsync(Credential credential)
        {
            var clientCredential = new ClientCredential(
                    credential.AadClientId,
                    credential.AadSecret);

            var cred = await ApplicationTokenProvider.LoginSilentAsync(
                    credential.AadTenantId,
                    clientCredential,
                    ActiveDirectoryServiceSettings.Azure);

            // create media service client
            credential.Client = new AzureMediaServicesClient(credential.ArmEndpoint, cred)
            {
                SubscriptionId = credential.SubscriptionId,
            };

            credential.Client.LongRunningOperationRetryTimeout = 2;

            return credential;
        }

        public static StandardEncoderPreset encoderPreset = new StandardEncoderPreset(
        codecs: new Codec[]
        {
            new AacAudio(
                channels: 2,
                samplingRate: 48000,
                bitrate: 128000,
                profile: AacAudioProfile.AacLc
            ),

            new H264Video(
                layers:  new H264Layer[]
                {
                    new H264Layer // Resolution: 1280x720
                    {
                        Bitrate=1800000,
                        Width="1280",
                        Height="720",
                        Label="HD",
                    },
                    new H264Layer // YouTube 144p: 256×144
                    {
                        Bitrate=64000,
                        Width="256",
                        Height="144",
                        Label="SD",
                    }
                }),

            new JpgImage(
                start: "{Best}",
//                    step: "25%",
//                    range: "60%",
                layers: new JpgLayer[] {
                    new JpgLayer(
                        width: "100%",
                        height: "100%"
                    ),
                })
        },
        formats: new Format[]
        {
            new Mp4Format(
                filenamePattern:"Video-{Basename}-{Label}-{Bitrate}{Extension}"
            ),
            new JpgFormat(
                filenamePattern:"Thumbnail-{Basename}-{Index}{Extension}"
            )
        });

        public async Task<Transform> GetOrCreateTransformAsync(
           IAzureMediaServicesClient client,
           string resourceGroupName,
           string accountName,
           string transformName)
        {
            Transform transform = await client.Transforms.GetAsync(resourceGroupName, accountName, transformName);

            if (transform == null)
            {
                transform = await client.Transforms.CreateOrUpdateAsync(resourceGroupName, accountName, transformName, new List<TransformOutput>{
                    new TransformOutput(encoderPreset)
                });
            }

            return transform;
        }

        public async Task<Uri> InitVideoContainer(Credential credential, string contentAddress, string fileName)
        {
            #region 1. initialize
            var inputAsset = new Asset(
                name: string.Format(_configAsset.InputName, contentAddress), 
                container: string.Format(_configAsset.InputName, contentAddress), 
                description: string.Format(_configAsset.InputDescription, fileName));
            #endregion

            #region 2. set input
            inputAsset = await credential.Client.Assets.CreateOrUpdateAsync(
                credential.ResourceGroup,
                credential.AccountName,
                inputAsset.Container,
                inputAsset);

            var response = await credential.Client.Assets.ListContainerSasAsync(
                credential.ResourceGroup,
                credential.AccountName,
                inputAsset.Name,
                permissions: AssetContainerPermission.ReadWrite,
                expiryTime: DateTime.Now.AddHours(6).ToUniversalTime());
            #endregion

            return new Uri(response.AssetContainerSasUrls.First());
        }

        public async Task<CloudBlockBlob> UploadFile(
           Uri UploadUrl,
           string filePath)
        {
            string fileName = Path.GetFileName(filePath);
         
            CloudBlobContainer container = new CloudBlobContainer(UploadUrl);
            var inputBlob = container.GetBlockBlobReference(fileName);

            await inputBlob.UploadFromFileAsync(filePath);
            return inputBlob;
        }


        public async Task<Asset> CreateOutputAssetAsync(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string outputName, string fileName)
        {
            var outputAsset = new Asset(name: outputName, container: outputName, description: $"encode \"{fileName}\"");
            await client.Assets.DeleteAsync(resourceGroupName,accountName,
                outputName);
            return await client.Assets.CreateOrUpdateAsync(resourceGroupName,
                accountName, outputName, outputAsset);
        }

        public async Task<Job> SubmitJobAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName,
            string inputAssetName,
            string resultName)
        {
            // Use the name of the created input asset to create the job input.
            JobInput jobInput = new JobInputAsset(assetName: inputAssetName);

            JobOutput[] jobOutputs =
            {
                new JobOutputAsset(resultName),
            };

            Job job = await client.Jobs.GetAsync(resourceGroupName, accountName,
                    transformName, jobName);

            if (job != null)
            {
                await client.Jobs.DeleteAsync(resourceGroupName, accountName,
                    transformName, jobName);

                // buat sinkronisasi
                await Task.Delay(3000);
            }
            job = await client.Jobs.CreateAsync(
                resourceGroupName,
                accountName,
                transformName,
                jobName,
                new Job
                {
                    Input = jobInput,
                    Outputs = jobOutputs,
                });
            return job;
        }

        public async Task<Job> WaitForJobToFinishAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName)
        {
            const int SleepIntervalMs = 20 * 1000;

            Job job;
            do
            {
                job = await client.Jobs.GetAsync(resourceGroupName, accountName, transformName, jobName);

                Console.WriteLine($"Job is '{job.State}'.");
                for (int i = 0; i < job.Outputs.Count; i++)
                {
                    JobOutput output = job.Outputs[i];
                    Console.Write($"\tJobOutput[{i}] is '{output.State}'.");
                    if (output.State == JobState.Processing)
                    {
                        Console.Write($"  Progress (%): '{output.Progress}'.");
                    }

                    Console.WriteLine();
                }

                if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
                {
                    await Task.Delay(SleepIntervalMs);
                }
            }
            while (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled);

            return job;
        }

        public async Task<StreamingLocator> CreateStreamingLocatorAsync(
            IAzureMediaServicesClient client,
            string resourceGroup,
            string accountName,
            string assetName,
            string locatorName)
        {
            StreamingLocator locator = await client.StreamingLocators.CreateAsync(
                resourceGroup,
                accountName,
                locatorName,
                new StreamingLocator
                {
                    AssetName = assetName,
                    StreamingPolicyName = PredefinedStreamingPolicy.ClearStreamingOnly
                });

            return locator;
        }

        public async Task<IList<string>> GetStreamingUrlsAsync(
           IAzureMediaServicesClient client,
           string resourceGroupName,
           string accountName,
           String locatorName)
        {
            const string DefaultStreamingEndpointName = "default";

            IList<string> streamingUrls = new List<string>();

            StreamingEndpoint streamingEndpoint = await client.StreamingEndpoints.GetAsync(resourceGroupName, accountName, DefaultStreamingEndpointName);

            if (streamingEndpoint != null)
            {
                if (streamingEndpoint.ResourceState != StreamingEndpointResourceState.Running)
                {
                    await client.StreamingEndpoints.StartAsync(resourceGroupName, accountName, DefaultStreamingEndpointName);
                }
            }

            ListPathsResponse paths = await client.StreamingLocators.ListPathsAsync(resourceGroupName, accountName, locatorName);

            foreach (StreamingPath path in paths.StreamingPaths)
            {
                UriBuilder uriBuilder = new UriBuilder
                {
                    Scheme = "https",
                    Host = streamingEndpoint.HostName,

                    Path = path.Paths[0]
                };
                streamingUrls.Add(uriBuilder.ToString());
            }

            return streamingUrls;
        }
    }
}
