# Destiny Ghost Assistant

## Overview
A native Windows application (C# WPF) that implements a Destiny 2 inspired Ghost assistant, using OpenRouter as a backend for AI interaction. Features include live chat and planned system interaction tools.

## Features
- Live chat with an AI assistant (powered by OpenRouter).
- Persona-driven responses based on a system prompt.
- Conversation history management.
- (Planned) Tool invocation for system interactions (e.g., file creation/reading).
- Minimalist, neuromorphic-inspired UI (conceptual).

## Prerequisites
- .NET SDK (Version 8.0 or later recommended). Download from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- Windows Operating System (for running the WPF application).
- An OpenRouter API Key (get from [https://openrouter.ai/](https://openrouter.ai/))

## Setup and API Key Configuration

1.  **Clone or Download:** Get a copy of the application files.
2.  **API Key (Handled on First Run):**
    *   This application requires an OpenRouter API key to function. (You can obtain one from [https://openrouter.ai/](https://openrouter.ai/))
    *   On the **first time you run the application**, if an API key is not found cached locally, you will be **prompted to enter your OpenRouter API key** via a dialog box.
    *   Enter your valid API key when prompted and click "OK".
    *   The application will then save this key to a local cache file for future use. You will not be prompted again unless this cache file is deleted or becomes corrupted.
3.  **API Key Cache Location:**
    *   The API key is cached in a file named `settings.dat` located in the application's local data folder:
        `%LOCALAPPDATA%\DestinyGhostAssistant\settings.dat`
        (e.g., `C:\Users\YourUserName\AppData\Local\DestinyGhostAssistant\settings.dat`)
    *   If you wish to change or remove the API key, you can delete this `settings.dat` file. The application will then prompt you for a new key on its next startup.
4.  **Security Note:** While this method is more user-friendly than hardcoding, be aware that the key is stored in plain text in the `settings.dat` file on your local machine. For enhanced security in environments where others might access your local application data, consider filesystem permissions or other local security measures. This application does not encrypt the cached key.

## Building the Application
1. Open a terminal or command prompt.
2. Navigate to the solution root directory: `cd DestinyGhostAssistant`
3. Navigate to the project directory: `cd DestinyGhostAssistant` (this is the inner one containing the .csproj file).
4. Restore dependencies: `dotnet restore`
5. Build the project: `dotnet build` (for a Debug build)
   Or for a Release build: `dotnet build -c Release`

## Running the Application
After a successful build:
**From the project directory (`DestinyGhostAssistant/DestinyGhostAssistant`):**
  - For Debug: `dotnet run` or navigate to `bin/Debug/net8.0-windows/` (adjust TargetFramework if different) and run `DestinyGhostAssistant.exe`.
  - For Release: Navigate to `bin/Release/net8.0-windows/` (adjust TargetFramework) and run `DestinyGhostAssistant.exe`.

## Publishing the Application (Creating an Executable Package)
To create a self-contained executable package:
1. Open a terminal in the project directory (`DestinyGhostAssistant/DestinyGhostAssistant`).
2. For a framework-dependent publish (smaller size, requires .NET runtime on target machine):
   `dotnet publish -c Release`
   The output will be in `bin/Release/net8.0-windows/publish/`.
3. For a self-contained publish (larger size, includes .NET runtime, more portable):
   `dotnet publish -c Release --runtime win-x64 -p:PublishSingleFile=true --self-contained true` (Example for x64)
   (You might need to specify a target runtime identifier, e.g., `win-x64`, `win-x86`.)
   The output will be in `bin/Release/net8.0-windows/win-x64/publish/` (or similar based on runtime).
   The single executable will be in this directory.

## Project Structure
- `/DestinyGhostAssistant` (Solution Root)
  - `/DestinyGhostAssistant` (WPF Project Directory)
    - `/Assets` (Images, etc.)
    - `/Models` (Data structures)
    - `/ViewModels` (MVVM ViewModels)
    - `/Services` (API clients, other services)
    - `/Views` (User controls, if any - currently MainWindow is primary view)
    - `/Utils` (Utility classes like RelayCommand, Converters)
    - `/Resources` (XAML Resource Dictionaries, if any)
  - `README.md` (This file)

## Contributing
(Optional: Add guidelines if you plan for contributions.)
