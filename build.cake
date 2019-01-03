#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.3.1"
 #addin nuget:?package=Cake.Sonar&version=1.1.18
#tool "nuget:?package=OctopusTools"
#addin nuget:?package=Cake.Coverlet


var target = Argument("target", "Pack");
var configuration = Argument("configuration", "Release");
var coverletDirectory = Directory("./coverage-results");
var publishorderApi = Directory("./publishOrderApi/");
var solutionFile = "Core_Sonar.sln";  
var websolutionFile = "./Core_Sonar/Core_Sonar.csproj";
var coverageResultsFileName = "coveragetest.xml";

Task("Pack") 
  .Does(() => {
    var nuGetPackSettings   = new NuGetPackSettings {
                                    Id                      = "Core_Sonar",
                                    Version                 = "0.0.0.1",
                                    Title                   = "Core Demo",
                                    Authors                 = new[] {"Nilam Rajvanshi"},
                                    Description             = "Demo of creating cake.build scripts.",
                                    Summary                 = "Excellent summary of what the Cake (C# Make) build tool does.",
                                    ProjectUrl              = new Uri("https://github.com/nilam2612/Core_Sonar"),
                                    Files                   = new [] {
                                                                        new NuSpecContent {Source = "c.exe", Target = "bin"},
                                                                      },
                                    BasePath                = "./Core_Sonar/Core_Sonar/bin/Debug",
                                    OutputDirectory         = "./nuget"
                                };

    NuGetPack(nuGetPackSettings);
  });

Task("OctoPush")
  .IsDependentOn("Pack")
  .Does(() => {
    OctoPush(" https://coresonar.octopus.app", "", new FilePath("./nuget/Core_Sonar.0.0.0.1.nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
  });

Task("OctoRelease")
  .IsDependentOn("OctoPush")
  .Does(() => {
    OctoCreateRelease("Core_Sonar", new CreateReleaseSettings {
        Server = "https://coresonar.octopus.app",
        ApiKey = "",
        ReleaseNumber = "0.0.0.1"
      });
  });

// Build using the build configuration specified as an argument.
 Task("Build")
    .Does(() =>
    {
			Information("Build Project");
            DotNetCoreBuild(solutionFile.ToString(),new DotNetCoreBuildSettings()
                            {
                                Configuration = configuration,
                                ArgumentCustomization = args => args.Append("--no-restore"),
                            });
    });

	Task("PublishOrderApi")    
 .Does(() =>
    {
        DotNetCorePublish(websolutionFile.ToString(),new DotNetCorePublishSettings()
                        {
                                Configuration = configuration,
                                OutputDirectory = publishorderApi,
                      ArgumentCustomization = args => args.Append("--no-restore"),
                        });
    });
	
	 Task("SonarBegin")
   .Does(() => {
      SonarBegin(new SonarBeginSettings{
       
         Key = "core_sonar_2",
        Name = "Sonar Demo App",
        Url = "http://localhost:9000/",
         Login = "admin",
        Password = "admin",
        Verbose = true,
        Version = "1.0.0.0"
        });
    });
  

  
 Task("SonarEnd")
   .Does(() => {
     SonarEnd(new SonarEndSettings{
        Login = "admin",
        Password = "admin"
     });
  });
  

Task("Test")
    .Does(() => {
	    var testProject = "./UnitTestProject1/UnitTestProject1.csproj";
        var settings = new DotNetCoreTestSettings
        {
            ArgumentCustomization = args => args.Append("/p:CollectCoverage=true")
                                                .Append("/p:CoverletOutputFormat=opencover")		    				
                                                .Append("/p:CoverletOutput=./" + coverageResultsFileName)
        };
        DotNetCoreTest(testProject, settings);
       
    });
   // Look under a 'Tests' folder and run dotnet test against all of those projects.
// Then drop the XML test results file in the Artifacts folder at the root.
Task("TestOLD")
    .Does(() =>
    {
        var project = "./UnitTestProject1/UnitTestProject1.csproj";
    
          
            DotNetCoreTest(
                project.ToString(),
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                
                    ArgumentCustomization = args => args.Append("--no-restore")
                },
                new CoverletSettings() 
                {
			  CollectCoverage = true,
			CoverletOutputFormat = CoverletOutputFormat.opencover	,
			CoverletOutputDirectory = publishorderApi,
        CoverletOutputName = $"results-{DateTime.UtcNow:dd-MM-yyyy-HH-mm}"
       
                }
                );
     
    });
  




	Task("SonarCov")
   .IsDependentOn("SonarBegin")
    .IsDependentOn("Build")
  .IsDependentOn("SonarEnd")
   .IsDependentOn("Test");
	
Task("Coverlet")  
    .IsDependentOn("Build")
   .IsDependentOn("Test");

RunTarget(target);
