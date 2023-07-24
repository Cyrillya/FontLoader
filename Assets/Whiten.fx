sampler uImage0 : register(s0);

float4 Whiten(float2 coords : TEXCOORD0) : COLOR0 {
    float4 c = tex2D(uImage0, coords);
    if (c.a != 0) {
        return c.aaaa;
    }
    return c;
}

technique Technique1 {
    pass RoundRectangle {
        PixelShader = compile ps_2_0 Whiten();
    }
}