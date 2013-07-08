properties {
    $ProductName = "NCrash"
    $BaseDir = Resolve-Path "."
    $SolutionFile = "$BaseDir\$ProductName.sln"
    $OutputDir = "$BaseDir\Deploy\Package\"
    # Gets the number of commits since the last tag. 
    $Version = "1.0." + (git describe --tags --long).split('-')[1]
    $BuildConfiguration = "Release"
    
    $NuGetPackageName = "NCrash"
    $NuGetWinFormsPackageName = "NCrash.WinForms"
    $NuGetWPFPackageName = "NCrash.WPF"

    $NuGetPackDir = "$OutputDir" + "Pack"
    $NuGetPackWinFormsDir = "$OutputDir" + "PackWinForms"
    $NuGetPackWPFDir = "$OutputDir" + "PackWPF"

    $NuSpecFileName = "NCrash.nuspec"
    $NuSpecWinFormsFileName = "NCrash.WinForms.nuspec"
    $NuSpecWPFFileName = "NCrash.WPF.nuspec"

    $NuGetPackagePath = "$OutputDir" + $NuGetPackageName + "." + $Version + ".nupkg"
    $NuGetWinFormsPackagePath = "$OutputDir" + $NuGetWinFormsPackageName + "." + $Version + ".nupkg"
    $NuGetWPFPackagePath = "$OutputDir" + $NuGetWPFPackageName + "." + $Version + ".nupkg"
    
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
    # Pack main dll
    mkdir $NuGetPackDir
    cp "$NuSpecFileName" "$NuGetPackDir"

    mkdir "$NuGetPackDir\lib\net40"
    cp "$OutputDir\$ProductName.dll" "$NuGetPackDir\lib\net40"

    $Spec = [xml](get-content "$NuGetPackDir\$NuSpecFileName")
    $Spec.package.metadata.version = ([string]$Spec.package.metadata.version).Replace("{Version}",$Version)
    $Spec.Save("$NuGetPackDir\$NuSpecFileName")

    exec { .\.nuget\nuget pack "$NuGetPackDir\$NuSpecFileName" -OutputDirectory "$OutputDir" }

    # Pack WinForms dll
    mkdir $NuGetPackWinFormsDir
    cp "$NuSpecWinFormsFileName" "$NuGetPackWinFormsDir"

    mkdir "$NuGetPackWinFormsDir\lib\net40"
    cp "$OutputDir\$NuGetWinFormsPackageName.dll" "$NuGetPackWinFormsDir\lib\net40"

    $Spec = [xml](get-content "$NuGetPackWinFormsDir\$NuSpecWinFormsFileName")
    $DependObj = $Spec.package.metadata.dependencies.dependency | ? { $_.id -eq "NCrash" }
    $DependObj.SetAttribute("version", ($DependObj.version).Replace("{Version}",$Version))
    $Spec.package.metadata.version = ([string]$Spec.package.metadata.version).Replace("{Version}",$Version)
    $Spec.Save("$NuGetPackWinFormsDir\$NuSpecWinFormsFileName")

    exec { .\.nuget\nuget pack "$NuGetPackWinFormsDir\$NuSpecWinFormsFileName" -OutputDirectory "$OutputDir" }

    # Pack WPF dll
    mkdir $NuGetPackWPFDir
    cp "$NuSpecWPFFileName" "$NuGetPackWPFDir"

    mkdir "$NuGetPackWPFDir\lib\net40"
    cp "$OutputDir\$NuGetWPFPackageName.dll" "$NuGetPackWPFDir\lib\net40"

    $Spec = [xml](get-content "$NuGetPackWPFDir\$NuSpecWPFFileName")
    $DependObj = $Spec.package.metadata.dependencies.dependency | ? { $_.id -eq "NCrash" }
    $DependObj.SetAttribute("version", ($DependObj.version).Replace("{Version}",$Version))
    $Spec.package.metadata.version = ([string]$Spec.package.metadata.version).Replace("{Version}",$Version)
    $Spec.Save("$NuGetPackWPFDir\$NuSpecWPFFileName")

    exec { .\.nuget\nuget pack "$NuGetPackWPFDir\$NuSpecWPFFileName" -OutputDirectory "$OutputDir" }
}

task Publish -depends Pack {
    exec { .\.nuget\nuget push $NuGetPackagePath }
    exec { .\.nuget\nuget push $NuGetWinFormsPackagePath }
    exec { .\.nuget\nuget push $NuGetWPFPackagePath }
}
