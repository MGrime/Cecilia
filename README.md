![Image of Splash Art](https://github.com/MGrime/Cecilia/blob/master/Images/Small%20Brand%20With%20Smile.png)
# A Discord Music bot written in C# and Discord.NET

## What is Cecilia?

Cecilia aims to be a versitile and easy to use Discord Music Bot. Currently in this early stage of development it supports playback of music from Youtube, through both direct links and searches, however extra services will be added as development progresses.

## Completed Features

* Play audio from Youtube at the highest possible bitrate.
* Search with terms or a URL
* Pause/Resume/Skip control at any time.
* Unlimited queue size
* Rich Embed responses and automatic message deletion for a clean interface
* Serving of music from a single host to multiple servers (**WARNING**: Functionality is experimental)
* Automatic download cleanup.

## Planned Features

* Support other platforms for audio download (Soundcloud, Vimeo etc.).
* Spotify API integration.
* Discord Rich Presence integration.
* Limit skipping & leaving to user who requested the current song.
* Web interface for management.

## How To Use

The two methods to run Cecilia are either through a stable binary from the Releases tab or by forking the code and compiling the DLL yourself.
Cecilia is built on top of .NET Core which is cross-platform however, the currently supported platforms are Windows and Linux. You may attempt to run the DLL on any platform that supports the .NET Core SDK however, this is done at your own risk as we do not test for these platforms.

### Downloading the Binary

#### Windows

1. Download a release from the Releases tab and extract to your running folder.
2. Install FFMPEG and add to the system path, Sodium and Opus and place the Sodium and Opus DLLs in the same directory as the downloaded binary.
3. Execute Cecilia_NET.dll using .NET Core through a Command Prompt or Powershell.

#### Linux

1. Download a release from the Releases tab and extract to your running folder.
2. Install FFMPEG, Sodium and Opus through your distribution's package manager.
3. Execute Cecilia_NET.dll binary using .NET Core in your terminal emulator.

### Compilation

**THIS IS NOT A RECOMMENDED METHOD DUE TO THE FREQUENCY OF CODE CHANGES** 

1. Clone the repository.
2. Open the .sln file in a compatible IDE (Tested include VS2019 and Rider by JetBrains). The important feature it must have is the NuGet Package Manager as this will pull the required libraries.
3. Build the solution and run through your chosen IDE.

### Screenshots

**Coming Soon**

### Contact

The fastest way is to contact either myself via email [here](mailto:MGrime1@uclan.ac.uk) or James Easton [here](https://www.jameseaston.co.uk/#contact). If it is regarding a code contribution then please make a pull request and I will look at it as soon as I can.

