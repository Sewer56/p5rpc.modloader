
# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

./p5rpc.modloader/Publish.ps1 -ProjectPath "p5rpc.modloader/p5rpc.modloader.csproj" `
              -PackageName "p5rpc.modloader" `
			  -ReadmePath ./README-LOADER.md `
              -MakeDelta false -UseGitHubDelta true `
              -MetadataFileName "p5rpc.modloader.ReleaseMetadata.json" `
			  -UseScriptDirectory false `
              -GitHubUserName zarroboogs -GitHubRepoName p5rpc.modloader -GitHubInheritVersionFromTag false `
			  @args

Pop-Location