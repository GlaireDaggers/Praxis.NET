using ContentPipe.Core;
using ContentPipe.Extras;
using ContentPipe.FNA;
using ContentPipe.Praxis;

public static class Program
{
    public static int Main()
    {
        var builder = new Builder();

        builder.AddRule("*.fx", new EFBShaderProcessor("../Tools/binaries/efb.exe", []));
        builder.AddRule("*.gltf", new GltfProcessor());
        builder.AddRule("*.glb", new GltfProcessor());

        builder.AddRule("*.dds", new CopyProcessor());
        builder.AddRule("*.ktx", new CopyProcessor());
        builder.AddRule("*.png", new CopyProcessor());
        builder.AddRule("*.jpg", new CopyProcessor());
        builder.AddRule("*.jpeg", new CopyProcessor());
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
        builder.AddRule("*.owbproject", new CopyProcessor());
        builder.AddRule("*.owblevel", new CopyProcessor());

        return ContentPipeAPI.Build(builder);
    }
}