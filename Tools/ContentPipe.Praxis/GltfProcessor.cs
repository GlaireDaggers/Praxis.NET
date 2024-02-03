namespace ContentPipe.Praxis;

using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContentPipe.Core;
using SharpGLTF.Schema2;

public class GltfProcessor : SingleAssetProcessor<GltfProcessor.Data>
{
    public struct Data
    {
        public Dictionary<string, string>? materials;
    }

    private class PraxisMaterialData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "opaque";

        [JsonPropertyName("effect")]
        public string? Effect { get; set; }

        [JsonPropertyName("effectTechnique")]
        public string? EffectTechnique { get; set; }

        [JsonPropertyName("effectTechniqueSkinned")]
        public string? EffectTechniqueSkinned { get; set; }

        [JsonPropertyName("colorBlendFunc")]
        public string ColorBlendFunction { get; set; } = "add";

        [JsonPropertyName("alphaBlendFunc")]
        public string AlphaBlendFunction { get; set; } = "add";

        [JsonPropertyName("colorSrcBlend")]
        public string ColorSourceBlend { get; set; } = "one";

        [JsonPropertyName("alphaSrcBlend")]
        public string AlphaSourceBlend { get; set; } = "one";
        
        [JsonPropertyName("colorDstBlend")]
        public string ColorDestBlend { get; set; } = "zero";

        [JsonPropertyName("alphaDstBlend")]
        public string AlphaDestBlend { get; set; } = "zero";

        [JsonPropertyName("blendColor")]
        public string BlendColor { get; set; } = "0, 0, 0, 0";

        [JsonPropertyName("writeR")]
        public bool WriteR { get; set; } = true;

        [JsonPropertyName("writeG")]
        public bool WriteG { get; set; } = true;

        [JsonPropertyName("writeB")]
        public bool WriteB { get; set; } = true;

        [JsonPropertyName("writeA")]
        public bool WriteA { get; set; } = true;

        [JsonPropertyName("depthEnable")]
        public bool DepthEnable { get; set; } = true;
        
        [JsonPropertyName("depthWriteEnable")]
        public bool DepthWriteEnable { get; set; } = true;
        
        [JsonPropertyName("depthCompare")]
        public string DepthCompare { get; set; } = "lessEqual";

        [JsonPropertyName("stencilEnable")]
        public bool StencilEnable { get; set; } = false;

        [JsonPropertyName("stencilCompare")]
        public string StencilCompare { get; set; } = "always";

        [JsonPropertyName("stencilPass")]
        public string StencilPass { get; set; } = "keep";

        [JsonPropertyName("stencilFail")]
        public string StencilFail { get; set; } = "keep";

        [JsonPropertyName("stencilDepthFail")]
        public string StencilDepthFail { get; set; } = "keep";

        [JsonPropertyName("twoSidedStencil")]
        public bool TwoSidedStencil { get; set; } = false;

        [JsonPropertyName("ccwStencilCompare")]
        public string CCWStencilCompare { get; set; } = "always";

        [JsonPropertyName("ccwStencilPass")]
        public string CCWStencilPass { get; set; } = "keep";

        [JsonPropertyName("ccwStencilFail")]
        public string CCWStencilFail { get; set; } = "keep";

        [JsonPropertyName("ccwStencilDepthFail")]
        public string CCWStencilDepthFail { get; set; } = "keep";
        
        [JsonPropertyName("stencilMask")]
        public int StencilMask { get; set; } = int.MaxValue;
        
        [JsonPropertyName("stencilWriteMask")]
        public int StencilWriteMask { get; set; } = int.MaxValue;
        
        [JsonPropertyName("stencilRef")]
        public int StencilRef { get; set; } = 0;
        
        [JsonPropertyName("cullMode")]
        public string CullMode { get; set; } = "cullClockwiseFace";
        
        [JsonPropertyName("depthBias")]
        public float DepthBias { get; set; } = 0f;
        
        [JsonPropertyName("fillMode")]
        public string FillMode { get; set; } = "solid";
        
        [JsonPropertyName("msaa")]
        public bool MSAA { get; set; } = true;
        
        [JsonPropertyName("scissorTestEnable")]
        public bool ScissorTestEnable { get; set; } = false;
        
        [JsonPropertyName("slopeScaleDepthBias")]
        public float SlopeScaleDepthBias { get; set; } = 0f;

        [JsonPropertyName("intParams")]
        public Dictionary<string, int> IntParams { get; set; } = new Dictionary<string, int>();

        [JsonPropertyName("floatParams")]
        public Dictionary<string, float> FloatParams { get; set; } = new Dictionary<string, float>();

        [JsonPropertyName("vec2Params")]
        public Dictionary<string, string> Vec2Params { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("vec3Params")]
        public Dictionary<string, string> Vec3Params { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("vec4Params")]
        public Dictionary<string, string> Vec4Params { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("tex2DParams")]
        public Dictionary<string, string> Tex2DParams { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("texCubeParams")]
        public Dictionary<string, string> TexCubeParams { get; set; } = new Dictionary<string, string>();
    }

    private struct Color
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = (byte)(r * 255);
            G = (byte)(g * 255);
            B = (byte)(b * 255);
            A = (byte)(a * 255);
        }

        public Color(int r, int g, int b, int a = 255)
        {
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
            A = (byte)a;
        }
    }

    private struct BoundingSphere
    {
        public Vector3 position;
        public float radius;

        public BoundingSphere(Vector3 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }

        public static BoundingSphere CreateMerged(BoundingSphere a, BoundingSphere b)
        {
            Vector3 diff = b.position - a.position;
			float distance = diff.Length();

            // intersection
			if (distance <= a.radius + b.radius)
			{
				// a contains b
				if (distance <= a.radius - b.radius)
				{
                    return a;
				}

				// b contains a
				if (distance <= b.radius - a.radius)
				{
                    return b;
				}
			}

			float radius1 = Math.Max(a.radius - distance, b.radius);
			float radius2 = Math.Max(a.radius + distance, b.radius);

			diff += (radius1 - radius2) / (2 * diff.Length()) * diff;

            return new BoundingSphere(a.position + diff, (radius1 + radius2) / 2f);
        }

        public BoundingSphere Transform(Matrix4x4 matrix)
        {
            BoundingSphere sphere = new BoundingSphere
            {
                position = Vector3.Transform(position, matrix),
                radius = radius *
                (
                    (float)Math.Sqrt((double)Math.Max(
                        (matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12) + (matrix.M13 * matrix.M13),
                        Math.Max(
                            (matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22) + (matrix.M23 * matrix.M23),
                            (matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32) + (matrix.M33 * matrix.M33))
                        )
                    )
                )
            };
            return sphere;
        }
    }

    private struct MeshVert
    {
        public Vector3 pos;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 uv0;
        public Vector2 uv1;
        public Color color0;
        public Color color1;
        public Color boneJoints;
        public Color boneWeights;
    }

    private class PraxisMesh
    {
        public BoundingSphere bounds;
        public MeshVert[] vertices;
        public ushort[] indices;

        public PraxisMesh(MeshVert[] vertices, ushort[] indices)
        {
            this.vertices = vertices;
            this.indices = indices;
        }
    }

    private struct Primitive
    {
        public PraxisMesh mesh;
        public Material material;
    }

    private class ModelPart
    {
        public BoundingSphere Bounds
        {
            get
            {
                return mesh.bounds.Transform(transform);
            }
        }

        public PraxisMesh mesh;
        public Material material;
        public Matrix4x4 transform;

        public ModelPart(PraxisMesh mesh, Material material, Matrix4x4 transform)
        {
            this.mesh = mesh;
            this.material = material;
            this.transform = transform;
        }
    }

    private class Model
    {
        public BoundingSphere bounds;
        public List<ModelPart> parts = new List<ModelPart>();

        public void RecalcBounds()
        {
            if (parts.Count > 0)
            {
                bounds = parts[0].Bounds;
                
                for (int i = 1; i < parts.Count; i++)
                {
                    bounds = BoundingSphere.CreateMerged(bounds, parts[i].Bounds);
                }
            }
            else
            {
                bounds = new BoundingSphere(Vector3.Zero, 0f);
            }
        }
    }

    private CompressonatorTextureProcessor _textureProcessor;

    public GltfProcessor(CompressonatorTextureProcessor textureProcessor)
    {
        _textureProcessor = textureProcessor;
    }

    protected override string GetOutputExtension(string inputExtension)
    {
        return "pmdl";
    }

    protected override void Process(BuildInputFile<Data> inputFile, string outputPath, BuildOptions options)
    {
        var model = ModelRoot.Load(inputFile.filepath);
        var praxisModel = new Model();

        List<string> matPaths = new List<string>();

        // convert materials
        foreach (var mat in model.LogicalMaterials)
        {
            // if metadata specifies an override, use that
            if (inputFile.metadata.materials?.ContainsKey(mat.Name) ?? false)
            {
                matPaths.Add(inputFile.metadata.materials[mat.Name]);
            }
            // otherwise generate a new default material file in the destination path and save path to it
            else
            {
                string matBasePath = MakeRelativePath(Path.GetDirectoryName(outputPath)!, options.outputDirectory);
                string matPath = Path.Combine(Path.GetDirectoryName(outputPath)!, mat.Name + ".json");

                ConvertMaterial(mat, matPath, matBasePath, options.outputDirectory);

                matPaths.Add("content/" + Path.Combine(matBasePath, mat.Name + ".json"));
            }
        }

        // convert logical meshes into Praxis runtime meshes
        Dictionary<Mesh, List<Primitive>> meshMap = new Dictionary<Mesh, List<Primitive>>();
        foreach (var m in model.LogicalMeshes)
        {
            var list = new List<Primitive>();
            LoadGltfMesh(m, list);
            meshMap[m] = list;
        }

        // convert into flat mesh parts array
        foreach (var node in model.LogicalNodes)
        {
            if (node.Mesh != null)
            {
                var primList = meshMap[node.Mesh];

                foreach (var prim in primList)
                {
                    praxisModel.parts.Add(new ModelPart(prim.mesh, prim.material, node.WorldMatrix));
                }
            }
        }

        praxisModel.RecalcBounds();

        using (var outfile = File.OpenWrite(outputPath))
        using (var writer = new BinaryWriter(outfile, System.Text.Encoding.UTF8))
        {
            // 8-byte magic 'PRXSMESH' (0x4853454D53585250)
            writer.Write(0x4853454D53585250UL);

            // 4-byte version (100)
            writer.Write(100U);

            // 4-byte number of model parts
            writer.Write(praxisModel.parts.Count);

            // 4-byte number of materials
            writer.Write(matPaths.Count);

            // 4-byte number of animations (0 if model does not have a skeleton)
            if (model.LogicalSkins.Count == 0) writer.Write(0);
            else writer.Write(model.LogicalAnimations.Count);

            // 1-byte hasSkeleton flag
            writer.Write(model.LogicalSkins.Count > 0);

            // bounding sphere position + radius (16 bytes)
            writer.Write(praxisModel.bounds.position.X);
            writer.Write(praxisModel.bounds.position.Y);
            writer.Write(praxisModel.bounds.position.Z);
            writer.Write(praxisModel.bounds.radius);

            // serialize material paths
            foreach (var mat in matPaths)
            {
                writer.Write(mat);
            }

            // serialize each model part
            foreach (var part in praxisModel.parts)
            {
                SerializeModelPart(part, writer);
            }

            List<Node> skeletonNodes = new List<Node>();

            // serialize skeleton
            if (model.LogicalSkins.Count > 0)
            {
                var skin = model.LogicalSkins[0];

                Dictionary<Node, int> jointmap = new Dictionary<Node, int>();

                // note: skin may omit "skeleton", in which case we just use the root node of the first joint (according to spec joints must all share
                // a common root)
                Node skeletonRoot = skin.Skeleton ?? skin.GetJoint(0).Joint.VisualRoot;

                for (int i = 0; i < skin.JointsCount; i++)
                {
                    var jointNode = skin.GetJoint(i).Joint;
                    var root = jointNode.VisualRoot;
                    jointmap[jointNode] = i;
                }

                GatherSkeleton(skeletonRoot, skeletonNodes);

                // serialize skeleton nodes
                writer.Write(skeletonNodes.Count);
                foreach (var node in skeletonNodes)
                {
                    SerializeSkeleton(skin, node, jointmap, skeletonNodes, writer);
                }

                // serialize animations
                Dictionary<Node, IAnimationSampler<Vector3>> transformSamplers = [];
                Dictionary<Node, IAnimationSampler<Quaternion>> rotationSamplers = [];
                Dictionary<Node, IAnimationSampler<Vector3>> scaleSamplers = [];

                foreach (var anim in model.LogicalAnimations)
                {    
                    // gather animation channels (note: valid for GLTF to have multiple channels targetting a node's pos/rot/scale, but we condense them)
                    transformSamplers.Clear();
                    rotationSamplers.Clear();
                    scaleSamplers.Clear();
                    GatherChannels(anim, transformSamplers, rotationSamplers, scaleSamplers);

                    SerializeAnimation(anim, transformSamplers, rotationSamplers, scaleSamplers, skeletonNodes, writer);
                }
            }
        }
    }

    private string ConvertTexture(Image image, CompressonatorTextureProcessor.TextureFormat fmt, int alphaThreshold, string basePath, string contentPath, string texName)
    {
        // unpack image to temporary file
        var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        tmpPath = Path.ChangeExtension(tmpPath, image.Content.FileExtension);
        image.Content.SaveToFile(tmpPath);

        string texPath = Path.Combine(basePath, texName);
        string realDir = Path.Combine(contentPath, texPath);

        // convert via Compressonator
        _textureProcessor.ConvertTexture(tmpPath, new CompressonatorTextureProcessor.TextureMetadata
        {
            format = fmt,
            alphaThreshold = alphaThreshold,
            mipmap = true
        }, realDir);

        // delete temp file
        File.Delete(tmpPath);

        return texPath;
    }

    private void ConvertMaterial(Material mat, string outPath, string basePath, string contentPath)
    {
        PraxisMaterialData matData = new PraxisMaterialData();
        matData.FloatParams["AlphaCutoff"] = mat.AlphaCutoff;
        matData.Effect = "content/shaders/BasicLit.fxo";
        matData.EffectTechnique = "Default";
        matData.EffectTechniqueSkinned = "Skinned";

        switch (mat.Alpha)
        {
            case AlphaMode.BLEND:
                matData.DepthWriteEnable = false;
                break;
            case AlphaMode.MASK:
                matData.EffectTechnique = "Default_Mask";
                matData.EffectTechniqueSkinned = "Skinned_Mask";
                break;
        }

        if (mat.FindChannel("BaseColor") is MaterialChannel baseColor)
        {
            matData.Vec4Params["DiffuseColor"] = $"{baseColor.Color.X}, {baseColor.Color.Y}, {baseColor.Color.Z}, {baseColor.Color.W}";

            if (baseColor.Texture != null)
            {
                // opaque materials use BC1 textures
                // masked materials use BC1 w/ 1-bit alpha
                // blended materials use BC3

                var fmt = CompressonatorTextureProcessor.TextureFormat.BC1;
                var alphaThreshold = 0;

                if (mat.Alpha == AlphaMode.MASK)
                {
                    alphaThreshold = (int)(mat.AlphaCutoff * 255);
                }
                else if (mat.Alpha == AlphaMode.BLEND)
                {
                    fmt = CompressonatorTextureProcessor.TextureFormat.BC3;
                }

                string texPath = ConvertTexture(baseColor.Texture.PrimaryImage, fmt, alphaThreshold, basePath, contentPath, mat.Name + "_DiffuseColor.dds");
                matData.Tex2DParams["DiffuseTexture"] = "content/" + texPath;
            }
        }

        if (mat.FindChannel("Emissive") is MaterialChannel emissive)
        {
            matData.Vec4Params["EmissiveColor"] = $"{emissive.Color.X}, {emissive.Color.Y}, {emissive.Color.Z}, {emissive.Color.W}";

            if (emissive.Texture != null)
            {
                string texPath = ConvertTexture(emissive.Texture.PrimaryImage, CompressonatorTextureProcessor.TextureFormat.BC1, 0, basePath, contentPath, mat.Name + "_Emissive.dds");
                matData.Tex2DParams["EmissiveTexture"] = "content/" + texPath;
            }
        }

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        string matJson = JsonSerializer.Serialize(matData, options);
        File.WriteAllText(outPath, matJson);
    }

    private void GatherChannels(Animation anim, Dictionary<Node, IAnimationSampler<Vector3>> transformSamplers,
        Dictionary<Node, IAnimationSampler<Quaternion>> rotationSamplers, Dictionary<Node, IAnimationSampler<Vector3>> scaleSamplers)
    {
        foreach (var channel in anim.Channels)
        {
            var translation = channel.GetTranslationSampler();
            var rotation = channel.GetRotationSampler();
            var scale = channel.GetScaleSampler();

            if (translation != null)
            {
                transformSamplers[channel.TargetNode] = translation;
            }

            if (rotation != null)
            {
                rotationSamplers[channel.TargetNode] = rotation;
            }

            if (scale != null)
            {
                scaleSamplers[channel.TargetNode] = scale;
            }
        }   
    }

    private void SerializeAnimation(Animation anim, Dictionary<Node, IAnimationSampler<Vector3>> transformSamplers,
        Dictionary<Node, IAnimationSampler<Quaternion>> rotationSamplers, Dictionary<Node, IAnimationSampler<Vector3>> scaleSamplers,
        List<Node> nodeArray, BinaryWriter writer)
    {
        // name
        writer.Write(anim.Name);

        // duration
        writer.Write(anim.Duration);
        
        // channel count
        writer.Write(anim.Channels.Count);

        foreach (var channel in anim.Channels)
        {
            // target node id
            int targetId = nodeArray.IndexOf(channel.TargetNode);
            writer.Write(targetId);

            var translation = transformSamplers.ContainsKey(channel.TargetNode) ? transformSamplers[channel.TargetNode] : null;
            var rotation = rotationSamplers.ContainsKey(channel.TargetNode) ? rotationSamplers[channel.TargetNode] : null;
            var scale = scaleSamplers.ContainsKey(channel.TargetNode) ? scaleSamplers[channel.TargetNode] : null;

            if (translation == null)
            {
                // 0 keys
                writer.Write((byte)0);
                writer.Write(0);
            }
            else
            {
                SerializeCurve(translation, writer);
            }

            if (rotation == null)
            {
                // 0 keys
                writer.Write((byte)0);
                writer.Write(0);
            }
            else
            {
                SerializeCurve(rotation, writer);
            }

            if (scale == null)
            {
                // 0 keys
                writer.Write((byte)0);
                writer.Write(0);
            }
            else
            {
                SerializeCurve(scale, writer);
            }
        }
    }

    private void SerializeCurve(IAnimationSampler<Vector3> curve, BinaryWriter writer)
    {
        switch (curve.InterpolationMode)
        {
            case AnimationInterpolationMode.STEP:
                writer.Write((byte)0);
                break;
            case AnimationInterpolationMode.LINEAR:
                writer.Write((byte)1);
                break;
            case AnimationInterpolationMode.CUBICSPLINE:
                writer.Write((byte)2);
                break;
        }

        if (curve.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE)
        {
            var keys = curve.GetCubicKeys().ToArray();
            writer.Write(keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                // time
                writer.Write(keys[i].Key);
                // tangent in
                writer.Write(keys[i].Value.TangentIn.X);
                writer.Write(keys[i].Value.TangentIn.Y);
                writer.Write(keys[i].Value.TangentIn.Z);
                // value
                writer.Write(keys[i].Value.Value.X);
                writer.Write(keys[i].Value.Value.Y);
                writer.Write(keys[i].Value.Value.Z);
                // tangent out
                writer.Write(keys[i].Value.TangentOut.X);
                writer.Write(keys[i].Value.TangentOut.Y);
                writer.Write(keys[i].Value.TangentOut.Z);
            }
        }
        else
        {
            var keys = curve.GetLinearKeys().ToArray();
            writer.Write(keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                // time
                writer.Write(keys[i].Key);
                // value
                writer.Write(keys[i].Value.X);
                writer.Write(keys[i].Value.Y);
                writer.Write(keys[i].Value.Z);
            }
        }
    }

    private void SerializeCurve(IAnimationSampler<Quaternion> curve, BinaryWriter writer)
    {
        switch (curve.InterpolationMode)
        {
            case AnimationInterpolationMode.STEP:
                writer.Write((byte)0);
                break;
            case AnimationInterpolationMode.LINEAR:
                writer.Write((byte)1);
                break;
            case AnimationInterpolationMode.CUBICSPLINE:
                writer.Write((byte)2);
                break;
        }

        if (curve.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE)
        {
            var keys = curve.GetCubicKeys().ToArray();
            writer.Write(keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                // time
                writer.Write(keys[i].Key);
                // tangent in
                writer.Write(keys[i].Value.TangentIn.X);
                writer.Write(keys[i].Value.TangentIn.Y);
                writer.Write(keys[i].Value.TangentIn.Z);
                writer.Write(keys[i].Value.TangentIn.W);
                // value
                writer.Write(keys[i].Value.Value.X);
                writer.Write(keys[i].Value.Value.Y);
                writer.Write(keys[i].Value.Value.Z);
                writer.Write(keys[i].Value.Value.W);
                // tangent out
                writer.Write(keys[i].Value.TangentOut.X);
                writer.Write(keys[i].Value.TangentOut.Y);
                writer.Write(keys[i].Value.TangentOut.Z);
                writer.Write(keys[i].Value.TangentOut.W);
            }
        }
        else
        {
            var keys = curve.GetLinearKeys().ToArray();
            writer.Write(keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                // time
                writer.Write(keys[i].Key);
                // value
                writer.Write(keys[i].Value.X);
                writer.Write(keys[i].Value.Y);
                writer.Write(keys[i].Value.Z);
                writer.Write(keys[i].Value.W);
            }
        }
    }

    private void GatherSkeleton(Node node, List<Node> nodeArray)
    {
        nodeArray.Add(node);

        foreach (var child in node.VisualChildren)
        {
            GatherSkeleton(child, nodeArray);
        }
    }

    private void SerializeSkeleton(Skin skin, Node node, Dictionary<Node, int> jointmap, List<Node> nodeArray, BinaryWriter writer)
    {
        Matrix4x4 invBindPose;

        // bone name
        writer.Write(node.Name);

        // joint index
        if (jointmap.ContainsKey(node))
        {
            writer.Write(jointmap[node]);
            invBindPose = skin.GetJoint(jointmap[node]).InverseBindMatrix;
        }
        else
        {
            writer.Write(-1);
            Matrix4x4.Invert(node.WorldMatrix, out invBindPose);
        }

        // local rest position
        writer.Write(node.LocalTransform.Translation.X);
        writer.Write(node.LocalTransform.Translation.Y);
        writer.Write(node.LocalTransform.Translation.Z);

        // local rest rotation
        writer.Write(node.LocalTransform.Rotation.X);
        writer.Write(node.LocalTransform.Rotation.Y);
        writer.Write(node.LocalTransform.Rotation.Z);
        writer.Write(node.LocalTransform.Rotation.W);

        // local rest scale
        writer.Write(node.LocalTransform.Scale.X);
        writer.Write(node.LocalTransform.Scale.Y);
        writer.Write(node.LocalTransform.Scale.Z);

        // inverse bind pose (not necessarily the same as rest pose!)
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                writer.Write(invBindPose[i, j]);
            }
        }

        // child indices
        var children = node.VisualChildren.ToArray();
        writer.Write(children.Length);

        foreach (var child in children)
        {
            int childIndex = nodeArray.IndexOf(child);
            writer.Write(childIndex);
        }
    }

    private void SerializeModelPart(ModelPart modelPart, BinaryWriter writer)
    {
        writer.Write(modelPart.material.LogicalIndex);

        writer.Write(modelPart.mesh.vertices.Length);
        writer.Write(modelPart.mesh.indices.Length);

        for (int i = 0; i < modelPart.mesh.vertices.Length; i++)
        {
            var vtx = modelPart.mesh.vertices[i];
            var pos = Vector3.Transform(vtx.pos, modelPart.transform);
            var normal = Vector3.TransformNormal(vtx.normal, modelPart.transform);
            var tangent = Vector3.TransformNormal(new Vector3(vtx.tangent.X, vtx.tangent.Y, vtx.tangent.Z), modelPart.transform);

            writer.Write(pos.X);
            writer.Write(pos.Y);
            writer.Write(pos.Z);
            writer.Write(normal.X);
            writer.Write(normal.Y);
            writer.Write(normal.Z);
            writer.Write(tangent.X);
            writer.Write(tangent.Y);
            writer.Write(tangent.Z);
            writer.Write(vtx.tangent.W);
            writer.Write(vtx.uv0.X);
            writer.Write(vtx.uv0.Y);
            writer.Write(vtx.uv1.X);
            writer.Write(vtx.uv1.Y);
            writer.Write(vtx.color0.R);
            writer.Write(vtx.color0.G);
            writer.Write(vtx.color0.B);
            writer.Write(vtx.color0.A);
            writer.Write(vtx.color1.R);
            writer.Write(vtx.color1.G);
            writer.Write(vtx.color1.B);
            writer.Write(vtx.color1.A);
            writer.Write(vtx.boneJoints.R);
            writer.Write(vtx.boneJoints.G);
            writer.Write(vtx.boneJoints.B);
            writer.Write(vtx.boneJoints.A);
            writer.Write(vtx.boneWeights.R);
            writer.Write(vtx.boneWeights.G);
            writer.Write(vtx.boneWeights.B);
            writer.Write(vtx.boneWeights.A);
        }

        for (int i = 0; i < modelPart.mesh.indices.Length; i++)
        {
            writer.Write(modelPart.mesh.indices[i]);
        }
    }

    private static void LoadGltfMesh(Mesh mesh, List<Primitive> outMeshList)
    {
        // convert vertices
        var primitives = mesh.Primitives;

        foreach (var prim in primitives)
        {
            var vpos = prim.GetVertexAccessor("POSITION")?.AsVector3Array()!;
            var vnorm = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var vtan = prim.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var vcolor0 = prim.GetVertexAccessor("COLOR_0")?.AsVector4Array();
            var vcolor1 = prim.GetVertexAccessor("COLOR_1")?.AsVector4Array();
            var vtex0 = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var vtex1 = prim.GetVertexAccessor("TEXCOORD_1")?.AsVector2Array();
            var vjoints = prim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            var vweights = prim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
            var tris = prim.GetTriangleIndices().ToArray();

            // since we only use 16-bit indices, ensure that model won't overflow max index
            Debug.Assert(vpos.Count <= 65536);

            MeshVert[] vertices = new MeshVert[vpos.Count];
            ushort[] indices = new ushort[tris.Length * 3];

            float radius = 0f;

            for (int i = 0; i < vpos.Count; i++)
            {
                MeshVert v = new MeshVert
                {
                    pos = vpos[i],
                    color0 = new Color(255, 255, 255)
                };

                radius = MathF.Max(radius, v.pos.Length());

                if (vnorm != null) v.normal = vnorm[i];
                if (vtan != null) v.tangent = vtan[i];
                if (vcolor0 != null) v.color0 = new Color(vcolor0[i].X, vcolor0[i].Y, vcolor0[i].Z, vcolor0[i].W);
                if (vcolor1 != null) v.color1 = new Color(vcolor1[i].X, vcolor1[i].Y, vcolor1[i].Z, vcolor1[i].W);
                if (vtex0 != null) v.uv0 = vtex0[i];
                if (vtex1 != null) v.uv1 = vtex1[i];
                if (vjoints != null) v.boneJoints = new Color((byte)vjoints[i].X, (byte)vjoints[i].Y, (byte)vjoints[i].Z, (byte)vjoints[i].W);
                if (vweights != null) v.boneWeights = new Color(vweights[i].X, vweights[i].Y, vweights[i].Z, vweights[i].W);

                vertices[i] = v;
            }

            for (int i = 0; i < tris.Length; i++)
            {
                indices[i * 3] = (ushort)tris[i].A;
                indices[(i * 3) + 1] = (ushort)tris[i].B;
                indices[(i * 3) + 2] = (ushort)tris[i].C;

                if (vnorm == null)
                {
                    // oh... model doesn't have normals. I guess just generate something.
                    var a = vpos[tris[i].A];
                    var b = vpos[tris[i].B];
                    var c = vpos[tris[i].C];

                    var n = Vector3.Cross(b - a, c - b);

                    vertices[tris[i].A].normal = n;
                    vertices[tris[i].B].normal = n;
                    vertices[tris[i].C].normal = n;
                }
            }
            
            outMeshList.Add(new Primitive
            {
                mesh = new PraxisMesh(vertices, indices)
                {
                    bounds = new BoundingSphere(Vector3.Zero, radius)
                },
                material = prim.Material
            });
        }
    }
}
