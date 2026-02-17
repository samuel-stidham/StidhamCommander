# StidhamCommander: Core v1.0 Roadmap & Technical Specs

> **Status:** Phase 2 (In Progress - Step 1 Complete)
> **Goal:** Implement a testable, cross-platform Write Engine using .NET 10/C# 14.

---

## Phase 1: Foundation [COMPLETE ✅]

- [x] Solution Scaffolding (Clean Architecture structure)
- [x] `FileDiscoveryService`: Ordered directory listing.
- [x] `SizeFormatter`: C# 14 extension members for human-readable bytes.
- [x] Environment: `Makefile`, `.editorconfig`, and `.config/ai/` bootstrap.

## Phase 1.5: Frontend-Ready Abstractions [COMPLETE ✅]

- [x] **State Notification**: Implemented events (`OperationStarted`, `OperationProgress`, `OperationCompleted`, `OperationFailed`) and `IProgress<OperationProgress>` patterns for UI reactivity.
- [x] **Cancellation Support**: All long-running operations in `FileOperationService` accept and respect `CancellationToken`. Verified with 10 comprehensive cancellation tests.
- [x] **TUI/GUI Compatibility**: All Core models are records/classes that are serializable and observable.
- [x] **Test Coverage**: 126 passing unit tests with 100% MockFileSystem coverage.

---

## Phase 2: File Operations (Write Engine) [IN PROGRESS]

### 2.1 Exception & Safety Layer [COMPLETE ✅]

- [x] **Cross-Platform Guard**: `GuardProtectedPath()` protects system roots (`/`, `C:\`), system folders (`/etc`, `/bin`), and user home roots across Linux, Windows, and macOS.
- [x] **ProtectedPathException**: Custom exception with `Path` and `OperationName` properties, inherits from `UnauthorizedAccessException` for better error context.

### 2.2 FileOperationService [COMPLETE ✅]

Implemented using `IFileSystem` abstractions:

- [x] **Basic Operations**
  - `DeleteAsync(string path, bool recursive)`: Supports files/folders with Guard check, progress reporting, and cancellation.
  - `RenameAsync(string path, string newName)`: Atomic same-volume rename with collision detection.
- [x] **Recursive Operations (Advanced)**
  - `CopyAsync(string src, string dest, IProgress<OperationProgress>? progress)`:
    - Handles directory trees recursively with progress reporting.
    - Full cancellation support with mid-operation checks.
  - `MoveAsync(string src, string dest)`:
    - Tries atomic move first.
    - Falls back to Copy + Delete for cross-volume moves.
    - Respects cancellation throughout fallback operation.

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

- [x] **Coverage Requirement**: 100% of `FileOperationService` logic covered by `MockFileSystem` tests. 126 tests across 9 test files.
- [x] **Cancellation Testing**: Comprehensive cancellation tests including immediate cancellation, mid-operation cancellation, and timeout scenarios.
- [ ] **Stress Testing**: Handling deeply nested directories (10+ levels).
- [x] **Concurrency**: All operations use `Task.Run()` and accept `CancellationToken` to avoid blocking.

---

## Summary: Phase 1 & 1.5 Complete

**Implemented:**

- FileDiscoveryService for directory navigation
- FileOperationService with DeleteAsync, RenameAsync, CopyAsync, MoveAsync
- Full observable pattern support (events + `IProgress<T>`)
- Cross-platform protected path guarding
- Comprehensive cancellation support
- 126 passing unit tests

**Next Phase:** Exception refinements and custom exception types (ProtectedPathException), then Phase 3 navigation intelligence.
