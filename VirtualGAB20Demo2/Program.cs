using System.Threading.Tasks;
using Pulumi;

namespace VirtualGAB20Demo2
{
    internal static class Program
    {
        static Task<int> Main() => Deployment.RunAsync<FuncStack>();
    }
}
