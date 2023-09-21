## Release instructions

### Once

Get added to https://www.nuget.org/profiles/Promoted.ai. Make an API key, but give ownership to the organization instead of yourself.

### Every time

Depending on if you changed the `schema` repo, you may need to do the following steps for the `Promoted.Protos` package first.

1. `dotnet test`
1. Update the \<Version\> in src/lib/Lib.csproj
1. `cd src/lib`
1. `dotnet pack --configuration Release`
1. `dotnet nuget push [path to the .nupkg] --api-key [whatever you generated before] --source https://api.nuget.org/v3/index.json`
1. Make a PR for the .csproj change
1. If the `README` was updated, copy the new version to NuGet
