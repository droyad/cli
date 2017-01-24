using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Utils
{
    public class GlobalToolCommandResolver : AbstractPathBasedCommandResolver
    {
        public GlobalToolCommandResolver(IEnvironmentProvider environment,
            IPlatformCommandSpecFactory commandSpecFactory) : base(environment, commandSpecFactory) { }

        internal override string ResolveCommandPath(CommandResolverArguments commandResolverArguments)
        {
#if !NET46
            string profileDir =
                Environment.GetEnvironmentVariable(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "USERPROFILE"
                    : "HOME");
#else
            string profileDir = Environment.GetEnvironmentVariable("USERPROFILE");
#endif

            if (string.IsNullOrEmpty(profileDir))
            {
                return null;
            }

            string globalToolsProjectDir = Path.Combine(profileDir, ".dotnet", "GlobalTools");

            if (Directory.Exists(globalToolsProjectDir))
            {
                return null;
            }

            return _environment.GetCommandPathFromRootPath(
                commandResolverArguments.ProjectDirectory,
                commandResolverArguments.CommandName,
                commandResolverArguments.InferredExtensions.OrEmptyIfNull());
        }

        internal override CommandResolutionStrategy GetCommandResolutionStrategy()
        {
            return CommandResolutionStrategy.ProjectLocal;
        }
    }
}
