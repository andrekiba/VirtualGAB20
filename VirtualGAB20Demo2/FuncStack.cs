using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

namespace VirtualGAB20Demo2
{
    internal class FuncStack : Stack
    {
        public FuncStack()
        {
            var projectName = Deployment.Instance.ProjectName;
            var stackName = Deployment.Instance.StackName;

            #region Resource Group
            
            var resourceGroupName = $"{projectName}-{stackName}-rg";
            var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
            {
                Name = resourceGroupName
            });
            
            #endregion 

            #region Azure Storage
            
            var storageAccountName = $"{projectName}{stackName}st";
            var storageAccount = new Account(storageAccountName, new AccountArgs
            {
                Name = storageAccountName,
                ResourceGroupName = resourceGroup.Name,
                AccountReplicationType = "LRS",
                AccountTier = "Standard"
            });
            
            #endregion
            
            #region Func Blob
            
            var container = new Container("zips", new ContainerArgs
            {
                StorageAccountName = storageAccount.Name,
                ContainerAccessType = "private"
            });
            
            var blob = new Blob("funczip", new BlobArgs
            {
                StorageAccountName = storageAccount.Name,
                StorageContainerName = container.Name,
                Type = "Block",
                Source = new FileArchive("../VirtualGAB20Func/bin/Debug/netcoreapp3.1/publish")
            });

            var codeBlobUrl = SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount);
            
            #endregion 
            
            #region AppService Plan
            
            var planName = $"{projectName}-{stackName}-plan";
            var appServicePlan = new Plan(planName, new PlanArgs
            {
                Name = planName,
                ResourceGroupName = resourceGroup.Name,
                Kind = "FunctionApp",
                Sku = new PlanSkuArgs
                {
                    Tier = "Dynamic",
                    Size = "Y1"
                }
            });

            #endregion 
            
            #region Function
            
            var funcName = $"{projectName}-{stackName}-func";
            var func = new FunctionApp(funcName, new FunctionAppArgs
            {
                Name = funcName,
                ResourceGroupName = resourceGroup.Name,
                AppServicePlanId = appServicePlan.Id,
                AppSettings =
                {
                    {"runtime", "dotnet"},
                    {"WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl}
                },
                StorageConnectionString = storageAccount.PrimaryConnectionString,
                Version = "~3"
            });
            
            #endregion 
            
            this.Endpoint = Output.Format($"https://{func.DefaultHostname}/api/Hello?name=GlobalAzure");
            
            // var func1Name = $"{projectName}-{stackName}-func";
            // var func1 = new ArchiveFunctionApp(func1Name, new ArchiveFunctionAppArgs
            // {
            //     ResourceGroupName = resourceGroup.Name,
            //     Archive = new FileArchive("../VirtualGAB20Func/bin/Debug/netcoreapp3.1/publish")
            // });
        }

        [Output]
        public Output<string> Endpoint { get; set; }
    }
}
