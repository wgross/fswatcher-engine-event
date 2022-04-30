Import-Module fs-dirs
Import-Module Microsoft.PowerShell.SecretManagement

Task secret-loaded {
   
    $script:secret = Get-Secret -Name powershellGallery -AsPlainText
    
} -precondition ($null -ne $script:secret)

Task clean-compile {

    "$PSScriptRoot\src\FSWatcherEngineEvent"  | fs-dirs\Invoke-AtContainer {
        dotnet clean 
        dotnet publish 
    }
}

Task publish-filesystem {
    # create a file system structire for publishing to the gallery
    New-Item  -Path "FSWatcherEngineEvent\1.2" -ItemType Directory -ErrorAction SilentlyContinue
    Get-ChildItem -File | Copy-Item -Destination "FSWatcherEngineEvent\1.2"
} -depends clean-compile

Task publish-gallery {

    "$PSScriptRoot\src\FSWatcherEngineEvent\bin\Debug\netstandard2.0\publish" | fs-dirs\Invoke-AtContainer {
    
        # now publish to the gallery
        Publish-Module -Path "FSWatcherEngineEvent\1.2" -NuGetApiKey $script:secret -Verbose 
    }

} -depends secret-loaded,publish-filesystem

Task default -depends publish-gallery