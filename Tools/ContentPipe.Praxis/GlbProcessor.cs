namespace ContentPipe.Praxis;

using ContentPipe.Core;
using SharpGLTF.Schema2;

public class GlbProcessor : SingleAssetProcessor
{
    protected override string GetOutputExtension(string inputExtension)
    {
        return "glb";
    }

    protected override void Process(BuildInputFile inputFile, string outputPath, BuildOptions options)
    {
        var model = ModelRoot.Load(inputFile.filepath);
        model.SaveGLB(outputPath);
    }
}
