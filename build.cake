#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.3.1"
 #addin nuget:?package=Cake.Sonar&version=1.1.18
#tool "nuget:?package=OctopusTools"
#addin nuget:?package=Cake.Coverlet


var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");
var coverletDirectory = Directory("./coverage-results");
var publishorderApi = Directory("./publishOrderApi/");
var solutionFile = "Core_Sonar.sln";  
var websolutionFile = "./Core_Sonar/Core_Sonar.csproj";



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
  
   // Look under a 'Tests' folder and run dotnet test against all of those projects.
// Then drop the XML test results file in the Artifacts folder at the root.
Task("Test")
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
			CoverletOutputFormat = CoverletOutputFormat.opencover,
			  CoverletOutputDirectory = Directory(@".\coverage-results\"),
        CoverletOutputName = $"resultss"
                }
                );
     
    });
  

	Task("Sonar")
   .IsDependentOn("SonarBegin")
    .IsDependentOn("Build")
  .IsDependentOn("SonarEnd")
   .IsDependentOn("Test");
	
Task("Coverlet")  
    .IsDependentOn("Build")
   .IsDependentOn("Test");

RunTarget(target);
