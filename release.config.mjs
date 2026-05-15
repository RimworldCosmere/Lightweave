import { readFileSync } from 'node:fs';

const publishedFileIds = JSON.parse(
    readFileSync(new URL('./PublishedFileIds.json', import.meta.url), 'utf8'),
);
const descriptionHeader = readFileSync(new URL('./.github/README.header.md', import.meta.url), 'utf8');
const descriptionFooter = readFileSync(new URL('./.github/README.footer.md', import.meta.url), 'utf8');

const lightweaveWorkshopIds = publishedFileIds.Lightweave ?? {};
const hasWorkshopIds = Object.keys(lightweaveWorkshopIds).length > 0;

const plugins = [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    [
        "@semantic-release/github",
        {
            "assets": [
                { path: './zips/**/*.zip' },
                { path: './nupkgs/*.nupkg' },
                { path: './nupkgs/*.snupkg' },
            ]
        }
    ],
    [
        "semantic-release-replace-plugin",
        {
            "replacements": [
                {
                    "files": ["Lightweave/Runtime/BuildInfo.cs"],
                    "from": "Revision = \".*\";",
                    "to": "Revision = \"${nextRelease.version}\";",
                    "countMatches": true
                },
                {
                    "files": ["Lightweave/Runtime/BuildInfo.cs"],
                    "from": "BuildTime = \".*\";",
                    "to": "BuildTime = \"${(new Date()).toISOString()}\";",
                    "results": [
                        {
                            "file": "Lightweave/Runtime/BuildInfo.cs",
                            "hasChanged": true,
                            "numMatches": 1,
                            "numReplacements": 1
                        }
                    ],
                    "countMatches": true
                }
            ]
        }
    ],
    [
        "@semantic-release/exec",
        {
            "prepareCmd": "dotnet pack Lightweave.sln -c Release -p:Version=${nextRelease.version} -p:PackageVersion=${nextRelease.version} -p:FileVersion=${nextRelease.version.replace(/-.*/, '')}.0 -p:AssemblyVersion=${nextRelease.version.replace(/-.*/, '')}.0 -p:InformationalVersion=${nextRelease.version} -o ./nupkgs --include-symbols -p:SymbolPackageFormat=snupkg",
            "publishCmd": "dotnet nuget push './nupkgs/*.nupkg' --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate"
        }
    ],
    [
        "@semantic-release/git",
        {
            "assets": ["Lightweave/Runtime/BuildInfo.cs"]
        }
    ],
];

if (hasWorkshopIds) {
    plugins.push([
        "semantic-release-steam",
        {
            "appId": "294100",
            "branchTargets": {
                "main": "stable",
                "beta": "beta"
            },
            "descriptionHeader": descriptionHeader,
            "descriptionFooter": descriptionFooter,
            "assetBaseUrlTemplate": "https://raw.githubusercontent.com/RimworldCosmere/Lightweave/{branch}",
            "mods": [
                {
                    "name": "Lightweave",
                    "path": ".",
                    "workshopIds": lightweaveWorkshopIds,
                }
            ]
        }
    ]);
}

/**
 * @type {import('semantic-release').GlobalConfig}
 */
export default {
    branches: ["main", { name: 'beta', prerelease: true }, { name: 'alpha', prerelease: true }],
    plugins,
    tagFormat: "${version}",
};
