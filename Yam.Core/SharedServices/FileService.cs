using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam.Core.SharedServices
{
    public class FileService(BlobServiceClient blobServiceClient) : IFileService
    {

        private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
        // The name of the container where you'll store the images
        private const string ContainerName = "images";



        public async Task<string> UploadAsync(IFormFile file)
        {
            // 1. Get or create the container
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // 2. Generate a unique blob name to avoid conflicts
            var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // 3. Upload the file stream to the blob
            await using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            // 4. Return the public URI of the uploaded blob
            return blobClient.Uri.ToString();
        }

    }
}
