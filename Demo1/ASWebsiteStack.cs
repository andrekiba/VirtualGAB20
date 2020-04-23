using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Shared.Protocol;
using Pulumi;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;
using Pulumi.Azure.Storage.Inputs;

namespace Demo1
{
    internal class ASWebsiteStack : Stack
    {
        public ASWebsiteStack()
        {
            var projectName = Deployment.Instance.ProjectName.ToLower();
            var stackName = Deployment.Instance.StackName;
            
            // Create an Azure Resource Group
            var resourceGroupName = $"virtualgab20-{projectName}-{stackName}-rg";
            var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
            {
                Name = resourceGroupName
            });

            // Create an Azure Storage Account
            var storageAccountName = $"virtualgab20{projectName}{stackName}st";
            var storageAccount = new Account(storageAccountName, new AccountArgs
            {
                Name = storageAccountName,
                ResourceGroupName = resourceGroup.Name,
                AccountReplicationType = "LRS",
                AccountTier = "Standard",
                AccountKind = "StorageV2",
                EnableHttpsTrafficOnly = true,
                StaticWebsite = new AccountStaticWebsiteArgs
                {
                    IndexDocument = "index.html",
                    //Error404Document = "404.html"
                }
            });

            var files = Directory.GetFiles("./wwwroot");
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var blob = new Blob(name, new BlobArgs
                {
                    Name = name,
                    StorageAccountName = storageAccountName,
                    StorageContainerName = "$web",
                    Type = "Block",
                    ContentType = "text/html",
                    Source = new FileAsset(file)
                });
            }
            
            //storageAccount.PrimaryBlobConnectionString.Apply(async cs => await EnableStaticSite(cs));

            // Export the web endpoint for the storage account
            StorageWebsite = storageAccount.PrimaryWebEndpoint;
        }
        
        static async Task EnableStaticSite(string connectionString)
        {
            var sa = CloudStorageAccount.Parse(connectionString);

            var blobClient = sa.CreateCloudBlobClient();
            var blobServiceProperties = new ServiceProperties
            {
                StaticWebsite = new StaticWebsiteProperties
                {
                    Enabled = true,
                    IndexDocument = "index.html",
                    ErrorDocument404Path = "404.html"
                }
            };
            await blobClient.SetServicePropertiesAsync(blobServiceProperties);
        }

        [Output]
        public Output<string> StorageWebsite { get; set; }
    }
}
