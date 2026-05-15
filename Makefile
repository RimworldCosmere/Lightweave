# Lightweave - Development Makefile
#
# Usage:
#   make help                    - Show available commands
#   make build                   - dotnet build Lightweave.sln
#   make build-assets            - Build Unity AssetBundles for current platform
#   make build-assets-all        - Build for Windows, macOS, Linux
#   make build-assets-all-verbose - Same, with verbose output (CI)
#   make restore                 - dotnet restore
#   make clean                   - Remove build outputs

.PHONY: help build build-assets build-assets-verbose build-assets-all build-assets-all-verbose restore clean

SHELL := /bin/bash
.SHELLFLAGS := -O xpg_echo -c

.DEFAULT_GOAL := help

GREEN := \033[0;32m
BLUE := \033[0;34m
YELLOW := \033[0;33m
NC := \033[0m

help: ## Display this help message
	@echo "$(BLUE)Lightweave Development Makefile$(NC)"
	@echo ""
	@awk 'BEGIN {FS = ":.*##"; printf "Usage: make $(YELLOW)<target>$(NC)\n\n"} /^[a-zA-Z_0-9-]+:.*?##/ { printf "  $(GREEN)%-30s$(NC) %s\n", $$1, $$2 }' $(MAKEFILE_LIST)

build: ## dotnet build the solution
	@echo "$(BLUE)Building Lightweave.sln...$(NC)"
	@dotnet build Lightweave.sln

restore: ## dotnet restore
	@echo "$(BLUE)Restoring NuGet packages...$(NC)"
	@dotnet restore Lightweave.sln

clean: ## Remove bin/obj/AssetBundles/Assemblies
	@echo "$(YELLOW)Cleaning build outputs...$(NC)"
	@find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
	@rm -rf Assemblies AssetBundles

build-assets: ## Build Unity AssetBundles for current platform
	@echo "$(BLUE)Building Unity AssetBundles...$(NC)"
	@dotnet script ./scripts/build-assets.csx

build-assets-verbose: ## Build Unity AssetBundles with verbose output
	@echo "$(BLUE)Building Unity AssetBundles (verbose)...$(NC)"
	@dotnet script ./scripts/build-assets.csx -- --verbose

build-assets-all: ## Build Unity AssetBundles for all platforms
	@echo "$(BLUE)Building Unity AssetBundles for all platforms...$(NC)"
	@echo "  Building for Windows..."
	UNITY_BUILD_TARGET=windows dotnet script ./scripts/build-assets.csx
	@echo "  Building for macOS..."
	UNITY_BUILD_TARGET=mac dotnet script ./scripts/build-assets.csx
	@echo "  Building for Linux..."
	UNITY_BUILD_TARGET=linux dotnet script ./scripts/build-assets.csx
	@echo "$(GREEN)All platform bundles built!$(NC)"

build-assets-all-verbose: ## Build Unity AssetBundles for all platforms (verbose, CI)
	@echo "$(BLUE)Building Unity AssetBundles for all platforms (verbose)...$(NC)"
	@echo "  Building for Windows..."
	UNITY_BUILD_TARGET=windows dotnet script ./scripts/build-assets.csx -- --verbose
	@echo "  Building for macOS..."
	UNITY_BUILD_TARGET=mac dotnet script ./scripts/build-assets.csx -- --verbose
	@echo "  Building for Linux..."
	UNITY_BUILD_TARGET=linux dotnet script ./scripts/build-assets.csx -- --verbose
	@echo "$(GREEN)All platform bundles built!$(NC)"
