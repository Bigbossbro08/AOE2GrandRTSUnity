#ifndef RGBtoHSL_HLSL_INCLUDED
#define RGBtoHSL_HLSL_INCLUDED

void RGBtoHSL_float(float3 In, out float3 Out)
{
    float maxC = max(In.r, max(In.g, In.b));
    float minC = min(In.r, min(In.g, In.b));
    float delta = maxC - minC;
    float h = 0.0;
    float s = 0.0;
    float l = (maxC + minC) * 0.5;

    if (delta > 0.00001)
    {
        s = delta / (1.0 - abs(2.0 * l - 1.0) + 1e-5);
        if (maxC == In.r)
            h = (In.g - In.b) / delta + (In.g < In.b ? 6.0 : 0.0);
        else if (maxC == In.g)
            h = (In.b - In.r) / delta + 2.0;
        else
            h = (In.r - In.g) / delta + 4.0;
        h /= 6.0;
    }

    Out = float3(h, s, l);
}

void HSLtoRGB_float(float3 hsl, out float3 OUT);

void ApplyPlayerColorFromMask_float(float4 baseColor, float3 playerColor, float mask, out float4 OUT)
{
    //float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
    //float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).a;
            
    // Player mask coloring (your original logic)
    if (mask >= 0.99)
    {
        float3 baseHSL = 0.0;
        RGBtoHSL_float(baseColor.rgb, baseHSL);
        float3 playerHSL = 0.0;
        RGBtoHSL_float(playerColor.rgb, playerHSL);
        float3 resultHSL = float3(playerHSL.r, playerHSL.g, baseHSL.b);
        float3 resultRGB = 0.0;
        HSLtoRGB_float(resultHSL, resultRGB);
        OUT = float4(resultRGB, baseColor.a);
        return;
    }
            
    OUT = baseColor;
}
//float3 RGBtoHSL_float3(float3 color)
//{
//    float maxC = max(color.r, max(color.g, color.b));
//    float minC = min(color.r, min(color.g, color.b));
//    float delta = maxC - minC;
//    float h = 0.0;
//    float s = 0.0;
//    float l = (maxC + minC) * 0.5;
//
//    if (delta > 0.00001)
//    {
//        s = delta / (1.0 - abs(2.0 * l - 1.0) + 1e-5);
//        if (maxC == color.r)
//            h = (color.g - color.b) / delta + (color.g < color.b ? 6.0 : 0.0);
//        else if (maxC == color.g)
//            h = (color.b - color.r) / delta + 2.0;
//        else
//            h = (color.r - color.g) / delta + 4.0;
//        h /= 6.0;
//    }
//    return float3(h, s, l);
//}

float hue2rgb_float(float p, float q, float t)
{
    if (t < 0.0) t += 1.0;
    if (t > 1.0) t -= 1.0;
    if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
    if (t < 1.0 / 2.0) return q;
    if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
    return p;
}

void HSLtoRGB_float(float3 hsl, out float3 OUT)
{
    float r, g, b;
    if (hsl.y == 0.0)
    {
        r = g = b = hsl.z;
    }
    else
    {
        float q = hsl.z < 0.5 ? hsl.z * (1.0 + hsl.y) : hsl.z + hsl.y - hsl.z * hsl.y;
        float p = 2.0 * hsl.z - q;
        r = hue2rgb_float(p, q, hsl.x + 1.0 / 3.0);
        g = hue2rgb_float(p, q, hsl.x);
        b = hue2rgb_float(p, q, hsl.x - 1.0 / 3.0);
    }
    OUT = saturate(float3(r, g, b));
    //return saturate(float3(r, g, b));
}

#endif