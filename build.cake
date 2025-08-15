
var TARGET = Argument("t", Argument("target", "ci"));

Task("binderate")
	.Does
	(
		() =>
		{
			FilePath config_file = MakeAbsolute(new FilePath("./config.json")).FullPath;
			DirectoryPath base_path = MakeAbsolute(new DirectoryPath("./")).FullPath;

			int exit = StartProcess
								(
									"xamarin-android-binderator",
									$"--config=\"{config_file}\" --base_path=\"{base_path}\""
								);
			if (exit != 0) 
			{
				throw new Exception($"xamarin-android-binderator exited with code {exit}.");
			}
		}
	);

Task("native")
	.Does
	(
		() =>
		{
			/*
			string fn = IsRunningOnWindows() ? "gradlew.bat" : "gradlew";
			FilePath gradlew = MakeAbsolute((FilePath)("./native/KotlinSample/" + fn));
			int exit = StartProcess
						(
							gradlew, 
							new ProcessSettings 
							{
								Arguments = "assemble",
								WorkingDirectory = "./native/KotlinSample/"
							}
						);
			if (exit != 0)
			{
				throw new Exception($"Gradle exited with exit code {exit}.");
			}
			*/
		}
	);

Task("externals")
	.IsDependentOn("binderate")
	.IsDependentOn("native");


Task("libs")
	.IsDependentOn("externals")
	.Does
	(
		() =>
		{
			DotNetMSBuildSettings settings_dotnet_msbuild = new DotNetMSBuildSettings()
													.SetConfiguration("Release")
													.EnableBinaryLogger("./output/libs.binlog")
													.WithProperty("DesignTimeBuild", "false")
													.WithTarget("Build");
			DotNetBuildSettings settings_dotnet_build = new ()
															{
																MSBuildSettings = settings_dotnet_msbuild
															};
			
			DotNetBuild("./generated/Xamarin.Kotlin.sln", settings_dotnet_build);
		}
	);

Task("nuget")
	.IsDependentOn("libs")
	.Does(() =>
{
	var settings = new MSBuildSettings()
		.SetConfiguration("Release")
		.EnableBinaryLogger("./output/nuget.binlog")
		.WithProperty("NoBuild", "true")
		.WithProperty("PackageOutputPath", MakeAbsolute((DirectoryPath)"./output/").FullPath)
		.WithTarget("Pack");

	MSBuild("./generated/Xamarin.Kotlin.sln", settings);
});

Task("samples")
	.IsDependentOn("libs")
	.Does(() =>
{
	var settings = new MSBuildSettings()
		.SetConfiguration("Release")
		.SetVerbosity(Verbosity.Minimal)
		.EnableBinaryLogger("./output/samples.binlog")
		.WithRestore()
		.WithProperty("DesignTimeBuild", "false");

	MSBuild("./samples/KotlinSample.sln", settings);
});

Task("clean")
	.Does(() =>
{
	CleanDirectories("./generated/*/bin");
	CleanDirectories("./generated/*/obj");

	CleanDirectories("./externals/");
	CleanDirectories("./generated/");
	CleanDirectories("./native/.gradle");
	CleanDirectories("./native/**/build");
});

Task("ci")
	.IsDependentOn("externals")
	.IsDependentOn("libs")
	.IsDependentOn("nuget")
	.IsDependentOn("samples");

RunTarget(TARGET);
