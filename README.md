# StidhamCommander

![License: MIT License](https://img.shields.io/badge/License-mit-blue.svg)

A high-performance, cross-platform "Orthodox File Manager" (OFM) built with **.NET 10 (LTS)** and **C# 14**.

StidhamCommander is a dual-pane file management system designed for power users who value speed and keyboard-centric workflows. This project serves as a professional portfolio piece demonstrating Clean Architecture, modern C# paradigms, and cross-platform systems programming.

## Key Features
- **Dual-Pane Interface:** Efficient file operations inspired by Total Commander and Midnight Commander.
- **Cross-Platform:** Developed on **Ubuntu Cinnamon 24.04 LTS**. Fully compatible with Windows and macOS.
- **Hybrid UI:** Features both a **Terminal.Gui** TUI and an **Avalonia UI** desktop frontend.
- **Native AOT Ready:** Optimized for Ahead-of-Time compilation for near-instant startup.

## Architecture
The project follows a decoupled **Onion Architecture** to ensure the core business logic remains independent of the UI framework.

- **Stidham.Commander.Core:** A shared library containing the "Virtual File System" engine, path normalization, and file operation logic.
- **Stidham.Commander.TUI:** A terminal-based interface using `Terminal.Gui`.
- **Stidham.Commander.GUI:** A modern desktop interface using `Avalonia UI`.
- **Stidham.Commander.Core.Tests:** Comprehensive unit test suite using `xUnit` with isolated disposable test environments.

## Technical Stack
- **Language:** C# 14 (utilizing `field` keywords, extension members, and `Span<T>`)
- **Framework:** .NET 10.0 (LTS)
- **Testing:** xUnit
- **UI Frameworks:** Terminal.Gui, Avalonia UI
- **Dev Environment:** Ubuntu Cinnamon 24.04 LTS, Fish Shell

## Getting Started

### Prerequisites
- .NET 10 SDK

### Build & Test
```bash
# Clone the repository
git clone git@github.com-snhu:samuel-stidham/StidhamCommander.git

# Restore dependencies
dotnet restore

# Run the unit tests
dotnet test

# Run the TUI
dotnet run --project src/Stidham.Commander.TUI
```

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.