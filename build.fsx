// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing
open Fake.Squirrel

// Properties
let buildDir = "./build/"
let testDir = "./tests/"
let packagingDir = "./packaging/"

let getVersion() = (FileVersion "./build/PatternRecog.GUI.exe")
// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir]
)

Target "BuildApp" (fun _ ->
    !! "PatternRecog.GUI/PatternRecog.GUI.fsproj"
      |> MSBuildRelease buildDir "Build"
      |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    !! "**/*.Tests.csproj"
      |> MSBuildRelease testDir "Build"
      |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->
    !! (testDir + "/*.Tests.dll")
      |> xUnit2 (fun p -> {p with HtmlOutputPath = Some(testDir @@ "PatternRecog.Tests.html")})
)

Target "Default" (fun _ ->
    trace "Hello World from FAKE"
    trace (sprintf "%A" (getVersion()))
)

Target "CreatePackage" (fun _ ->
    // Copy all the package files into a package folder
    // CopyFiles packagingDir (!! "./build/*")

    let buildVersion = getVersion()
    NuGet (fun p -> 
        {p with
            Version = buildVersion
            Publish = false }) 
            "PatternRecog.nuspec"
)

Target "CreateInstaller" (fun _ ->
    SquirrelPack (fun p -> { p with WorkingDir = None }) (sprintf "./NuGet/PatternRecog.%s.nupkg" (getVersion()))

)

// Dependencies
"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"
  ==> "Default"
  
"Test"
  ==> "CreatePackage"
  ==> "CreateInstaller"

// start build
RunTargetOrDefault "Default"