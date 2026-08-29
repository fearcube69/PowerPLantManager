### I just made this project because changing powerplant setting is such as hassle.
By default your CPU will run with boost clock on especially on the X86/X64 cpu or whatever the name is.
If you set the max cpu clock to less than 100%, it will turn off the CPU boost clock somehow but ok

Why turn your CPU Boost Clock OFF?
weeelll, these are the benefit
- lower cpu temp
- less fan noise if you're on laptop
- lower power consumption

In some instance, expect a bit of performance loss but some game or task have barely performance loss.
Well, try it out and have fun I guess....

### I think I forgot to tell you how to compile and install...
#### Here it is 

## Building & Packaging

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or latest .NET SDK)
* Windows 10 / 11 (64-bit)

---

### Option 1: Build Portable Executable (Single-File `.exe`)

Compile the application and runtime into a single standalone `.exe` that runs on any Windows PC without needing .NET installed:

1. Open PowerShell in the project root folder.
2. Run the single-file publish command:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
