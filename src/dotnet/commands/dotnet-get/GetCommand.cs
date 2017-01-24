using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Restore;

namespace Microsoft.DotNet.Tools.Get
{
    public class GetCommand
    {
        private const string GlobalProjectFileText = @"<Project Sdk=""Microsoft.NET.Sdk"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
  </ItemGroup>
</Project>";

        private const string ScratchProjectFileFormat = @"<Project Sdk=""Microsoft.NET.Sdk"" ToolsVersion=""15.0"">
  <PropertyGroup>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""{0}"" Version=""{1}"" />
  </ItemGroup>
</Project>";

        public static int Run(string[] args)
        {
            Console.WriteLine(string.Join(" ", args.Select(x => $"\"{x}\"")));

            var profileDir = Environment.GetEnvironmentVariable(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                  ? "USERPROFILE"
                                  : "HOME");

            if (string.IsNullOrEmpty(profileDir))
            {
                return -1;
            }

            var dotnetDir = Path.Combine(profileDir, ".dotnet");
            var globalToolsProjectDir = Path.Combine(dotnetDir, "GlobalTools");
            Directory.CreateDirectory(dotnetDir);
            Directory.CreateDirectory(globalToolsProjectDir);
            var globalProject = Path.Combine(globalToolsProjectDir, "Global.csproj");

            var scratchProjectDir = Path.Combine(dotnetDir, "Scratch");
            Directory.CreateDirectory(scratchProjectDir);
            var scratchProject = Path.Combine(scratchProjectDir, "Scratch.csproj");
            var scratchProjectPackages = Path.Combine(scratchProjectDir, "Packages");

            var projectText = string.Format(ScratchProjectFileFormat, args[0], args[1]);
            File.WriteAllText(scratchProject, projectText);
            string[] restoreArgs;

            if(args.Length == 2)
            {
                restoreArgs = new[] { scratchProject, "--packages", scratchProjectPackages };
            }
            else
            {
                restoreArgs = new[] { scratchProject, "--packages", scratchProjectPackages, "-s", args[2] };
            }

            int execResult = RestoreCommand.Run(restoreArgs);

            if(execResult != 0)
            {
                return execResult;
            }

            var nuspecPath = Path.Combine(scratchProjectPackages, args[0].ToLowerInvariant(), args[1].ToLowerInvariant(), $"{args[0].ToLowerInvariant()}.nuspec");
            var nuspec = File.ReadAllText(nuspecPath);

            if (!File.Exists(globalProject))
            {
                File.WriteAllText(globalProject, GlobalProjectFileText);
            }

            if (nuspec.IndexOf(@"""DotnetCliTool""", StringComparison.Ordinal) > -1)
            {
                var rootElement = ProjectRootElement.Open(globalProject);
                var toolRef = rootElement.Items.FirstOrDefault(i => i.ItemType == "DotnetCliToolReference" && string.Equals(i.Include, args[0], StringComparison.OrdinalIgnoreCase));
                if(toolRef != null)
                {
                    var versionMetadata = toolRef.Metadata.FirstOrDefault(x => x.Name == "Version");

                    if(versionMetadata == null)
                    {
                        toolRef.AddMetadata("Version", args[1]);
                    }
                    else
                    {
                        versionMetadata.Value = args[1];
                    }
                }
                else
                {
                    var itemGroup = rootElement.ItemGroups.FirstOrDefault();

                    if (itemGroup == null)
                    {
                        itemGroup = rootElement.AddItemGroup();
                    }

                    var item = itemGroup.AddItem("DotnetCliToolReference", args[0]);
                    item.AddMetadata("Version", args[1]);
                }

                rootElement.Save();

                if (args.Length == 2)
                {
                    restoreArgs = new[] { globalProject };
                }
                else
                {
                    restoreArgs = new[] { globalProject, "-s", args[2] };
                }
            }
            else
            {
                if (args.Length == 2)
                {
                    restoreArgs = new[] { scratchProject };
                }
                else
                {
                    restoreArgs = new[] { scratchProject, "-s", args[2] };
                }
            }

            Directory.Delete(scratchProjectPackages, true);
            execResult = RestoreCommand.Run(restoreArgs);
            return execResult;
        }
    }
}
