# Ghost Desktop Agent

## Overview
A modern, floating desktop agent with a sleek dark UI inspired by shadcn. This Windows application features AI-powered chat capabilities through OpenRouter, with tools for reading and writing files. The agent appears as a small, always-on-top floating window that you can position anywhere on your screen.

## Features
- **Modern Dark UI** - Beautiful shadcn-inspired dark theme with smooth animations
- **Floating Window** - Small, draggable window that stays on top of other applications
- **AI Chat** - Powered by OpenRouter with support for multiple LLM models
- **File Tools** - Create, read, and write files through natural language
- **Conversation Management** - Save and load chat histories
- **Customizable** - Adjust system prompts and select different AI models

## Screenshots & UI
The application features:
- Compact floating window (380x700px by default)
- Transparent borders with drop shadow effect
- Draggable title bar with quick-access buttons
- Modern message bubbles with distinct colors for user/assistant/system messages
- Smooth dark theme optimized for extended use

## Prerequisites
- **Windows Operating System** (Windows 10 or later recommended)
- **.NET 8.0 SDK or Runtime** - Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
  - For development: Download the SDK
  - For just running the app: Download the Runtime (Desktop Runtime)
- **OpenRouter API Key** - Get one free at [https://openrouter.ai/](https://openrouter.ai/)

## Installation Guide for Windows

### Option 1: Simple Installation (Recommended for Users)

1. **Install .NET 8.0 Desktop Runtime**
   - Go to [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Download "**.NET Desktop Runtime 8.0.x**" for Windows x64
   - Run the installer and follow the prompts

2. **Download the Application**
   - Download the latest release from the GitHub releases page
   - Or clone this repository:
     ```
     git clone https://github.com/ArmDaniel/electron-ghost.git
     cd electron-ghost
     ```

3. **Build the Application**
   - Open Command Prompt or PowerShell
   - Navigate to the project directory:
     ```
     cd DestinyGhostAssistant\DestinyGhostAssistant
     ```
   - Build the application:
     ```
     dotnet build -c Release
     ```

4. **Run the Application**
   - After building, run:
     ```
     dotnet run --configuration Release
     ```
   - Or navigate to `bin\Release\net8.0-windows\` and double-click `DestinyGhostAssistant.exe`

5. **First-Time Setup**
   - On first run, you'll be prompted to enter your OpenRouter API key
   - Get a free API key from [https://openrouter.ai/](https://openrouter.ai/)
   - Paste your API key and click OK
   - The key will be saved locally for future use

### Option 2: Creating a Standalone Executable

For a portable version that doesn't require .NET to be installed:

1. **Navigate to the project directory:**
   ```
   cd DestinyGhostAssistant\DestinyGhostAssistant
   ```

2. **Publish as a self-contained executable:**
   ```
   dotnet publish -c Release --runtime win-x64 -p:PublishSingleFile=true --self-contained true
   ```

3. **Find your executable:**
   - Navigate to `bin\Release\net8.0-windows\win-x64\publish\`
   - Copy `DestinyGhostAssistant.exe` to wherever you want
   - You can now run this .exe on any Windows machine without installing .NET!

### Option 3: Developer Setup

1. **Install .NET 8.0 SDK**
   - Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Choose the SDK (not just Runtime)

2. **Clone the repository:**
   ```
   git clone https://github.com/ArmDaniel/electron-ghost.git
   cd electron-ghost\DestinyGhostAssistant\DestinyGhostAssistant
   ```

3. **Restore dependencies:**
   ```
   dotnet restore
   ```

4. **Build and run:**
   ```
   dotnet build
   dotnet run
   ```

## Using the Application

### Basic Usage
1. **Positioning** - Drag the window by the title bar to position it anywhere on your screen
2. **Chat** - Type your message in the input box and press Enter or click Send
3. **Tools** - Ask the agent to read or write files naturally (e.g., "read the file at C:\Documents\notes.txt")
4. **Settings** - Click the ⚙ icon to adjust AI model and system prompt
5. **Save/Load** - Use 💾 to save conversations and 📂 to load them
6. **New Chat** - Click + to start a fresh conversation
7. **Close** - Click × to exit the application

### Available Tools
The AI assistant can use these tools when needed:
- **read_file_content** - Read text from any file on your computer
- **create_file** - Create new text files
- **write_file** - Write or update existing files

All file operations require your confirmation before executing for security.

### Settings
Access settings via the ⚙ icon:
- **AI Model** - Choose from various OpenRouter models (default: nousresearch/deephermes-3-mistral-24b-preview:free)
- **System Prompt** - Customize the AI's personality and behavior
- **API Key** - Update your OpenRouter API key if needed

## API Key Management

### Where is my API key stored?
Your OpenRouter API key is stored locally at:
```
%LOCALAPPDATA%\DestinyGhostAssistant\settings.dat
```
(Example: `C:\Users\YourName\AppData\Local\DestinyGhostAssistant\settings.dat`)

### Changing your API key
1. Delete the `settings.dat` file from the location above
2. Restart the application
3. Enter your new API key when prompted

### Security Note
The API key is stored in plain text on your local machine. While this is convenient, be aware that anyone with access to your user account could read this file. For enhanced security, ensure your Windows user account is password-protected.

## Troubleshooting

### "Application won't start"
- Make sure you have .NET 8.0 Runtime installed
- Try running from Command Prompt to see error messages:
  ```
  cd path\to\DestinyGhostAssistant\DestinyGhostAssistant
  dotnet run
  ```

### "API Key errors"
- Ensure you have a valid OpenRouter API key
- Delete `%LOCALAPPDATA%\DestinyGhostAssistant\settings.dat` and restart
- Check your internet connection

### "Window appears but is all black/white"
- This might be a graphics driver issue
- Try updating your graphics drivers
- The application requires Windows 10 or later

### "File tools not working"
- Ensure the file path is valid and accessible
- Check that you have read/write permissions for the specified location
- Remember to confirm the operation when prompted

## Advanced Configuration

### Customizing the Window
Edit `MainWindow.xaml` to adjust:
- Window size (Height/Width properties)
- Colors (modify SolidColorBrush resources)
- Position on startup

### Adding Custom Tools
1. Create a new class in `Services/Tools/` implementing `ITool`
2. Register it in `ToolExecutorService.cs` in the `RegisterTools()` method
3. Rebuild the application

## Project Structure
```
/DestinyGhostAssistant
  /DestinyGhostAssistant (WPF Project Directory)
    /Assets - Images and icons
    /Models - Data structures
    /ViewModels - MVVM ViewModels
    /Services - API clients and tool executors
      /Tools - File operation tools
    /Views - Additional UI components
    /Utils - Utility classes
```

## Building From Source

### Debug Build
```bash
cd DestinyGhostAssistant\DestinyGhostAssistant
dotnet build
```

### Release Build
```bash
dotnet build -c Release
```

### Publish Single-File Executable
```bash
dotnet publish -c Release --runtime win-x64 -p:PublishSingleFile=true --self-contained true
```

## Tips for Best Experience

1. **Position Wisely** - Place the window in a corner of your screen where it won't obstruct your work
2. **Use Keyboard Shortcuts** - Press Enter to send messages quickly
3. **Save Important Chats** - Use the save feature to keep useful conversations
4. **Experiment with Models** - Different models have different strengths; try a few!
5. **Clear System Prompts** - Well-crafted system prompts lead to better responses

## Known Limitations

- File tools only support text files (no binary files)
- No markdown rendering (plain text only for now)
- Windows-only (WPF is Windows-specific)
- Requires internet connection for AI features

## Privacy & Data

- All chat data is stored locally on your machine
- Conversations are sent to OpenRouter's API for AI responses
- No telemetry or tracking is implemented
- Your API key is stored in plain text locally

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is open source. Check the repository for license details.

## Support

For issues, questions, or feature requests:
- Open an issue on GitHub
- Check existing issues for solutions
- Review the troubleshooting section above
