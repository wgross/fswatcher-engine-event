Import-Module fs-dirs
Import-Module Microsoft.PowerShell.SecretManagement

properties {
    $rootDir = $PSScriptRoot
    $publishDirectory = "$PSScriptRoot\FSWatcherEngineEvent\1.4"
}

Task secret-loaded {
   
    $script:secret = Get-Secret -Name powershellGallery -AsPlainText
    $script:secret|Write-Debug
    
} -precondition { $null -eq $script:secret }

Task clean-compile {

    Write-Host "Cleaning project: $publishDirectory"

    "$rootDir\src\FSWatcherEngineEvent"  | fs-dirs\Invoke-AtContainer {
        dotnet clean 
        dotnet publish 
    }
}

Task publish-filesystem {
    
    Write-Host "Publish directory: $publishDirectory"
    Test-Path $publishDirectory
    
    New-Item  -Path "$PSScriptRoot\FSWatcherEngineEvent\1.4" -ItemType Directory -ErrorAction SilentlyContinue

    "$rootDir\src\FSWatcherEngineEvent\bin\debug\netstandard2.0\publish"  | fs-dirs\Invoke-AtContainer {
        Get-ChildItem -File | Copy-Item -Destination $publishDirectory -Exclude @("run.ps1","init.ps1") -Force
    }

} -depends clean-compile

Task publish-gallery {

    $publishDirectory | fs-dirs\Invoke-AtContainer {
    
        # now publish to the gallery
        Publish-Module -Name .\FSWatcherEngineEvent.psd1 -NuGetApiKey $script:secret -Verbose 
    }

} -depends secret-loaded,publish-filesystem

Task default -depends publish-gallery