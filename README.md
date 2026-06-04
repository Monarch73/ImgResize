# ImgResize

A command-line utility built as a .NET tool to batch process, resize, and compress JPEG images.

## Features

- **Recursive Processing:** Scans a specified input directory (and all subdirectories) for `.jpg` and `.jpeg` files.
- **Auto-Resizing:** Ensures that no image exceeds 4 Megapixels (4,000,000 pixels). If an image is larger, it's proportionally scaled down.
- **Smart Compression:** Uses a binary search algorithm to find the highest possible JPEG quality while keeping the final file size at or below 1.4 MB.
- **Preserves Structure:** Maintains the exact folder hierarchy from the input directory when saving to the output directory.

## Installation

Because this utility is packaged as a .NET tool, you can easily install it globally on your machine using the .NET CLI.

### Installing from Source

1. Clone or download this repository.
2. Open a terminal and navigate to the project directory containing `ImgResize.csproj`.
3. Build the tool package by running:
   ```shell
   dotnet pack
   ```
4. Install the tool globally using the locally generated package:
   ```shell
   dotnet tool install --global --add-source ./nupkg ImgResize
   ```

*(Note: If you already have it installed and want to update to a newer build, use `dotnet tool update` instead of `install`.)*

## Usage

Once installed, you can run the tool from anywhere in your terminal using the `imgresize` command:

```shell
imgresize <input directory> <output directory>
```

### Example

```shell
imgresize C:\Photos\Vacation C:\Photos\CompressedVacation
```

The tool will process all JPEGs found in `C:\Photos\Vacation`, resize and compress them to fit the constraints, and save them into `C:\Photos\CompressedVacation` using the same sub-folder structure.
