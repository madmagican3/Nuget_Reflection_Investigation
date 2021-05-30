# Nuget_Reflection_Investigation

This is a simple console application that gets all the packages from all the catalogs currently in nuget.org. It then extracts the zips, and using mono.cecil investigates them for any instances of reflection. It then writes the results into a folder specified at startup.

To use in program.cs, change the working directory to a folder you're willing to let it work in. 
