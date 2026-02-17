# Phase 1.5: Observable Pattern Implementation - Current Work

> **Status:** Active
> **Goal:** Implement observable patterns for file operations (State Notification).
> **Definition of Done**: Each step includes implementation feature + passing unit tests.

---

## Step 1: Event Args Models

- [x] **1a. Task**: Create `Models/OperationEventArgs.cs` with event arg classes:
  - `OperationStartedEventArgs(string operationName, string path)`
  - `OperationProgressEventArgs(long bytesProcessed, long totalBytes)`
  - `OperationCompletedEventArgs(string operationName, long totalBytes)`
  - `OperationFailedEventArgs(string operationName, string path, Exception error)`
- [x] **1b. Unit Tests**: `OperationEventArgsTests.cs` — Verify construction, property access, immutability

---

## Step 2: Progress Model

- [x] **2a. Task**: Create `Models/OperationProgress.cs` with record:
  - `OperationProgress(string OperationName, string CurrentPath, long BytesProcessed, long TotalBytes, double PercentComplete)`
- [x] **2b. Unit Tests**: `OperationProgressTests.cs` — Verify record construction, percent calculation accuracy

---

## Step 3: FileOperationService Skeleton (Observable Plumbing)

- [x] **3a. Task**: Refactor `FileOperationService.cs`:
  - Add 4 public events (OperationStarted, OperationProgress, OperationCompleted, OperationFailed)
  - Add method signatures (all async, all with `IProgress<OperationProgress>?` and `CancellationToken`)
  - Add protected helper methods to raise events consistently
  - Add placeholder for `GuardProtectedPath()` (Phase 2)
- [x] **3b. Unit Tests**: `FileOperationServiceObservabilityTests.cs` — Verify events are raised when subscribed, event arg data is correct

---

## Step 4: Implement DeleteAsync

- [x] **4a. Task**: Full `DeleteAsync` implementation (file + recursive directory support)
- [x] **4b. Unit Tests**: `FileOperationServiceDeleteTests.cs` (10-15 test cases):
  - Delete single file
  - Delete directory non-recursive (empty)
  - Delete directory recursive (tree)
  - Cancellation mid-operation
  - File not found handling
  - Event sequence verification
  - Progress reporting accuracy

---

## Step 5: Implement RenameAsync

- [x] **5a. Task**: Full `RenameAsync` implementation (atomic rename, collision detection)
- [x] **5b. Unit Tests**: `FileOperationServiceRenameTests.cs`:
  - Rename file
  - Rename directory
  - Collision handling (throws)
  - Event sequence
  - Cancellation support

---

## Step 6: Implement CopyAsync

- [x] **6a. Task**: Full recursive `CopyAsync` with progress granularity
- [x] **6b. Unit Tests**: `FileOperationServiceCopyTests.cs`:
  - Copy single file
  - Copy directory tree
  - Progress reporting (bytes/percentage)
  - Cancellation mid-copy
  - Overwrite flag behavior
  - Event sequence

---

## Step 7: Implement MoveAsync

- [ ] **7a. Task**: Full `MoveAsync` (atomic first, fallback to Copy+Delete cross-volume)
- [ ] **7b. Unit Tests**: `FileOperationServiceMoveTests.cs`:
  - Move file same volume
  - Move directory same volume
  - Move cross-volume (triggers fallback)
  - Cancellation handling
  - Event sequence
  - Rollback on failure scenarios

---

## Notes

- Each task must have passing tests before moving to the next
- Git commits at checkpoint intervals
- Use `MockFileSystem` for all unit tests
- Follow C# 14 conventions (primary constructors, collection expressions, file-scoped namespaces)
