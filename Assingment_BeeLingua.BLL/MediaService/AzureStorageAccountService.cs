using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Assingment_BeeLingua.BLL.MediaService
{
    [Obsolete]
    public class AzureStorageAccountService
    {
        public static string envConnectionString = Environment.GetEnvironmentVariable("AzureStorageAccountCredential");

        #region Sub Classes
        public class Config
        {
            public string AccountName { get; set; }
            public string AccountKey { get; set; }
            public string ContainerName { get; set; }
        }
        #endregion

        private readonly Config n_config;
        private readonly string n_relativeFilePath;
        private CloudStorageAccount n_storageAccount;

        public CloudBlobClient Client { get; private set; }
        public CloudBlobContainer Container { get; private set; }
        public CloudBlockBlob Blob { get; private set; }
        public StorageUri GetUri
        {
            get{ return Blob.StorageUri; }
        }

        public AzureStorageAccountService(Config config, string relativeFilePath)
        {
            n_config = config;
            n_relativeFilePath = relativeFilePath;

            initialize().Wait();
        }

        private async Task initialize()
        {
            n_storageAccount = CloudStorageAccount.Parse(envConnectionString);
            Client = n_storageAccount.CreateCloudBlobClient();

            Container = Client.GetContainerReference(n_config.ContainerName);
            if (!await Container.ExistsAsync())
                throw new FileNotFoundException("container not found");

            Blob = Container.GetBlockBlobReference(n_relativeFilePath);
        }

        public async Task<AzureStorageAccountService> GetFile()
        {
            if (!await Blob.ExistsAsync())
                throw new FileNotFoundException("file not found");
            return this;
        }

        public async Task<string> GetSas(DateTime dateStart, DateTime dateEnd)
        {
            await GetFile();

            if (dateStart > dateEnd)
                throw new Exception("dateEnd must greater than dateStart");

            return Blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = dateStart,
                SharedAccessExpiryTime = dateEnd
            });
        }

        public async Task<string> GetSas(DateTime dateEnd)
        {
            return await GetSas(DateTime.UtcNow, dateEnd);
        }

        public async Task<CloudBlockBlob> Upload(Stream stream)
        {
            if (stream.Length == 0)
                throw new Exception("No file to upload");

            await Blob.UploadFromStreamAsync(stream);
            return Blob;
        }

        public async Task<CloudBlockBlob> Upload(Stream stream, string fileName)
        {
            if (stream.Length == 0)
                throw new Exception("No file to upload");

            Blob = Container.GetBlockBlobReference(n_relativeFilePath + fileName);

            await Blob.UploadFromStreamAsync(stream);
            return Blob;
        }
    }
}
