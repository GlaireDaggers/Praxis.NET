﻿using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using OWB;

namespace Praxis.Core;

public class JsonVector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string data = reader.GetString()!;
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Vector2(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture)
        );
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonVector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string data = reader.GetString()!;
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Vector3(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture),
            float.Parse(v[2], CultureInfo.InvariantCulture)
        );
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonVector4Converter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string data = reader.GetString()!;
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Vector4(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture),
            float.Parse(v[2], CultureInfo.InvariantCulture),
            float.Parse(v[3], CultureInfo.InvariantCulture)
        );
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonQuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string data = reader.GetString()!;
        string[] v = data.Split(CultureInfo.InvariantCulture.TextInfo.ListSeparator.ToCharArray());
        return new Quaternion(
            float.Parse(v[0], CultureInfo.InvariantCulture),
            float.Parse(v[1], CultureInfo.InvariantCulture),
            float.Parse(v[2], CultureInfo.InvariantCulture),
            float.Parse(v[3], CultureInfo.InvariantCulture)
        );
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class JsonColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string data = reader.GetString()!;
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

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}