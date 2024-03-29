﻿using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using ResourceCache.Core;

namespace Praxis.Core;

public class JsonResourceHandleConverter<T> : JsonConverter<ResourceHandle<T>>
{
    public readonly PraxisGame Game;

    public JsonResourceHandleConverter(PraxisGame game)
    {
        Game = game;
    }

    public override ResourceHandle<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string path = reader.GetString()!;
        return Game.Resources.Load<T>(path);
    }

    public override void Write(Utf8JsonWriter writer, ResourceHandle<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonRuntimeResourceConverter<T> : JsonConverter<RuntimeResource<T>>
{
    public readonly PraxisGame Game;

    public JsonRuntimeResourceConverter(PraxisGame game)
    {
        Game = game;
    }

    public override RuntimeResource<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string path = reader.GetString()!;
        return Game.Resources.Load<T>(path);
    }

    public override void Write(Utf8JsonWriter writer, RuntimeResource<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonVector2Converter : JsonConverter<Vector2>
{
    public static Vector2 Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Vector2(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture)
        );
    }

    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonVector3Converter : JsonConverter<Vector3>
{
    public static Vector3 Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Vector3(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture),
            float.Parse(v[2], CultureInfo.InvariantCulture)
        );
    }

    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonVector4Converter : JsonConverter<Vector4>
{
    public static Vector4 Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Vector4(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture),
            float.Parse(v[2], CultureInfo.InvariantCulture),
            float.Parse(v[3], CultureInfo.InvariantCulture)
        );
    }
    
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonQuaternionConverter : JsonConverter<Quaternion>
{
    public static Quaternion Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Quaternion(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture),
            float.Parse(v[2], CultureInfo.InvariantCulture),
            float.Parse(v[3], CultureInfo.InvariantCulture)
        );
    }

    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonEulerConverter : JsonConverter<Quaternion>
{
    public static Quaternion Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        float x = float.Parse(v[0]);
        float y = float.Parse(v[1]);
        float z = float.Parse(v[2]);
        return Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(y), MathHelper.ToRadians(x), MathHelper.ToRadians(z));
    }

    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonColorConverter : JsonConverter<Color>
{
    public static Color Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        if (v.Length == 3)
        {
            return new Color(
                int.Parse(v[0], CultureInfo.InvariantCulture),
                int.Parse(v[1], CultureInfo.InvariantCulture),
                int.Parse(v[2], CultureInfo.InvariantCulture)
            );
        }
        else if (v.Length == 4)
        {
            return new Color(
                int.Parse(v[0], CultureInfo.InvariantCulture),
                int.Parse(v[1], CultureInfo.InvariantCulture),
                int.Parse(v[2], CultureInfo.InvariantCulture),
                int.Parse(v[3], CultureInfo.InvariantCulture)
            );
        }
        else
        {
            throw new FormatException();
        }
    }

    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonRectConverter : JsonConverter<Rectangle>
{
    public static Rectangle Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        
        return new Rectangle(
            int.Parse(v[0], CultureInfo.InvariantCulture),
            int.Parse(v[1], CultureInfo.InvariantCulture),
            int.Parse(v[2], CultureInfo.InvariantCulture),
            int.Parse(v[3], CultureInfo.InvariantCulture)
        );
    }

    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonMarginConverter : JsonConverter<Margins>
{
    public static Margins Parse(string data)
    {
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        
        return new Margins(
            int.Parse(v[0], CultureInfo.InvariantCulture),
            int.Parse(v[1], CultureInfo.InvariantCulture),
            int.Parse(v[2], CultureInfo.InvariantCulture),
            int.Parse(v[3], CultureInfo.InvariantCulture)
        );
    }
    
    public override Margins Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, Margins value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonComponentDataConverter : JsonConverter<IComponentData>
{
    private static Dictionary<string, Type> _componentDataTypes = new Dictionary<string, Type>();

    static JsonComponentDataConverter()
    {
        // build a list of serializable IComponentData types
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAssignableTo(typeof(IComponentData)))
                {
                    if (type.GetCustomAttribute<SerializedComponentAttribute>() is SerializedComponentAttribute attr)
                    {
                        _componentDataTypes.Add(attr.TypeName, type);
                    }
                }
            }
        }
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IComponentData).IsAssignableFrom(typeToConvert);
    }

    public override IComponentData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            if (!jsonDoc.RootElement.TryGetProperty("$type", out var typeProperty))
            {
                throw new JsonException();
            }

            var type = typeProperty.GetString()!;

            if (_componentDataTypes.ContainsKey(type))
            {
                var childOptions = new JsonSerializerOptions(options);
                childOptions.Converters.Remove(this);

                return (IComponentData)JsonSerializer.Deserialize(jsonDoc, _componentDataTypes[type], childOptions)!;
            }

            throw new JsonException("Unknown component type: " + type);
        }
    }

    public override void Write(Utf8JsonWriter writer, IComponentData value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}