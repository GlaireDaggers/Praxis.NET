// adapted from https://learnopengl.com/Guest-Articles/2022/Phys.-Based-Bloom

float4 TargetSize;
float FilterRadius;
float ThresholdMin;
float ThresholdMax;
float Brightness;

Texture2D SpriteTexture : register(t0);
sampler SpriteTextureSampler : register(s0);

Texture2D BloomTexture;
sampler BloomTextureSampler {	
	Texture = BloomTexture;
};

struct PixelInput {
    float4 position: SV_Position0;
    float2 texcoord: TEXCOORD0;
};

float4 PS_Threshold(PixelInput p) : SV_TARGET {
    float3 e = tex2D(SpriteTextureSampler, p.texcoord).rgb;
    float luma = dot(e, float3(float3(0.299, 0.587, 0.114)));
    luma = smoothstep(ThresholdMin, ThresholdMax, luma);
    return float4(e * luma, 1.0);
}

float4 PS_Composite(PixelInput p) : SV_TARGET {
    float3 a = tex2D(SpriteTextureSampler, p.texcoord).rgb;
    float3 b = tex2D(BloomTextureSampler, p.texcoord).rgb * Brightness;
    return float4(a + b, 1.0);
}

float4 PS_Downsample(PixelInput p) : SV_TARGET {
	float x = TargetSize.z;
	float y = TargetSize.w;

	// take 13 samples around current texel
	float3 a = tex2D(SpriteTextureSampler, float2(p.texcoord.x - 2*x, p.texcoord.y + 2*y)).rgb;
    float3 b = tex2D(SpriteTextureSampler, float2(p.texcoord.x,       p.texcoord.y + 2*y)).rgb;
    float3 c = tex2D(SpriteTextureSampler, float2(p.texcoord.x + 2*x, p.texcoord.y + 2*y)).rgb;

    float3 d = tex2D(SpriteTextureSampler, float2(p.texcoord.x - 2*x, p.texcoord.y)).rgb;
    float3 e = tex2D(SpriteTextureSampler, float2(p.texcoord.x,       p.texcoord.y)).rgb;
    float3 f = tex2D(SpriteTextureSampler, float2(p.texcoord.x + 2*x, p.texcoord.y)).rgb;

    float3 g = tex2D(SpriteTextureSampler, float2(p.texcoord.x - 2*x, p.texcoord.y - 2*y)).rgb;
    float3 h = tex2D(SpriteTextureSampler, float2(p.texcoord.x,       p.texcoord.y - 2*y)).rgb;
    float3 i = tex2D(SpriteTextureSampler, float2(p.texcoord.x + 2*x, p.texcoord.y - 2*y)).rgb;

    float3 j = tex2D(SpriteTextureSampler, float2(p.texcoord.x - x, p.texcoord.y + y)).rgb;
    float3 k = tex2D(SpriteTextureSampler, float2(p.texcoord.x + x, p.texcoord.y + y)).rgb;
    float3 l = tex2D(SpriteTextureSampler, float2(p.texcoord.x - x, p.texcoord.y - y)).rgb;
    float3 m = tex2D(SpriteTextureSampler, float2(p.texcoord.x + x, p.texcoord.y - y)).rgb;
    
    // apply weighted distribution
    float3 downsample = e * 0.125;
    downsample += (a+c+g+i)*0.03125;
    downsample += (b+d+f+h)*0.0625;
    downsample += (j+k+l+m)*0.125;
    
    return float4(downsample, 1.0);
}

float4 PS_Upsample(PixelInput p) : SV_TARGET {
	float x = FilterRadius * TargetSize.z;
    float y = FilterRadius * TargetSize.w;
    
    // take 3x3 samples around current texel
    float3 a = tex2D(SpriteTextureSampler, float2(p.texcoord.x - x, p.texcoord.y + y)).rgb;
    float3 b = tex2D(SpriteTextureSampler, float2(p.texcoord.x,     p.texcoord.y + y)).rgb;
    float3 c = tex2D(SpriteTextureSampler, float2(p.texcoord.x + x, p.texcoord.y + y)).rgb;

    float3 d = tex2D(SpriteTextureSampler, float2(p.texcoord.x - x, p.texcoord.y)).rgb;
    float3 e = tex2D(SpriteTextureSampler, float2(p.texcoord.x,     p.texcoord.y)).rgb;
    float3 f = tex2D(SpriteTextureSampler, float2(p.texcoord.x + x, p.texcoord.y)).rgb;

    float3 g = tex2D(SpriteTextureSampler, float2(p.texcoord.x - x, p.texcoord.y - y)).rgb;
    float3 h = tex2D(SpriteTextureSampler, float2(p.texcoord.x,     p.texcoord.y - y)).rgb;
    float3 i = tex2D(SpriteTextureSampler, float2(p.texcoord.x + x, p.texcoord.y - y)).rgb;
    
    float3 upsample = e * 4.0;
    upsample += (b+d+f+h)*2.0;
    upsample += (a+c+g+i);
    upsample *= 1.0 / 16.0;
    
    return float4(upsample, 1.0);
}

technique Threshold {
    pass {
        PixelShader = compile ps_3_0 PS_Threshold();
    }
}

technique Composite {
    pass {
        PixelShader = compile ps_3_0 PS_Composite();
    }
}

technique Downsample {
    pass {
        PixelShader = compile ps_3_0 PS_Downsample();
    }
}

technique Upsample {
    pass {
        PixelShader = compile ps_3_0 PS_Upsample();
    }
}
