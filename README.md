<p align="center">
  <img src="https://github.com/MGrime/Cecilia/blob/master/Images/Med%20Brand.png">
</p>

## What is Cecilia?

Cecilia aims to be a versitile and easy to use Discord Music Bot. It is a self-hosted bot meaning you must provide the server that will run the bot through your own bot application. Cecilia provides builds for both x86_64 and arm machines. This means you can host Cecilia on a Raspberry Pi!

Currently in this early stage of development it supports playback of music from Youtube, through both direct links and searches, however extra services will be added as development progresses.

## Completed Features

* Play audio from Youtube at the highest possible bitrate.
* Spotify integration to find playing song on spotify and link it in chat.
* Search with terms or a URL
* Pause/Resume/Skip control at any time.
* Vote based skipping system with dynamic messaging updating.
* Unlimited queue size.
* Rich Embed responses and automatic message deletion for a clean interface.
* Serving of music from a single host to multiple servers (**WARNING**: Functionality is very experimental and largely untested. One host per server is highly recommended).
* Automatic download cleanup.

## Planned Features
* Support other platforms for audio download (Soundcloud, Vimeo etc.).
* Discord Rich Presence integration.
* Web interface for management.
* Removing from queue.

## How To Use

### Recommended - Docker

The recommended way of running Cecilia is through the Docker image. This is hosted on [Docker Hub](https://hub.docker.com/repository/docker/mgrime/cecilia/). Two images are provided:
1. An amd64 image - This is the image you should run if you are using a standard PC. It can run using Docker on Linux or Windows (requires Linux containers to be enabled)
2. An arm32 Image - This image can be run on ARM processors. Currently the only tested platform is Raspi OS on a Raspberry Pi 3 B+. However I see no reason it shouldn't work on other Raspberry Pi variants; however performance may be poor on a lower end Raspberry Pi.

Using the Docker image is the best way to host Cecilia, especially if have multiple servers, as you can make different containers for the each server (each using a unique bot token) from a single image. This is the recommended way to achieve this.

### Natively

If you are unable to run the docker image the two methods to run Cecilia natively are either through a stable binary from the Releases tab or by forking the code and compiling the DLL yourself.
Cecilia is built on top of .NET Core which is cross-platform however, the currently supported platforms are Windows and Linux. You may attempt to run the DLL on any platform that supports the .NET Core SDK however, this is done at your own risk as we do not test for these platforms.

### Please Note

Before running the bot through the docker image or natively you must create a bot application and add it to the server you wish to run Cecilia in. The steps for these can be found [here](https://docs.stillu.cc/guides/getting_started/first-bot.html). Follow the first section "Creating a Discord Bot" to create a bot application and add it to your server, you can ignore the rest of the instructions. When adding the bot to your server please use the permissions number generated [here](https://finitereality.github.io/permissions-calculator/?v=37211200). This will give Cecilia all of the required permissions for it's features.

If you wish to use spotify integration you must create a bot [here](https://developer.spotify.com/dashboard/login); this is optional and the bot will run fine without spotify integration being enabled.

### Downloading the Binary

#### Windows

1. Download a release from the Releases tab and extract to your running folder.
2. Install FFMPEG from [here](https://www.gyan.dev/ffmpeg/builds/). Choose the release download and extract the 3 .exe files to the folder alongside the Cecilia DLL. Download the Sodium and Opus DLL's from [here](https://dsharpplus.github.io/natives/index.html) and place them in the running folder. Rename libopus.dll to opus.dll.
3. Execute Cecilia_NET.dll using .NET Core through a Command Prompt or Powershell and follow the instructions.

#### Linux

1. Download a release from the Releases tab and extract to your running folder.
2. Install FFMPEG, Sodium and Opus through your distribution's package manager.
3. Execute Cecilia_NET.dll using .NET Core in a terminal and follow the instructions.

### Compilation

**THIS IS NOT A RECOMMENDED METHOD DUE TO THE FREQUENCY OF CODE CHANGES** 

1. Clone the repository.
2. Open the .sln file in a compatible IDE (Tested include VS2019 and Rider by JetBrains). The important feature it must have is the NuGet Package Manager as this will pull the required libraries.
3. Build the solution and run through your chosen IDE.

### Screenshots
<p align="center">
  <img src="https://github.com/MGrime/Cecilia/blob/master/Images/NowPlaying.png">
  <img src="https://github.com/MGrime/Cecilia/blob/master/Images/AddedSong.png">
</p>

<p align="center">
  <img src="https://github.com/MGrime/Cecilia/blob/master/Images/Queue.png">
  <img src="https://github.com/MGrime/Cecilia/blob/master/Images/Skip.png">
</p>


### Licenses and Thanks

Cecilia uses primarily MIT licensed libraries. The only exceptions to this are YoutubeExplode (LGPL-3.0). To be safe Cecilia is licensed under the same LGPL-3.0 license. Cecilia is not a commercial project and the code will always be fully open source.

We give our thanks and recognition to the libraries used that allow Cecilia to be developed smoothly:
* [Discord.NET](https://github.com/discord-net/Discord.Net)
* [Newtonsoft.Json](https://www.newtonsoft.com/json)
* [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET/)
* [YoutubeSearch](https://www.nuget.org/packages/YouTubeSearch)
* [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode)

### Contact

The fastest way is to contact either myself via email [here](mailto:MGrime1@uclan.ac.uk) or James Easton [here](https://www.jameseaston.co.uk/#contact). If it is regarding a code contribution then please make a pull request and I will look at it as soon as I can.

