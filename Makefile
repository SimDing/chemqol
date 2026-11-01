-include .env

DOTNET ?= dotnet
ILSPYCMD ?= $(HOME)/.dotnet/tools/ilspycmd
GAME_DIR ?= $(HOME)/.steam/steam/steamapps/common/Probably Stolen
MOD_ID ?= de.simonding.chemqol
MODS_DIR ?= $(GAME_DIR)/Mods
MANAGED_DIR = $(GAME_DIR)/Probably Stolen_Data/Managed
PROJECT = ChemQOL.csproj
CONFIGURATION ?= Release
OUTPUT_DIR = bin/$(CONFIGURATION)/netstandard2.1
MOD_DLL_NAME = ChemQOL.dll
MOD_DLL = $(OUTPUT_DIR)/$(MOD_DLL_NAME)
MOD_VERSION ?= $(shell awk -F'[<>]' '/<ModVersion>/{print $$3; exit}' manifest.xml)
RELEASE_ARCHIVE ?= dist/ChemQOL-$(MOD_VERSION).zip
DECOMPILE_DIR ?= decompiled/Assembly-CSharp

.PHONY: all help check-game build install release uninstall install-decompiler decompile clean

all: build

help:
	@printf '%s\n' \
	  'Targets:' \
	  '  make build     Compile the mod' \
	  '  make decompile Decompile the game assembly into $(DECOMPILE_DIR)' \
	  '  make install-decompiler Install the ILSpy command-line tool' \
	  '  make install   Build and copy the mod into the game Mods folder' \
	  '  make release   Build a versioned GitHub release ZIP in dist/' \
	  '  make uninstall Remove this mod DLL and manifest from the game folder' \
	  '  make clean     Remove local build output' \
	  '' \
	  'Current values:' \
	  '  GAME_DIR=$(GAME_DIR)' \
	  '  MOD_ID=$(MOD_ID)' \
	  '  DOTNET=$(DOTNET)'

check-game:
	@test -f "$(MANAGED_DIR)/Assembly-CSharp.dll" || { \
	  printf 'Game assemblies not found under: %s\nSet GAME_DIR to the game install folder.\n' "$(MANAGED_DIR)" >&2; \
	  exit 1; \
	}
	@for assembly in UnityEngine.dll UnityEngine.CoreModule.dll UnityEngine.InputLegacyModule.dll 0Harmony.dll; do \
	  test -f "$(MANAGED_DIR)/$$assembly" || { \
	    printf 'Required game assembly missing: %s/%s\n' "$(MANAGED_DIR)" "$$assembly" >&2; \
	    exit 1; \
	  }; \
	done

build: check-game
	$(DOTNET) build "$(PROJECT)" --configuration "$(CONFIGURATION)" \
	  -p:GameManagedPath="$(MANAGED_DIR)"

install: build
	install -d "$(MODS_DIR)/$(MOD_ID)/Localization"
	install -m 644 "$(MOD_DLL)" "$(MODS_DIR)/$(MOD_ID)/$(MOD_DLL_NAME)"
	install -m 644 manifest.xml "$(MODS_DIR)/$(MOD_ID)/manifest.xml"
	install -m 644 Localization/*.csv "$(MODS_DIR)/$(MOD_ID)/Localization/"

release: build
	@command -v zip >/dev/null 2>&1 || { printf 'The zip utility is required for make release.\n' >&2; exit 1; }
	@mkdir -p dist
	@set -eu; staging=$$(mktemp -d); trap 'rm -rf "$$staging"' EXIT; \
	  mod_dir="$$staging/Mods/$(MOD_ID)"; \
	  mkdir -p "$$mod_dir/Localization"; \
	  install -m 644 "$(MOD_DLL)" "$$mod_dir/$(MOD_DLL_NAME)"; \
	  install -m 644 manifest.xml "$$mod_dir/manifest.xml"; \
	  install -m 644 Localization/*.csv "$$mod_dir/Localization/"; \
	  install -m 644 README.md "$$staging/README.md"; \
	  cd "$$staging"; zip -qr "$(abspath $(RELEASE_ARCHIVE))" .
	@printf 'Created %s\n' "$(RELEASE_ARCHIVE)"

install-decompiler:
	$(DOTNET) tool install --global ilspycmd

decompile: check-game
	@command -v "$(ILSPYCMD)" >/dev/null 2>&1 || test -x "$(ILSPYCMD)" || { \
	  printf 'ILSpy command not found. Run: make install-decompiler\n' >&2; \
	  exit 1; \
	}
	mkdir -p "$(DECOMPILE_DIR)"
	"$(ILSPYCMD)" -p -o "$(DECOMPILE_DIR)" "$(MANAGED_DIR)/Assembly-CSharp.dll"

uninstall:
	@rm -f "$(MODS_DIR)/$(MOD_ID)/$(MOD_DLL_NAME)" "$(MODS_DIR)/$(MOD_ID)/manifest.xml"
	@rmdir "$(MODS_DIR)/$(MOD_ID)" 2>/dev/null || true

clean:
	$(DOTNET) clean "$(PROJECT)" --configuration "$(CONFIGURATION)"
	rm -rf bin obj