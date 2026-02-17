# StidhamCommander

![License: MIT License](https://img.shields.io/badge/License-mit-blue.svg)

A high-performance, cross-platform "Orthodox File Manager" (OFM) built with **.NET 10 (LTS)** and **C# 14**.

StidhamCommander is a dual-pane file management system designed for power users who value speed and keyboard-centric workflows. This project serves as a professional portfolio piece demonstrating Clean Architecture, modern C# paradigms, and cross-platform systems programming.

## Key Features

- **Dual-Pane Interface:** Efficient file operations inspired by Total Commander and Midnight Commander.
- **Cross-Platform:** Developed on **Ubuntu Cinnamon 24.04 LTS**. Fully compatible with Windows and macOS.
- **Hybrid UI:** Features both a **Terminal.Gui** TUI and an **Avalonia UI** desktop frontend.
- **Native AOT Ready:** Optimized for Ahead-of-Time compilation for near-instant startup.

## Safety & Security

**Protected Path Enforcement:**

StidhamCommander enforces strict safety policies to prevent accidental damage to system files. The following operations are **explicitly blocked** on protected system paths:

- **Copying** to or from protected paths
- **Moving** protected directories or moving to protected locations
- **Deleting** protected directories or files within protected paths
- **Renaming** protected system directories

**Protected Paths Include:**

- **Linux/macOS:** `/`, `/bin`, `/sbin`, `/etc`, `/usr`, `/sys`, `/proc`, `/boot`, `/dev`, `/var`, user home root
- **Windows:** `C:\`, `C:\Windows`, `C:\Program Files`, `C:\Program Files (x86)`, `C:\ProgramData`, user profile root
- **macOS:** `/System`, `/Library`, `/Applications`, `/Volumes`

**Administrator Operations:**

This file manager is designed for **non-privileged user operations only**. If you need to perform file operations on system directories:

1. Use your terminal with `sudo` (Linux/macOS) or elevated privileges (Windows)
2. Use your system's native file explorer with administrator rights
3. StidhamCommander will **never** request or support privilege elevation

This design philosophy ensures the file manager remains safe for everyday use and prevents accidental system damage. Power users requiring system-level operations should use appropriate system tools designed for that purpose.

## Architecture

The project follows a decoupled **Onion Architecture** to ensure the core business logic remains independent of the UI framework.

- **Stidham.Commander.Core:** A shared library containing:
  - File operation services (Copy, Move, Delete, Rename)
  - Protected path enforcement with cross-platform guards
  - Observable patterns (events, `IProgress<T>`) for UI progress reporting
  - Custom exception types for structured error handling
  - **Provides NO UI** - pure file operation logic only
- **Stidham.Commander.TUI:** A terminal-based interface using `Terminal.Gui`.
- **Stidham.Commander.GUI:** A modern desktop interface using `Avalonia UI`.
- **Stidham.Commander.Core.Tests:** Comprehensive unit test suite using `xUnit` with isolated disposable test environments.

**Core Design Principles:**

- Core library throws `ProtectedPathException` when operations target system paths
- Frontend applications (TUI/GUI) catch exceptions and present user-friendly error messages
- No privilege elevation is ever attempted - operations fail cleanly with informative errors
- All file operations support cancellation via `CancellationToken`

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

## Exception Handling for Developers

When consuming `Stidham.Commander.Core`, be prepared to handle these exceptions:

- **`ProtectedPathException`** (Phase 2): Thrown when attempting operations on system-protected paths
- **`FileNotFoundException`**: Source file/directory doesn't exist
- **`IOException`**: File collision (e.g., destination already exists), or other I/O errors
- **`OperationCanceledException`**: Operation was cancelled via `CancellationToken`
- **`UnauthorizedAccessException`**: Insufficient permissions (non-system paths where user lacks access)

**Example Error Handling Pattern:**

```csharp
try
{
    await fileOperationService.CopyAsync(source, destination, ct: cancellationToken);
}
catch (ProtectedPathException ex)
{
    // Show user: "Cannot copy to/from system directory"
    logger.LogWarning("Protected path access attempt: {Path}", ex.Path);
}
catch (OperationCanceledException)
{
    // Show user: "Operation cancelled"
}
catch (IOException ex)
{
    // Show user: "File already exists" or other I/O error
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
