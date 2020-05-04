using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace generate_blob_link
{
    public static class generate_blob_link
    {

        [FunctionName("generate_blob_link")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("Trigger function processed a request.");

            var accountName = req.Query["accountName"];
            var containerName = req.Query["containerName"];
            var blobName = req.Query["blobName"];

            log.LogInformation(string.Format("AccountName: {0}", accountName));
            log.LogInformation(string.Format("ContainerName: {0}", containerName));
            log.LogInformation(string.Format("BlobName: {0}", blobName));

            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(1)
            };

            sasBuilder.SetPermissions(permissions: BlobSasPermissions.Read);
            log.LogInformation("SAS Builder finished");

            var blobClient = new BlobServiceClient(new Uri(string.Format("https://{0}.blob.core.windows.net", accountName)), new DefaultAzureCredential());
            log.LogInformation("Blob Client Created");
            var userDelegationKey = blobClient.GetUserDelegationKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1));
            log.LogInformation("User delegation key properties:");
            log.LogInformation("Key signed start: {0}", userDelegationKey.Value.SignedStartsOn);
            log.LogInformation("Key signed expiry: {0}", userDelegationKey.Value.SignedExpiresOn);
            log.LogInformation("Key signed object ID: {0}", userDelegationKey.Value.SignedObjectId);
            log.LogInformation("Key signed tenant ID: {0}", userDelegationKey.Value.SignedTenantId);
            log.LogInformation("Key signed service: {0}", userDelegationKey.Value.SignedService);
            log.LogInformation("Key signed version: {0}", userDelegationKey.Value.SignedVersion);

            UriBuilder fullUri = new UriBuilder()
            {
                Scheme = "https",
                Host = string.Format("{0}.blob.core.windows.net", accountName),
                Path = string.Format("{0}/{1}", containerName, blobName),
                Query = sasBuilder.ToSasQueryParameters(userDelegationKey, accountName).ToString()
            };
            log.LogInformation("URI generated");

            //TODO: Add error responses
            var response = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Format("<head><meta http-equiv=\"Refresh\" content=\"0; URL={0}\"></head>", fullUri.Uri))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            log.LogInformation("Generated HTML redirect code");

            return response;
        }
    }
}
