using System.Threading.Tasks;
using Pulumi;

namespace Demo1
{
    internal static class Program
    {
        static Task<int> Main() => Deployment.RunAsync<MyStack>();
    }
}
