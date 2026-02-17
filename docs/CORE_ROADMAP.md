# StidhamCommander: Core v1.0 Roadmap & Technical Specs

> **Status:** Phase 2 (Active)
> **Goal:** Implement a testable, cross-platform Write Engine using .NET 10/C# 14.

---

## Phase 1: Foundation [COMPLETE]

- [x] Solution Scaffolding (Clean Architecture structure)
- [x] `FileDiscoveryService`: Ordered directory listing.
- [x] `SizeFormatter`: C# 14 extension members for human-readable bytes.
- [x] Environment: `Makefile`, `.editorconfig`, and `.config/ai/` bootstrap.

## Phase 1.5: Frontend-Ready Abstractions [IN PROGRESS]

- [ ] **State Notification**: Implement events or `Observable` patterns so UIs can react to file system changes.
- [ ] **Cancellation Support**: Ensure every long-running operation in `FileOperationService` accepts a `CancellationToken`.
- [ ] **TUI/GUI Compatibility**: Verification that all Core models are serializable and observable.

---

## Phase 2: File Operations (Write Engine)

### 2.1 Exception & Safety Layer

- [ ] **ProtectedPathException**: Primary constructor exception for unauthorized access.
- [ ] **Cross-Platform Guard**: Logic to protect system roots (`/`, `C:\`), system folders (`/etc`, `/bin`), and user home roots across Linux, Windows, and macOS.

### 2.2 FileOperationService

Implement the following using `IFileSystem` abstractions:

- [ ] **Basic Operations**
  - `Delete(string path, bool recursive)`: Support for files/folders + Guard check.
  - `Rename(string path, string newName)`: Atomic same-volume rename with `IOException` on name collision.
- [ ] **Recursive Operations (Advanced)**
  - `CopyAsync(string src, string dest, IProgress<double>? progress)`:
    - Must handle directory trees recursively.
    - Must support cancellation tokens.
  - `MoveAsync(string src, string dest)`:
    - Try atomic move first.
    - Fallback to Copy + Delete if crossing volumes/mount points.

---

## Phase 3: Navigation & Intelligence [PENDING]

### 3.1 Path Resolution

- [ ] **PathResolver**:
  - Expansion of tilde (`~`) to user home.
  - Normalization of `..` and `.` for cross-platform stability.
  - Canonical path resolution to prevent symlink loops.

### 3.2 Search Engine

- [ ] **SearchService**:
  - Globbing support (e.g., `**/*.cs`).
  - Asynchronous streaming of results using `IAsyncEnumerable<FileSystemItem>`.

---

## Phase 4: Quality Assurance [ONGOING]

- [ ] **Coverage Requirement**: 100% of `FileOperationService` logic must be covered by `MockFileSystem` tests.
- [ ] **Stress Testing**: Handling deeply nested directories (10+ levels).
- [ ] **Concurrency**: Verification that `CopyAsync` doesn't block the UI thread.
