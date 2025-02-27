﻿using FEZRepacker.Converter.FileSystem;
using FEZRepacker.Converter.XNB;

namespace FEZRepacker.Interface
{
    internal static partial class CommandLine
    {
        public enum UnpackingMode
        {
            Raw,
            DecompressedXNB,
            Converted
        }

        private static void ShowHelpFor(Command command)
        {
            string allNames = command.Name;
            if (command.Aliases.Length > 0)
            {
                allNames = $"[{command.Name}, {String.Join(", ", command.Aliases)}]";
            }

            string argsStr = String.Join(" ", command.Arguments.Select(arg => arg.Optional ? $"<{arg.Name}>" : $"[{arg.Name}]"));

            Console.WriteLine($"Usage: FEZRepacker.exe {allNames} {argsStr}");
            Console.WriteLine($"Description: {command.HelpText}");
        }

        private static void CommandHelp(string[] args)
        {
            if (args.Length > 0)
            {
                if (!FindCommand(args[0], out Command command))
                {
                    Console.WriteLine($"Unknown command \"{args[0]}\".");
                    Console.WriteLine($"Type \"FEZRepacker.exe --help\" for a list of commands.");
                    return;
                }
                ShowHelpFor(command);
                return;
            }

            foreach (var command in Commands)
            {
                ShowHelpFor(command);
                Console.WriteLine();
            }
        }

        public static void UnpackGameAssets(string contentPath, string outputDir, UnpackingMode unpackingMode)
        {
            var packagePaths = new string[] { "Essentials.pak", "Music.pak", "Other.pak", "Updates.pak" }
                .Select(path => Path.Combine(contentPath, path)).ToArray();

            foreach (var packagePath in packagePaths)
            {
                if (!File.Exists(packagePath))
                {
                    throw new Exception($"Given directory is not FEZ's Content directory (missing {Path.GetFileName(packagePath)}).");
                }
            }

            foreach (var packagePath in packagePaths)
            {
                var actualOutputDir = outputDir;
                if (packagePath.EndsWith("Music.pak"))
                {
                    actualOutputDir = Path.Combine(outputDir, "music");
                }
                UnpackPackage(packagePath, actualOutputDir, unpackingMode);
            }
        }

        public static void UnpackPackage(string pakPath, string outputDir, UnpackingMode mode)
        {
            if (Path.GetExtension(pakPath) != ".pak")
            {
                throw new Exception("A path must lead to a .PAK file.");
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            using var pakStream = File.OpenRead(pakPath);
            var pak = PakContainer.Read(pakStream);

            Console.WriteLine($"Unpacking archive {pakPath} containing {pak.Count} files...");

            int filesDone = 0;
            foreach (var pakFile in pak)
            {
                var extension = pakFile.GetExtensionFromHeaderOrDefault();

                Console.WriteLine($"({filesDone + 1}/{pak.Count}) {pakFile.Path} ({extension} file, size: {pakFile.Size} bytes)");

                try
                {
                    using var fileStream = pakFile.Open();

                    var outputBundle = FileBundle.Single(fileStream, ".xnb");

                    if (mode == UnpackingMode.DecompressedXNB)
                    {
                        outputBundle = FileBundle.Single(XnbCompressor.Decompress(fileStream), ".xnb");
                    }
                    else if (mode == UnpackingMode.Converted)
                    {
                        var converter = new XnbConverter();
                        outputBundle = converter.Convert(fileStream);
                        var formatName = converter.HeaderValid ? converter.FileType.Name.Replace("Reader", "") : "";
                        if (converter.Converted)
                        {
                            extension = converter.FormatConverter!.FileFormat;
                            var storageTypeName = (outputBundle.Count > 1 ? "bundle" : "file");
                            Console.WriteLine($"  Format {formatName} converted into {extension} {storageTypeName}.");
                        }
                        else
                        {
                            if (converter.HeaderValid)
                            {
                                Console.WriteLine($"  Unknown format {formatName} - saving as XNB asset.");
                            }
                            else
                            {
                                Console.WriteLine($"  Not a valid XNB file - saving with detected extension ({extension}).");
                            }

                        }
                    }

                    outputBundle.BundlePath = Path.Combine(outputDir, pakFile.Path + extension);
                    var outputDirectory = Path.GetDirectoryName(outputBundle.BundlePath) ?? "";
                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    foreach(var outputFile in outputBundle)
                    {
                        using var fileOutputStream = File.Open(outputBundle.BundlePath + outputFile.Extension, FileMode.Create);
                        outputFile.Data.CopyTo(fileOutputStream);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to unpack {pakFile.Path} - {ex.Message}");
                }
                filesDone++;
            }
        }

        public static void ListPackageContent(string pakPath)
        {
            if (Path.GetExtension(pakPath) != ".pak")
            {
                throw new Exception("A path must lead to a .PAK file.");
            }

            using var pakStream = File.OpenRead(pakPath);
            var pak = PakContainer.Read(pakStream);

            Console.WriteLine($"PAK package \"{pakPath}\" with {pak.Count} files.");
            Console.WriteLine();

            foreach (var item in pak)
            {
                var extension = item.GetExtensionFromHeaderOrDefault("unknown");
                Console.WriteLine($"{item.Path} ({extension} file, size: {item.Size} bytes)");
            }
        }

        public static void ConvertFromXNB(string inputPath, string outputPath)
        {
            Console.WriteLine("Feature currently not supported.");
        }

        public static void ConvertIntoXNB(string inputPath, string outputPath)
        {
            Console.WriteLine("Feature currently not supported.");
        }

        public static void AddToPackage(string inputPath, string outputPackagePath, string includePackagePath)
        {
            // prepare package
            PakContainer pak = new PakContainer();

            if (File.Exists(includePackagePath))
            {
                if (Path.GetExtension(includePackagePath) != ".pak")
                {
                    throw new Exception("Included package path must lead to a .PAK file.");
                }
                using var includeStream = File.OpenRead(includePackagePath);
                pak = PakContainer.Read(includeStream);
                Console.WriteLine($"Using {includePackagePath} as a base for package creation.");
            }

            // get a list of files to load
            if (!Directory.Exists(inputPath))
            {
                throw new Exception("Given input directory does not exist.");
            }

            string[] fileNamesToAdd = Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories);
            Console.WriteLine($"Found {fileNamesToAdd.Length} files.");

            // load and transform file list into bundles
            var fileList = new Dictionary<string, Stream>();
            foreach (var filePath in fileNamesToAdd)
            {
                var relativePath = Path.GetRelativePath(inputPath, filePath).Replace("/", "\\").ToLower();
                fileList[relativePath] = File.OpenRead(filePath);
            }
            var fileBundlesToAdd = FileBundle.BundleFiles(fileList);


            var assetsToAdd = new Dictionary<(string Path, string Extension), Stream>();

            // convert
            Console.WriteLine($"Converting {fileBundlesToAdd.Count()} assets...");
            var filesDone = 0;
            foreach (var fileBundle in fileBundlesToAdd)
            {
                Console.WriteLine($"({filesDone + 1}/{fileBundlesToAdd.Count}) {fileBundle.BundlePath}");

                try
                {
                    var deconverter = new XnbDeconverter();

                    var deconverterStream = deconverter.Deconvert(fileBundle);

                    if (deconverter.Converted)
                    {
                        Console.WriteLine($"  Format {fileBundle.MainExtension} deconverted into {deconverter.FormatConverter!.FormatName} XNB asset.");
                        assetsToAdd.Add((fileBundle.BundlePath, ".xnb"), deconverterStream);
                    }
                    else
                    {
                        Console.WriteLine($"  Format {fileBundle.MainExtension} doesn't have a converter - packing asset as raw files.");

                        foreach(var file in fileBundle)
                        {
                            file.Data.Seek(0, SeekOrigin.Begin);
                            var ext = fileBundle.MainExtension + file.Extension;
                            assetsToAdd.Add((fileBundle.BundlePath, ext), file.Data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to convert asset {fileBundle.BundlePath} - {ex.Message}");
                }
                filesDone++;
            }

            // add to pak
            Console.WriteLine($"Packing {assetsToAdd.Count()} assets into {outputPackagePath}...");

            foreach(var assetRecord in assetsToAdd)
            {
                var assetPath = assetRecord.Key.Path;
                var assetExtension = assetRecord.Key.Extension;
                var asset = assetRecord.Value;

                int removed = pak.RemoveAll(file => file.Path == assetPath && file.GetExtensionFromHeaderOrDefault() == assetExtension);
                if (removed > 0)
                {
                    Console.WriteLine($"File {assetPath} replaces {removed} file{(removed > 1 ? "s" : "")} that has been in the package already.");
                }

                pak.Add(PakFile.Read(assetPath, asset));
            }

            // save package
            using var fileOutputStream = File.Open(outputPackagePath, FileMode.Create);
            pak.Save(fileOutputStream);
        }
    }
}
