## Release instructions

### Once

Get added to https://www.nuget.org/profiles/Promoted.ai. This isn't strictly necessary though.

### Every time

1. `dotnet test`
1. Update the \<Version\> in src/lib/Lib.csproj
1. `cd src/lib`
1. `dotnet pack --configuration Release`
1. Depending on if you changed the `schema` repo, you may need to do the next step for the `Promoted.Protos` package first
1. `dotnet nuget push [path to the .nupkg] --api-key [get this from 1Password] --source https://api.nuget.org/v3/index.json`
