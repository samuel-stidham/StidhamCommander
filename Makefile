.PHONY: help build test tree copy-tree clean

# Default goal: show help
help:
	@echo "StidhamCommander Developer Tools"
	@echo "--------------------------------"
	@echo "make build      - Restore and build the solution"
	@echo "make test       - Run all xUnit tests"
	@echo "make tree       - Display clean project structure"
	@echo "make copy-tree  - Copy clean project structure to clipboard"
	@echo "make clean      - Remove bin and obj directories"

build:
	dotnet build

test:
	dotnet test

tree:
	@tree -I "bin|obj"

copy-tree:
	@tree -I "bin|obj" | xclip -selection clipboard
	@echo "✓ Project tree copied to clipboard."

clean:
	@find . -type d -name "bin" -exec rm -rf {} +
	@find . -type d -name "obj" -exec rm -rf {} +
	@echo "✓ Cleaned build artifacts."
