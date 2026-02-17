# Phase 2: Exception & Safety Layer - Current Work

> **Status:** In Progress
> **Goal:** Refine exception handling and safety mechanisms for production-ready file operations.
> **Definition of Done**: Custom exceptions implemented + passing unit tests + documentation.

---

## Overview

Phase 1 and 1.5 established the core FileOperationService with full CRUD operations, observable patterns, and cancellation support. Phase 2 focuses on:

1. **Custom Exception Types** for better error handling (enables frontends to provide better UX)
2. **Validation Layer** for input validation before operations
3. **Atomic Operation Patterns** for safer file operations

---

## Step 1: Custom Exception Types

### 1a. Create ProtectedPathException

- [x] **Task**: Create `Exceptions/ProtectedPathException.cs`
  - Custom exception class with primary constructor
  - Properties: `string Path`, `string OperationName`
  - Inherits from `UnauthorizedAccessException`
  - Include helpful message with protected path information

### 1b. Update GuardProtectedPath

- [x] **Task**: Refactor `FileOperationService.GuardProtectedPath()`
  - Throw `ProtectedPathException` instead of generic `UnauthorizedAccessException`
  - Include operation name in exception for better error context

### 1c. Update Tests

- [x] **Task**: Update all guard tests to expect `ProtectedPathException`
  - FileOperationServiceGuardTests.cs (13 tests)
  - FileOperationServiceCopyTests.cs (2 guard tests)
  - FileOperationServiceMoveTests.cs (2 guard tests)

---

## Step 2: File Operation Exceptions

### 2a. Create FileOperationException Base Class

- [ ] **Task**: Create `Exceptions/FileOperationException.cs`
  - Base exception for all file operation errors
  - Properties: `string OperationName`, `string Path`, `Exception? InnerException`
  - Abstract or base class with common functionality

### 2b. Specialized Exceptions

- [ ] **Task**: Create specific exception types:
  - `FileNotFoundException` - Already exists in BCL, use as-is
  - `DirectoryNotFoundException` - Already exists in BCL, use as-is
  - `IOException` - Already exists in BCL for collisions
  - Consider: `CircularSymlinkException` for future path resolution
  - Consider: `InsufficientPermissionsException` for permission errors

---

## Step 3: Input Validation Layer

### 3a. Path Validation

- [ ] **Task**: Create path validation helper methods
  - `ValidatePath(string path)` - Check for null, empty, invalid characters
  - `ValidateDestinationPath(string src, string dest)` - Prevent source == destination
  - `ValidatePathExists(string path)` - Check existence before operations

### 3b. Precondition Guards

- [ ] **Task**: Add validation guards to all public methods
  - ArgumentNullException for null arguments
  - ArgumentException for invalid paths
  - Call validation before GuardProtectedPath

### 3c. Validation Tests

- [ ] **Task**: Create `FileOperationServiceValidationTests.cs`
  - Test null/empty path handling
  - Test invalid character handling
  - Test source equals destination scenarios
  - Test path length limits (Windows MAX_PATH, Unix limits)

---

## Step 4: Atomic Operation Safety

### 4a. Transaction-like Copy

- [ ] **Task**: Implement safer copy pattern
  - Copy to temporary location first (e.g., `.tmp` suffix)
  - Verify copy integrity (size, maybe checksum)
  - Atomic rename from temp to final destination
  - Cleanup temp files on failure

### 4b. Move Rollback

- [ ] **Task**: Implement rollback for failed moves
  - If Copy+Delete fallback fails during delete, log but don't throw
  - Consider: Track partial operations for recovery
  - Add `CleanupAsync` method for orphaned temp files

### 4c. Safety Tests

- [ ] **Task**: Create `FileOperationServiceSafetyTests.cs`
  - Test copy failure midway (disk full simulation)
  - Test move rollback scenarios
  - Test cleanup after failures
  - Test concurrent operations (if supported)

---

## Step 5: Documentation & Polish

### 5a. XML Documentation

- [ ] **Task**: Add comprehensive XML docs to exception classes
  - Include usage examples in remarks
  - Document when each exception is thrown
  - Reference Phase 3 considerations (symlinks, etc.)

### 5b. Usage Documentation

- [ ] **Task**: Update README with exception handling guidance
  - Common exceptions frontends should catch
  - Error handling patterns for TUI/GUI consumer implementation
  - Logging recommendations for core library consumers

---

## Notes

- **Phase 1/1.5 Complete**: 126 passing tests, all core operations functional
- **Current Focus**: Custom exceptions for better error handling
- **Dependencies**: None - Phase 2 work is independent
- **Testing Strategy**: Update existing tests + add new validation/safety tests
- **Target**: Add ~20-30 new tests for validation and safety scenarios
- **Architectural Principle**: Core library provides NO UX - only file operation logic, exceptions, and observables that frontends (TUI/GUI) consume

---

## Success Criteria

Phase 2 is complete when:

1. ✅ Custom `ProtectedPathException` implemented and integrated
2. ✅ All 126+ existing tests still pass
3. ✅ Input validation for all public FileOperationService methods
4. ✅ Validation tests cover null, empty, invalid path scenarios
5. ✅ XML documentation for all custom exceptions
6. ✅ Updated CORE_ROADMAP.md marking Phase 2 complete

**Estimated Test Count After Phase 2**: 150+ tests

---

## Future Considerations (Phase 3+)

- Path normalization and symlink resolution
- Async enumerable search with glob patterns
- Performance optimizations for large directory trees
- Differential copy (only copy changed files)
