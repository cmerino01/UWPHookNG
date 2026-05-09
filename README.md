# UWPHookNG

> A modernized fork of [UWPHook](https://github.com/BrianLima/UWPHook) by Brian Lima — rebuilt for .NET 10 and current Windows builds.

Small project to link UWP / Microsoft Store / Xbox Game Pass games and apps to Steam.

If you want to add Windows Store or Xbox Game Pass Games to Steam, you need a bit of a workaround because Steam can't see UWP apps and won't always show them on your "Currently playing" status. This app automates that.

## What's different from upstream

- Targets **.NET 10** (not .NET Framework 4.8). Works on Windows 10 1809+ and Windows 11.
- UWP app discovery uses native **WinRT `PackageManager`** instead of hosting PowerShell in-process — much smaller publish output and faster startup.
- Modernized stack: System.Text.Json (no Newtonsoft), System.Net.Http.Json (no WebApi.Client), `HttpClient` (no `WebClient`).
- Atomic `shortcuts.vdf` writes with rolling backups under `%APPDATA%\Briano\UWPHook\backups\`.
- SteamGridDB API key encrypted at rest with DPAPI.
- New Xbox / Steam Big Picture-inspired dark UI with tile + list views.
- Builds for **win-x64 and win-arm64**.
- Test suite + CI with `dotnet test` on every push.

## To add UWP or XGP games to Steam

[Download the latest UWPHookNG release](https://github.com/cmerino01/UWPHookNG/releases) and store it somewhere on your PC.

Click **Refresh** to load installed UWP apps. Every UWP app and Xbox Game Pass game on your PC will appear as a tile.

![](https://i.imgur.com/pjGfGHe.png)

Select every app you want to add to Steam, you can change the name by double clicking the "name" collumn, press "Export selected apps to Steam" and 🎉, every app you selected will be added to Steam.

![](https://i.imgur.com/on46CMQ.png)

Close UWPHook, restart Steam if prompted, click play on your UWP game, and Steam will show your current game on your status as long as you are playing it!

----------

# SteamGridDB #

Since v2.8, UWPHook can automatically import grid, icons and hero images from [SteamGridDB](https://www.steamgriddb.com)

On your first usage, the app will ask you if you want it to download images, redirecting you to the settings page.

![](https://i.imgur.com/K0Cm4IL.png)

By adding a API Key obtained in the SteamGridDB preferences, UWPHook will try to find matching images for any exported games, giving you the following result:

![](https://i.imgur.com/mlvVdwb.png)

You can refine the images by using filters for animated images, blurred, no logo or memes for example, but it will always pick the first it finds for the filters automatically.

Special thanks to @FusRoDah061 for implementing the base feature!

# Troubleshooting #

- **Steam's Overlay isn't working!**
  - Unfortunately, it's a Steam limitation, Valve has to update it in order to work properly with UWP, DXTory is a recommended overlay for UWP games.
- **Using Steam Link**
  - Check the option "Streaming" mode inside the settings screen
- **Steam Deck?**
  - This app is not compatible with the Steam Deck in any way.

 If you are facing an error of any kind, please check the contents of the file 
- **I have shortcuts from other application that broke when i used UWPHook**
  - You can find a backup of your `shortcuts.vdf` file in `%appdata%\Roaming\Briano\UWPHook\backups`, each file in this directory is a backup of the original `shortcuts.vdf` before manipulation by UWPHook, the files are renamed `{userid}_{timestamp}_shortcut.vdf`, you can restore these files to their original location for usage.
- **My question isn't listed here!**
  - Drop by our subreddit and ask a question over there, maybe someone will help you, i surely will as soon as i can:
 **[https://www.reddit.com/r/uwphook](https://www.reddit.com/r/uwphook)**
  - Please also paste the contents of the file `%appdata%Roaming\Briano\UWPHook\application.log` so i can try to understand better the problem.
----------

# Building (UWPHookNG)

This fork targets modern .NET — no .NET Framework or external SharpSteam clone required.

**Requirements**

- Windows 10 or 11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or any newer SDK that can target `net10.0-windows`)
- Visual Studio 2022 17.12+ / Visual Studio 2026 (optional — the .NET CLI alone is sufficient)

**Clone with submodules** (this repo uses a git submodule for `VDFParser`):

```powershell
git clone --recurse-submodules https://github.com/cmerino01/UWPHookNG.git
```

If you already cloned without `--recurse-submodules`:

```powershell
git submodule update --init --recursive
```

**Build**

```powershell
dotnet build UWPHook.sln -c Release
```

Or open `UWPHook.sln` in Visual Studio and build/run normally.

**Publish a single-file exe**

```powershell
dotnet publish UWPHook\UWPHook.csproj -c Release -r win-x64 --self-contained true
```

Output: `UWPHook\bin\Release\net10.0-windows\win-x64\publish\UWPHook.exe`

## Installer 

The installer is built with [NSIS](https://nsis.sourceforge.io/Download), just run the script `UWPHook.nsi` and things **should** work. Modify any hardcoded paths to suit your setup.
The installation consists of zipping the application and creating some of the paths for the user, since the application is mostly static/dynamic and does not depend a lot on where it is installed, the installer is made for convenience.

----------

# About

UWPHookNG is open-source under the MIT License (see `LICENSE`).

- Original UWPHook © 2014–2024 [Brian Lima](https://github.com/BrianLima) — without his work this fork wouldn't exist. If you'd like to support him, [PayPal](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=9YPV3FHEFRAUQ).
- UWPHookNG modifications © 2026 [merinocodes](https://github.com/cmerino01).

This is a personal fork. APIs, file formats, scripts and the things UWPHookNG depends on can change without notice; expect breakage if Microsoft or Valve change something underneath. No warranty.
