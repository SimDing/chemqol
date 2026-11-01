-include .env

DOTNET ?= dotnet
GAME_DIR ?= $(HOME)/.steam/steam/steamapps/common/Probably Stolen
MOD_ID ?= chemqol
MODS_DIR ?= $(GAME_DIR)/Mods
MANAGED_DIR = $(GAME_DIR)/Probably Stolen_Data/Managed
PROJECT = ChemQOL.csproj
CONFIGURATION ?= Release
OUTPUT_DIR = bin/$(CONFIGURATION)/netstandard2.1
MOD_DLL_NAME = ChemQOL.dll
MOD_DLL = $(OUTPUT_DIR)/$(MOD_DLL_NAME)

.PHONY: all help check-game build install uninstall clean

all: build

help:
	@printf '%s\n' \
	  'Targets:' \
	  '  make build     Compile the mod' \
	  '  make install   Build and copy the mod into the game Mods folder' \
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
	@for assembly in UnityEngine.dll UnityEngine.CoreModule.dll 0Harmony.dll; do \
	  test -f "$(MANAGED_DIR)/$$assembly" || { \
	    printf 'Required game assembly missing: %s/%s\n' "$(MANAGED_DIR)" "$$assembly" >&2; \
	    exit 1; \
	  }; \
	done

build: check-game
	$(DOTNET) build "$(PROJECT)" --configuration "$(CONFIGURATION)" \
	  -p:GameManagedPath="$(MANAGED_DIR)"

install: build
	install -d "$(MODS_DIR)/$(MOD_ID)"
	install -m 644 "$(MOD_DLL)" "$(MODS_DIR)/$(MOD_ID)/$(MOD_DLL_NAME)"
	install -m 644 manifest.xml "$(MODS_DIR)/$(MOD_ID)/manifest.xml"

uninstall:
	@rm -f "$(MODS_DIR)/$(MOD_ID)/$(MOD_DLL_NAME)" "$(MODS_DIR)/$(MOD_ID)/manifest.xml"
	@rmdir "$(MODS_DIR)/$(MOD_ID)" 2>/dev/null || true

clean:
	$(DOTNET) clean "$(PROJECT)" --configuration "$(CONFIGURATION)"
	rm -rf bin obj