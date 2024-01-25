namespace OWB
{
    using System;
    using System.Text.Json.Serialization;
    using Microsoft.Xna.Framework;
    
    public enum FieldType
    {
        Bool,
        Int,
        Float,
        String,
        Vector2,
        Vector3,
        Vector4,
        Quaternion,
        Color,
        MultilineString,
        FilePath,
        NodeRef
    }
    
    public enum ShapeType
    {
        Box,
        Sphere,
        Line
    }

    public partial class Project
    {
        [JsonPropertyName("contentPath")]
        public string? ContentPath { get; set; }

        [JsonPropertyName("entityDefinitions")]
        public EntityDefinition[]? EntityDefinitions { get; set; }
    }

    public partial class EntityDefinition
    {
        [JsonPropertyName("fields")]
        public Field[]? Fields { get; set; }

        [JsonPropertyName("gizmos")]
        public Gizmo[]? Gizmos { get; set; }

        [JsonPropertyName("guid")]
        public Guid Guid { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public partial class Field
    {
        [JsonPropertyName("fieldType")]
        public FieldType FieldType { get; set; }

        [JsonPropertyName("isArray")]
        public bool IsArray { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public partial class Gizmo
    {
        [JsonPropertyName("color")]
        public Color Color { get; set; }

        [JsonPropertyName("position")]
        public Vector3 Position { get; set; }

        [JsonPropertyName("rotation")]
        public Quaternion Rotation { get; set; }

        [JsonPropertyName("scale")]
        public Vector3 Scale { get; set; }

        [JsonPropertyName("shapeType")]
        public ShapeType ShapeType { get; set; }
    }
}