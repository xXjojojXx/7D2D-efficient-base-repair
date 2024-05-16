# How to build

1. Configure the required environnement variables

    * `PATH_7D2D`: the path to the folder `path\to\7 Days To Die`

    * `7z.exe`: The executable of [7-Zip](https://www.7-zip.org/download.html) (needed to build the final zip file). It must be added to your env variable `Path`

2. Install [dotnet](https://dotnet.microsoft.com/en-us/download)

3. Build the dll by running [compile.cmd](./Scripts/compile.cmd) from the project root

4. (optional) Run [Release.cmd](./Scripts/release.cmd) to compile, then to enpack all the files in EfficientBaseRepair.zip and to load it in the Mod folder of the installation pointed by the env variable `PATH_7D2D`