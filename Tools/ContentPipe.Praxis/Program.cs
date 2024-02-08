using ContentPipe.Core;
using ContentPipe.Extras;
using ContentPipe.FNA;
using ContentPipe.Praxis;

public static class Program
{
    public static int Main()
    {
        var builder = new Builder();

        var compressor = new CompressonatorTextureProcessor("../Tools/binaries/win-x64/compressonator/compressonatorcli.exe",
            "../Tools/binaries/linux-x64/compressonator/compressonatorcli");

        var gltf = new GltfProcessor(compressor);

        builder.AddRule("*.fx", new EFBShaderProcessor("../Tools/binaries/efb.exe", []));
        builder.AddRule("*.gltf", gltf);
        builder.AddRule("*.glb", gltf);

        builder.AddRule("*.dds", new CopyProcessor());
        builder.AddRule("*.ktx", new CopyProcessor());
        builder.AddRule("*.png", compressor);
        builder.AddRule("*.jpg", compressor);
        builder.AddRule("*.jpeg", compressor);
        builder.AddRule("*.tga", compressor);
        builder.AddRule("*.bmp", compressor);
        builder.AddRule("*.wav", new CopyProcessor());
        builder.AddRule("*.ogg", new CopyProcessor());
        builder.AddRule("*.ogv", new CopyProcessor());
        builder.AddRule("*.ogv", new CopyProcessor());
        builder.AddRule("*.av1", new CopyProcessor());
        builder.AddRule("*.raw", new CopyProcessor());
        builder.AddRule("*.csv", new CopyProcessor());
        builder.AddRule("*.txt", new CopyProcessor());
        builder.AddRule("*.yaml", new CopyProcessor());
        builder.AddRule("*.json", new CopyProcessor());
        builder.AddRule("*.xml", new CopyProcessor());
        builder.AddRule("*.ss", new CopyProcessor());
        builder.AddRule("*.ttf", new CopyProcessor());
        builder.AddRule("*.owbproject", new CopyProcessor());
        builder.AddRule("*.owblevel", new CopyProcessor());

        return ContentPipeAPI.Build(builder);
    }
}
