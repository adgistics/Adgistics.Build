# ARP #

Adgistics Release Process (ARP) is an MsBuild toolset that provides a custom MsBuild configuration that integrates properly into .NET project files allowing easy execution of the tasks via Visual Studio, Mono, or the command line.  

At the moment it provides tasks to do the following:
* NArrange
* Gendarme
* Gendarme report
* Nunit
* Nunit report
* Nuget packaging

## Steps to creating an ARP ready project ##

** Step 1.** 

Clone the ARP project onto your machine if you haven't done so already:

```
#!bash#

git clone https://github.com/adgistics/Adgistics.Build.git
```

** Step 2. **

Clone the ARP.Example project, which will aid by providing base templates for your project configuration:

```
#!bash

git clone https://github.com/adgistics/Adgistics.Build.Example.git
```

**Step 3.** 

Create a new project via visual studio, try to follow the following folder  structure:

```
#!text

   root
    |- ProjectName
    |  |- ProjectName.csproj
    |- SolutionName.sln
```

*Info: You cannot divert from this folder structure, as the properties bootstrapping for the build scripts depends on them.  MsBuild makes this a hard problem to solve unfortunately.*

** Step 4.** 

Create a symbolic link to your cloned ARP repository so that your folder  structure now becomes:

```
#!text

   root
    |- arp
    |- ProjectName
    |  |- ProjectName.csproj
    |- SolutionName.sln

```

** Step 5.** 

Copy the arp.config folder, .gitignore file, and the .gitattributes files  from the ARP.Example project into your project so that your folder structure now becomes:

```
#!text

   root
    |- arp
    |
    |- arp.config
    |  |- Gendarme
    |  |  |- known-issues.ignore
    |  |
    |  |- nuspec
    |  |  |- Bob.nuspec
    |  |
    |  |- Release.proj
    |
    |- ProjectName
    |  |- ProjectName.csproj
    |
    |- .gitignore
    |- .gitattributes
    |- SolutionName.sln

```



** Step 6. ** 

Create a version.txt file in your project with the following file contents:

```
#!text

   0.0.1.0
```

The format of that versioning is {Major}.{Minor}.{Point}.{Build}

If you want to change the version of your project you should edit this file.

**DO NOT CHANGE THE {Build} NUMBER YOURSELF.** This will be automatically populated by the ARP build to contain the actual build number.

This file must be in the root of your project, so you will now have the following folder structure:

```
#!text

   root
    |- arp
    |- arp.config
    |- ProjectName
    |  |- ProjectName.csproj
    |  |- version.txt
    |- .gitignore
    |- .gitattributes
    |- SolutionName.sln

```

** Step 7.**  

Edit your ProjectName.csproj file, so that the top of it includes the following section (customised to match your project's description):

```
#!xml

  <PropertyGroup>
    <AssemblyTitle>ProjectName</AssemblyTitle>
    <AssemblyDescription>The description for your project.</AssemblyDescription>
    <AssemblyProduct>ProductName</AssemblyProduct>
  </PropertyGroup>
```

and adjust the bottom of it so it matches the following:

```
#!xml

  <!--
  ******************************************************************************
  | ARP BUILD SYSTEM IMPORTS
  | It is really important that you keep these at the bottom of your project like this -->

  <!--
  | The properties file is the main file with all your configuration options.-->
  <Import Project="..\arp\MSBuild\ARP.properties" Condition="Exists('..\arp\MSBuild\ARP.properties')" />

  <!--
  | This is the standard CSharp Targets file.  This needs to be exactly here
  | so that all the correct properties are configured that it depends upon.-->
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />

  <!--
  | This are the additional/custom ARP build targets.-->
  <Import Project="..\arp\MSBuild\ARP.targets" Condition="Exists('..\arp\MSBuild\ARP.targets')" />
  <!--
  ******************************************************************************
  -->
</Project>
```

NOTE: Please make sure you don't have duplicate references to the *Microsoft.CSharp.targets* file.  This is the standard Microsoft targets file, and it has to be structured like above.

## The arp.config folder ##

The arp.config folder provides you with additional build configuration for your project as well as the capability to publish your libraries into NuGet packages.

### Preparing your libraries for NuGet packaging ###

In order to do this you need to have a *.nuspec file for each library that you wish to package.  The filename needs to match the name of your project exactly.  So if you have a project called *Bob.csproj* then you will need a nuspec file named *Bob.nuspec*.

Generally you don't have to do too much editing to these files.  Anything within the file with *$variable-name$* format is a variable that will automatically be switched out by the ARP build targets.

You will need to put in the NuGet package references and the framework assembly references that your project depends on.  This unfortunately cannot be automated in a trustworthy manner.

It is highly recommended that you read the official NuGet documentation on how to structure and manage your NuSpec files.

## Notes  ##

**Test Libraries**
If you have a test project it must have a name that ends with '-Test', 
example: Foo-Test.csproj

## Known Limitations ##

Only supports NuGet packaging on Libraries and EXE project types.  You can 
still integrate the ARP system for support of NArrange and Gendarme into your
web project, however a custom packaging system will be required.