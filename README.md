# ChemQOL

ChemQOL is a quality-of-life mod for chemical workflows in **Probably Stolen**. It focuses on chemistry machines and handling chemical liquids, with a couple of liquid-handling conveniences that can also be used with water.

## Features

- Safety Cut-Off: Turn off the heating plate automatically when adding a liquid container.
- Automatically select the chem finisher recipe that best matches the inserted container's active ingredient.
- Closes the chem finisher recipe selector after choosing a recipe.
- Double-click a liquid container to select it, hover over another compatible container, then scroll down to pour a small amount (10% of max capacity) into it.
- Double-click a pipette to select it, hover over another compatible container, then scroll up to draw a small amount of liquid from it.

## Configuration

Enable the mod, then open its configuration panel from the Mods menu. Gameplay options are enabled by default; every feature can be deactivated individually.

### Translations

Option labels are localized in English and German.

Machine Translations in French, Russian, Chinese, Japanese, Spanish, Brazilian Portuguese, Korean, Italian, and Polish.


## Build and Install from Source

On Linux, install the .NET SDK and GNU Make, then configure the game installation directory.

If you have a custom steam install location:

```sh
cp .env.example .env
```

Edit `.env` and set `GAME_DIR` to the directory containing `Probably Stolen_Data/Managed`. For example:

```make
GAME_DIR=/path/to/SteamLibrary/steamapps/common/Probably Stolen
```

Build and install it into the game's `Mods/de.simonding.chemqol` directory:

```sh
make install
```

## 简体中文（机器翻译）

ChemQOL 是一款为 **Probably Stolen** 中化学工作流程提供便利功能的模组。它主要改进化学设备和化学液体的处理，同时也提供一些可用于水的液体操作便利功能。

## 功能

- 安全断电：放入液体容器时自动关闭加热板。
- 放入容器后，自动选择与其中主要活性成分最匹配的化学制剂成型机配方。
- 选择配方后自动关闭化学制剂成型机的配方选择器。
- 双击液体容器以选中它，将鼠标悬停在另一个兼容容器上，然后向下滚动，将少量液体（最大容量的 10%）倒入其中。
- 双击移液器以选中它，将鼠标悬停在另一个兼容容器上，然后向上滚动，从中吸取少量液体。

## 配置

启用模组后，在“模组”菜单中打开它的配置面板。游戏功能默认全部启用；每项功能都可以单独关闭。

### 翻译

选项标签提供英语和德语翻译。

设备相关文本提供法语、俄语、中文、日语、西班牙语、巴西葡萄牙语、韩语、意大利语和波兰语翻译。

## 从源代码构建和安装

在 Linux 上安装 .NET SDK 和 GNU Make，然后配置游戏的安装目录。

如果 Steam 安装在自定义位置：

```sh
cp .env.example .env
```

编辑 `.env`，将 `GAME_DIR` 设置为包含 `Probably Stolen_Data/Managed` 的目录。例如：

```make
GAME_DIR=/path/to/SteamLibrary/steamapps/common/Probably Stolen
```

将模组构建并安装到游戏的 `Mods/de.simonding.chemqol` 目录：

```sh
make install
```
