float4 TargetSize;

Texture2D SpriteTexture : register(t0);
sampler SpriteTextureSampler : register(s0);

struct PixelInput {
    float4 position: SV_Position0;
    float2 texcoord: TEXCOORD0;
};

float4 PS(PixelInput p) : SV_TARGET {
    float4 tex1 = tex2D(SpriteTextureSampler, p.texcoord);
    float4 tex2 = tex2D(SpriteTextureSampler, p.texcoord + float2(TargetSize.z * 2, 0.0));
    return float4(tex1.r, tex2.g, tex2.b, 1.0);
}

technique Main {
    pass {
        PixelShader = compile ps_3_0 PS();
    }
}
