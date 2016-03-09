// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing

// Properties
let buildDir = "./build/"
let testDir = "./tests/"

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
)

// Dependencies
"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"
  ==> "Default"

// start build
RunTargetOrDefault "Default"