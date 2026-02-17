# Phase 3: Navigation & Intelligence - Current Work

> **Status:** In Progress
> **Goal:** Build path resolution and search capabilities for navigation intelligence.
> **Definition of Done**: Core services implemented + passing unit tests + documentation updates.

---

## Overview

Phase 3 focuses on navigation and search intelligence in the Core library:

1. **Path Resolution** for cross-platform stability and safety
2. **Search Engine** for async discovery and globbing
3. **Stress Testing** for deeply nested directory handling (moved from Phase 4)

---

## Step 1: Path Resolution

### 1a. Create PathResolver

- [x] **Task**: Create `Services/PathResolver.cs`
  - Expand tilde (`~`) to user home
  - Normalize `..` and `.` segments
  - Resolve canonical paths safely
  - Detect and prevent symlink loops (use `CircularSymlinkException`)

### 1b. PathResolver Tests

- [x] **Task**: Create `PathResolverTests.cs`
  - Tilde expansion on Linux/macOS
  - Windows drive and UNC handling
  - Normalization of `..` and `.`
  - Circular symlink detection

---

## Step 2: Search Engine

### 2a. Create SearchService

- [x] **Task**: Create `Services/SearchService.cs`
  - Globbing support (e.g., `**/*.cs`)
  - Async streaming via `IAsyncEnumerable<FileSystemItem>`
  - Cancellation support via `CancellationToken`

### 2b. SearchService Tests

- [x] **Task**: Create `SearchServiceTests.cs`
  - Globbing match coverage
  - Async streaming behavior
  - Cancellation behavior

---

## Step 3: Stress Testing (Moved from Phase 4)

### 3a. Deep Directory Stress Test

- [x] **Task**: Add stress test for deeply nested directories (10+ levels)
  - Validate traversal does not overflow or hang
  - Cover both PathResolver and SearchService behavior
  - Track runtime and performance expectations

---

## Notes

- **Dependencies**: None
- **Testing Strategy**: New test suite for resolver and search
- **Architecture**: Core only, no UI dependencies
- **Performance**: Favor async streaming and cancellation
