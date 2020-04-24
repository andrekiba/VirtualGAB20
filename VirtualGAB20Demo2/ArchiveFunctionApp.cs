using System.Net;
using System.Text;
using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Storage;

namespace VirtualGAB20Demo2
{
    public class ArchiveFunctionApp : ComponentResource
    {
        [Output] public Output<string> AppId { get; set; } = null!;

        [Output] public Output<string> Endpoint { get; set; } = null!;

        public ArchiveFunctionApp(string name, ArchiveFunctionAppArgs args) 
            : base(nameof(ArchiveFunctionApp), name)
        {
            var projectName = Deployment.Instance.ProjectName.ToLower();
            var stackName = Deployment.Instance.StackName;
            var opts = new CustomResourceOptions { Parent = this };

            var storageAccountName = $"{projectName}{stackName}st";
            var storageAccount = new Account(storageAccountName, new AccountArgs
            {
                Name = storageAccountName,
                ResourceGroupName = args.ResourceGroupName,
                AccountReplicationType = "LRS",
                AccountTier = "Standard"
            }, opts);
            
            var planName = $"{projectName}-{stackName}-plan";
            var appServicePlan = args.Plan ?? new Plan(planName, new PlanArgs
            {
                Name = planName,
                ResourceGroupName = args.ResourceGroupName,
                Kind = "FunctionApp",
                Sku = new PlanSkuArgs
                {
                    Tier = "Dynamic",
                    Size = "Y1"
                }
            }, opts);
            
            var container = new Container("zips", new ContainerArgs
            {
                StorageAccountName = storageAccount.Name,
                ContainerAccessType = "private"
            }, opts);
            
            var blob = new Blob("funczip", new BlobArgs
            {
                StorageAccountName = storageAccount.Name,
                StorageContainerName = container.Name,
                Type = "Block",
                Source = args.Archive
            }, opts);
            
            var codeBlobUrl = SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount);
            
            args.AppSettings.Add("runtime", "dotnet");
            args.AppSettings.Add("WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl);
            
            var func = new FunctionApp(name, new FunctionAppArgs
            {
                Name = name,
                ResourceGroupName = args.ResourceGroupName,
                AppServicePlanId = appServicePlan.Id,
                AppSettings = args.AppSettings,
                StorageConnectionString = storageAccount.PrimaryConnectionString,
                Version = args.Version
            });
            
            AppId = func.Id;
            Endpoint = Output.Format($"https://{func.DefaultHostname}/api/");
        }
    }
    
    public class ArchiveFunctionAppArgs
    {
        public Input<string> ResourceGroupName { get; set; } = null!;
        public Plan Plan { get; set; }
        public Input<AssetOrArchive> Archive { get; set; } = null!;
    
        private InputMap<string>? appSettings;
        public InputMap<string> AppSettings
        {
            get => appSettings ??= new InputMap<string>();
            set => appSettings = value;
        }
        public Input<string> Version { get; set; } = "~3";
    }
}