properties {
    $ProductName = "NCrash"
    $BaseDir = Resolve-Path "."
    $SolutionFile = "$BaseDir\$ProductName.sln"
    $OutputDir = "$BaseDir\Deploy\Package\"
    # Gets the number of commits since the last tag. 
    $Version = "1.0." + (git describe --tags --long).split('-')[1]
    $BuildConfiguration = "Release"
    
    $NuGetPackageName = "NCrash"
    $NuGetPackDir = "$OutputDir" + "Pack"
    $NuSpecFileName = "NCrash.nuspec"
    $NuGetPackagePath = "$OutputDir" + $NuGetPackageName + "." + $Version + ".nupkg"
    
    $ArchiveDir = "$OutputDir" + "Archive"
}

Framework '4.0'

task default -depends Pack

task Init {
}

task Clean -depends Init {
    if (Test-Path $OutputDir) {
        ri $OutputDir -Recurse
    }
    
    ri "$NuGetPackageName.*.nupkg"
    ri "$NuGetPackageName.zip" -ea SilentlyContinue
}

task Build -depends Init,Clean {
    exec { msbuild $SolutionFile "/p:OutDir=$OutputDir" "/p:Configuration=$BuildConfiguration" }
}

task Pack -depends Build {
    mkdir $NuGetPackDir
    cp "$NuSpecFileName" "$NuGetPackDir"

    mkdir "$NuGetPackDir\lib"
    cp "$OutputDir\$ProductName.dll" "$NuGetPackDir\tools"

    $Spec = [xml](get-content "$NuGetPackDir\$NuSpecFileName")
    $Spec.package.metadata.version = ([string]$Spec.package.metadata.version).Replace("{Version}",$Version)
    $Spec.Save("$NuGetPackDir\$NuSpecFileName")

    exec { .\.nuget\nuget pack "$NuGetPackDir\$NuSpecFileName" -OutputDirectory "$OutputDir" }
}

task Publish -depends Pack {
    exec { .\.nuget\nuget push $NuGetPackagePath }
}
