// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing
open Fake.Squirrel
open Fake.Git

// Properties
let buildDir = "./build/"
let testDir = "./tests/"
let packagingDir = "./packaging/"

let releaseRepo = "git@github.com:PatternRecog/PatternRecog.github.io.git"
let releaseRepoDir = "ReleaseRepo"

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
    CreateDir "Nuget"
    let buildVersion = getVersion()
    NuGet (fun p -> 
        {p with
            Version = buildVersion
            Publish = false }) 
            "PatternRecog.nuspec"
)

Target "CreateInstaller" (fun _ ->
    let pkg = sprintf "./NuGet/PatternRecog.%s.nupkg" (getVersion())
    SquirrelPack (fun p -> { p with WorkingDir = None
                                    ReleaseDir = releaseRepoDir + "\\Releases" }) pkg

)

Target "CloneReleaseRepo" (fun _ ->
    fullclean releaseRepoDir
    DeleteDirs [releaseRepoDir + "\\.git"; releaseRepoDir]
    clone "." releaseRepo releaseRepoDir
)

Target "CommitReleases" (fun _ ->
    let msg = sprintf "version %A" (getVersion())
    StageAll releaseRepoDir
    Commit releaseRepoDir msg
    runSimpleGitCommand  releaseRepoDir "push origin master" |> tracefn "push: %s"
)

// Dependencies
//"Clean"
//  ==>
"BuildApp"
  ==> "BuildTest"
  ==> "Test"
  ==> "Default"
  
"Test"
  ==> "CreatePackage"
  ==> "CloneReleaseRepo"
  ==> "CreateInstaller"
  ==> "CommitReleases"

// start build
RunTargetOrDefault "Default"