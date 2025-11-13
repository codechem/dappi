using System.Runtime.CompilerServices;

namespace Dappi.HeadlessCms.Tests
{
    public class VerifyGlobalSettings
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            VerifyHttp.Initialize();
            Recording.Start();
        }
    }
}