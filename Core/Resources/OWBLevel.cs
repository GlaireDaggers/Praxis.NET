namespace OWB
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Microsoft.Xna.Framework;

    public partial class Level
    {
        [JsonPropertyName("root")]
        public SceneRootNode? Root { get; set; }
    }
    
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SceneRootNode), typeDiscriminator: "SceneRootNode")]
    [JsonDerivedType(typeof(BrushNode), typeDiscriminator: "BrushNode")]
    [JsonDerivedType(typeof(GenericEntityNode), typeDiscriminator: "GenericEntityNode")]
    [JsonDerivedType(typeof(LightNode), typeDiscriminator: "LightNode")]
    [JsonDerivedType(typeof(SplineNode), typeDiscriminator: "SplineNode")]
    [JsonDerivedType(typeof(StaticMeshNode), typeDiscriminator: "StaticMeshNode")]
    [JsonDerivedType(typeof(TerrainNode), typeDiscriminator: "TerrainNode")]
    public partial class Node
    {
        public Matrix worldTransform;

        [JsonPropertyName("children")]
        public Node[]? Children { get; set; }

        [JsonPropertyName("guid")]
        public Guid Guid { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("position")]
        public Vector3 Position { get; set; }

        [JsonPropertyName("rotation")]
        public Quaternion Rotation { get; set; }

        [JsonPropertyName("scale")]
        public Vector3 Scale { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public partial class SceneRootNode : Node
    {
        [JsonPropertyName("ambientColor")]
        public Color AmbientColor { get; set; }

        [JsonPropertyName("ambientIntensity")]
        public double AmbientIntensity { get; set; }
    }

    public partial class GenericEntityNode : Node
    {
        [JsonPropertyName("entityDefinition")]
        public Guid EntityDefinition { get; set; }

        [JsonPropertyName("fields")]
        public Dictionary<string, object>? Fields { get; set; }
    }

    public partial class LightNode : Node
    {
        [JsonPropertyName("color")]
        public Color Color { get; set; }

        [JsonPropertyName("innerConeAngle")]
        public double InnerConeAngle { get; set; }

        [JsonPropertyName("intensity")]
        public double Intensity { get; set; }

        [JsonPropertyName("lightType")]
        public long LightType { get; set; }

        [JsonPropertyName("outerConeAngle")]
        public double OuterConeAngle { get; set; }

        [JsonPropertyName("radius")]
        public double Radius { get; set; }
    }

    public partial class SplineNode : Node
    {
        [JsonPropertyName("closed")]
        public bool Closed { get; set; }

        [JsonPropertyName("points")]
        public SplineControlPoint[]? Points { get; set; }
    }

    public partial class StaticMeshNode : Node
    {
        [JsonPropertyName("collision")]
        public long Collision { get; set; }

        [JsonPropertyName("meshPath")]
        public string? MeshPath { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }
    }

    public partial class BrushNode : Node
    {
        [JsonPropertyName("collision")]
        public long Collision { get; set; }

        [JsonPropertyName("planes")]
        public BrushPlane[]? Planes { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }
    }
    
    public partial class TerrainNode : Node
    {
        [JsonPropertyName("detail")]
    	public long Detail { get; set; }
    	
        [JsonPropertyName("lodDistanceMultiplier")]
    	public double LodDistanceMultiplier { get; set; }
    	
        [JsonPropertyName("heightScale")]
    	public double HeightScale { get; set; }
    	
        [JsonPropertyName("terrainScale")]
    	public double TerrainScale { get; set; }
    	
        [JsonPropertyName("heightmapRes")]
    	public long HeightmapRes { get; set; }
    	
        [JsonPropertyName("layers")]
    	public TerrainLayer[]? Layers { get; set; }
    }

	public partial class SplineControlPoint
    {
        [JsonPropertyName("position")]
        public Vector3 Position { get; set; }

        [JsonPropertyName("rotation")]
        public Quaternion Rotation { get; set; }

        [JsonPropertyName("scale")]
        public double Scale { get; set; }
    }

    public partial class BrushPlane
    {
        [JsonPropertyName("position")]
        public Vector3 Position { get; set; }

        [JsonPropertyName("rotation")]
        public Quaternion Rotation { get; set; }

        [JsonPropertyName("textureOffset")]
        public Vector2 TextureOffset { get; set; }

        [JsonPropertyName("texturePath")]
        public string? TexturePath { get; set; }

        [JsonPropertyName("textureScale")]
        public Vector2 TextureScale { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }
    }
    
    public partial class TerrainLayer
    {
        [JsonPropertyName("scale")]
    	public double Scale { get; set; }
    	
        [JsonPropertyName("diffusePath")]
    	public string? DiffusePath { get; set; }
    	
        [JsonPropertyName("normalPath")]
    	public string? NormalPath { get; set; }
    	
        [JsonPropertyName("ormPath")]
    	public string? OrmPath { get; set; }
    }
}