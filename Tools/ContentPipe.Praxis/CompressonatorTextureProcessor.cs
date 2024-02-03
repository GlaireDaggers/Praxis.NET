using System.Diagnostics;
using ContentPipe.Core;

namespace ContentPipe.Praxis;

public class CompressonatorTextureProcessor : SingleAssetProcessor<CompressonatorTextureProcessor.TextureMetadata>
{
    public enum TextureFormat
    {
        ARGB_8888,
        ARGB_16F,
        ARGB_32F,
        BC1,
        BC3,
        BC4,
        BC5
    }

    public struct TextureMetadata
    {
        public TextureFormat format;
        public int alphaThreshold;
        public bool mipmap;
    }

    private readonly string _windowsPath;
    private readonly string _linuxPath;

    public CompressonatorTextureProcessor(string windowsPath, string linuxPath)
    {
        _windowsPath = windowsPath;
        _linuxPath = linuxPath;
    }

    protected override TextureMetadata DefaultMetadata => new TextureMetadata
    {
        format = TextureFormat.BC3,
        alphaThreshold = 0,
        mipmap = true
    };

    protected override string GetOutputExtension(string inFileExtension)
    {
        return "dds";
    }

    public void ConvertTexture(string inputPath, TextureMetadata options, string outputPath)
    {
        string compressonatorArgs = "";

        compressonatorArgs += $"-fd {options.format}";

        if (options.alphaThreshold > 0)
        {
            compressonatorArgs += $" -AlphaThreshold {options.alphaThreshold}";
        }

        if (options.mipmap)
        {
            compressonatorArgs += $" -mipsize 1";
        }
        else
        {
            compressonatorArgs += $" -nomipmap";
        }

        // invoke Compressonator
        string cmd = $"{compressonatorArgs} {inputPath} {outputPath}";

        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo();

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = _windowsPath;
        }
        else
        {
            // compressonator is a shell script on linux, not an executable
            startInfo.FileName = "/bin/bash";
            cmd = $"{_linuxPath} {cmd}";
        }
        
        startInfo.Arguments = cmd;

        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;

        process.StartInfo = startInfo;
        process.Start();

        string stdOut = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception("Texture processing failed: " + stdOut);
        }
    }

    protected override void Process(BuildInputFile<TextureMetadata> inputFile, string outputPath, BuildOptions options)
    {
        ConvertTexture(inputFile.filepath, inputFile.metadata, outputPath);
    }
}
