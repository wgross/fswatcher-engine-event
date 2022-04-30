Import-Module fs-dirs
Import-Module Microsoft.PowerShell.SecretManagement

Task secret-loaded {
   
    $script:secret = Get-Secret -Name powershellGallery -AsPlainText
    
} -precondition { $null -eq $script:secret }

Task clean-compile {

    "$PSScriptRoot\src\FSWatcherEngineEvent"  | fs-dirs\Invoke-AtContainer {
        dotnet clean 
        dotnet publish 
    }
}

Task publish-filesystem {
    
    # create a file system structure for publishing to the gallery
    $script:publishDirectory = New-Item  -Path "$PSScriptRoot\FSWatcherEngineEvent\1.3" -ItemType Directory -ErrorAction SilentlyContinue

    "$PSScriptRoot\src\FSWatcherEngineEvent\bin\debug\netstandard2.0\publish"  | fs-dirs\Invoke-AtContainer {
        Get-ChildItem -File | Copy-Item -Destination $script:publishDirectory -Exclude @("run.ps1","init.ps1") -Force
    }

} -depends clean-compile

Task publish-gallery {

    $script:publishDirectory | fs-dirs\Invoke-AtContainer {
    
        # now publish to the gallery
        Publish-Module -Name .\FSWatcherEngineEvent.psd1 -NuGetApiKey $script:secret -Verbose 
    }

} -depends secret-loaded,publish-filesystem

Task default -depends publish-gallery