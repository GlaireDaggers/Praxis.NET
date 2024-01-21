namespace ContentPipe.Praxis;

using System.Diagnostics;

using ContentPipe.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// A processor for shader files which invokes an EffectBuild executable to compile HLSL shaders into DXBC for FNA games
/// </summary>
public class EFBShaderProcessor : SingleAssetProcessor<EFBShaderProcessor.ShaderMetadata>
{
    public enum ShaderOptimizationLevel
    {
        Od,
        O0,
        O1,
        O2,
        O3,
    }

    public enum ShaderMatrixPacking
    {
        Default,
        ColumnOrder,
        RowOrder,
    }

    public struct ShaderMetadata
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ShaderOptimizationLevel optLevel;

        [JsonConverter(typeof(StringEnumConverter))]
        public ShaderMatrixPacking matrixPacking;
    }

    private readonly string _efbPath;
    private readonly string[] _includePaths;
    private readonly bool _disableValidation;
    private readonly bool _treatWarningsAsErrors;

    public EFBShaderProcessor(string efbPath, string[] includePaths, bool disableValidation = false, bool treatWarningsAsErrors = false)
    {
        _efbPath = efbPath;
        _includePaths = includePaths;
        _disableValidation = disableValidation;
        _treatWarningsAsErrors = treatWarningsAsErrors;
    }

    protected override ShaderMetadata DefaultMetadata => new ShaderMetadata
    {
        optLevel = ShaderOptimizationLevel.O1,
        matrixPacking = ShaderMatrixPacking.ColumnOrder,
    };

    protected override string GetOutputExtension(string inFileExtension)
    {
        return "fxo";
    }

    protected override void Process(BuildInputFile<ShaderMetadata> inputFile, string outputPath, BuildOptions options)
    {
        string efbArgs = "";

        if (_includePaths != null)
        {
            foreach (string includePath in _includePaths)
            {
                efbArgs += $" /I \"{includePath}\"";
            }
        }

        if (_disableValidation)
        {
            efbArgs += " /Vd";
        }

        if (_treatWarningsAsErrors)
        {
            efbArgs += " /WX";
        }

        if (inputFile.metadata.matrixPacking == ShaderMatrixPacking.ColumnOrder)
        {
            efbArgs += " /Zpc";
        }
        else if (inputFile.metadata.matrixPacking == ShaderMatrixPacking.RowOrder)
        {
            efbArgs += " /Zpr";
        }

        efbArgs += $" /{inputFile.metadata.optLevel}";

        // invoke EFB
        string cmd = $"{efbArgs} {inputFile.filepath} {outputPath}";

        Process process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo();

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = _efbPath;
            startInfo.Arguments = cmd;
        }
        else
        {
            startInfo.FileName = "wine";
            startInfo.Arguments = $"\"{_efbPath}\" {cmd}";
        }

        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;

        process.StartInfo = startInfo;
        process.Start();

        string stdOut = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception("Shader compilation failed: " + stdOut);
        }
    }
}