#ifndef LIL_FUNCTIONS_INCLUDED
#define LIL_FUNCTIONS_INCLUDED

//------------------------------------------------------------------------------------------------------------------------------
// Function
float lilTooning(float value, float border, float blur)
{
    float borderMin = saturate(border - blur * 0.5);
    float borderMax = saturate(border + blur * 0.5);
    return saturate((value - borderMin) / saturate(borderMax - borderMin));
}

float lilTooning(float value, float border, float blur, float borderRange)
{
    float borderMin = saturate(border - blur * 0.5 - borderRange);
    float borderMax = saturate(border + blur * 0.5);
    return saturate((value - borderMin) / saturate(borderMax - borderMin));
}

float4 lilOptMul(float4x4 mat, float3 pos)
{
    return mul(mat, float4(pos,1.0));
    //return mat._m00_m10_m20_m30 * pos.x + (mat._m01_m11_m21_m31 * pos.y + (mat._m02_m12_m22_m32 * pos.z + mat._m03_m13_m23_m33));
}

float3 lilTransformNormalOStoWS(float3 normalOS)
{
    #ifdef UNITY_ASSUME_UNIFORM_SCALING
        return mul((float3x3)LIL_MATRIX_M, normalOS);
    #else
        return mul(normalOS, (float3x3)LIL_MATRIX_I_M);
    #endif
}

float lilLuminance(float3 rgb)
{
    #ifdef LIL_COLORSPACE_GAMMA
        return dot(rgb, float3(0.22, 0.707, 0.071));
    #else
        return dot(rgb, float3(0.0396819152, 0.458021790, 0.00609653955));
    #endif
}

float3 lilGetLightDirection()
{
    return normalize(_MainLightPosition.xyz * lilLuminance(_MainLightColor.rgb) + 
                    unity_SHAr.xyz * 0.333333 + unity_SHAg.xyz * 0.333333 + unity_SHAb.xyz * 0.333333 + 
                    float3(0.0,0.001,0.0));
}

float3 lilGetLightDirection(float3 positionWS)
{
    #if defined(POINT) || defined(SPOT) || defined(POINT_COOKIE)
        return normalize(_MainLightPosition.xyz - positionWS);
    #else
        return _MainLightPosition.xyz;
    #endif
}

float3 lilGetLightMapDirection(float2 uv)
{
    #if defined(LIL_USE_LIGHTMAP) && defined(LIL_USE_DIRLIGHTMAP)
        float4 lightmapDirection = LIL_SAMPLE_LIGHTMAP(LIL_DIRLIGHTMAP_TEX,  LIL_LIGHTMAP_SAMP, uv);
        return lightmapDirection.xyz * 2.0 - 1.0;
    #else
        return 0;
    #endif
}

float3 lilGetSHToon()
{
    float3 N = lilGetLightDirection() * 0.666666;
    float3 res = float3(unity_SHAr.w,unity_SHAg.w,unity_SHAb.w);
    res.r += dot(unity_SHAr.rgb, N);
    res.g += dot(unity_SHAg.rgb, N);
    res.b += dot(unity_SHAb.rgb, N);
    float4 vB = N.xyzz * N.yzzx;
    res.r += dot(unity_SHBr, vB);
    res.g += dot(unity_SHBg, vB);
    res.b += dot(unity_SHBb, vB);
    res += unity_SHC.rgb * (N.x * N.x - N.y * N.y);
    #ifdef LIL_COLORSPACE_GAMMA
        res = LinearToSRGB(res);
    #endif
    return res;
}

float3 lilGetLightColor()
{
    return saturate(_MainLightColor.rgb + lilGetSHToon());
}

float3 lilGetLightMapColor(float2 uv)
{
    float3 outCol = 0;
    #ifdef LIL_USE_LIGHTMAP
        float4 lightmap = LIL_SAMPLE_LIGHTMAP(LIL_LIGHTMAP_TEX, LIL_LIGHTMAP_SAMP, uv);
        outCol += LIL_DECODE_LIGHTMAP(lightmap);
    #endif
    #ifdef LIL_USE_DYNAMICLIGHTMAP
        float4 dynlightmap = LIL_SAMPLE_LIGHTMAP(LIL_DYNAMICLIGHTMAP_TEX, LIL_DYNAMICLIGHTMAP_SAMP, uv);
        outCol += LIL_DECODE_DYNAMICLIGHTMAP(dynlightmap);
    #endif
    return outCol;
}

float3 lilGetVertexLights(float3 positionWS, float vertexLightStrength = 1.0)
{
    #ifdef LIL_BRP
        float4 toLightX = unity_4LightPosX0 - positionWS.x;
        float4 toLightY = unity_4LightPosY0 - positionWS.y;
        float4 toLightZ = unity_4LightPosZ0 - positionWS.z;

        float4 lengthSq = toLightX * toLightX + 0.000001;
        lengthSq += toLightY * toLightY;
        lengthSq += toLightZ * toLightZ;

        // Approximate _LightTextureB0
        float4 atten = saturate(saturate((25.0 - lengthSq * unity_4LightAtten0) * 0.111375) / (0.987725 + lengthSq * unity_4LightAtten0));

        float3 outCol;
        outCol =                   unity_LightColor[0].rgb * atten.x;
        outCol =          outCol + unity_LightColor[1].rgb * atten.y;
        outCol =          outCol + unity_LightColor[2].rgb * atten.z;
        outCol = saturate(outCol + unity_LightColor[3].rgb * atten.w);

        return outCol * vertexLightStrength;
    #else
        float3 outCol = 0.0;

        #ifdef _ADDITIONAL_LIGHTS_VERTEX
            uint lightsCount = GetAdditionalLightsCount();
            for (uint lightIndex = 0; lightIndex < lightsCount; lightIndex++)
            {
                Light light = GetAdditionalLight(lightIndex, positionWS);
                outCol += light.color * light.distanceAttenuation;
            }
        #endif

        return outCol * vertexLightStrength;
    #endif
}

float3 lilGetAdditionalLights(float3 positionWS)
{
    float3 outCol = 0.0;
    #ifdef _ADDITIONAL_LIGHTS
        uint lightsCount = GetAdditionalLightsCount();
        for (uint lightIndex = 0; lightIndex < lightsCount; lightIndex++)
        {
            Light light = GetAdditionalLight(lightIndex, positionWS);
            outCol += light.distanceAttenuation * light.shadowAttenuation * light.color;
        }
    #endif
    return outCol;
}

void CalcFur(inout float3 positionWS, float3 normalOS, float4 tangentOS, float4 color, float2 uv4)
{
    // Base
    float3 bitangentOS = cross(normalOS, tangentOS.xyz) * (tangentOS.w * LIL_NEGATIVE_SCALE);
    float3x3 tbnOS = float3x3(tangentOS.xyz, bitangentOS, normalOS);
    float3 vectorWS = mul((float3x3)LIL_MATRIX_M, mul(color.xyz, tbnOS));
    float furLength = length(vectorWS);

    // Motion
    float3 motionWS = float3(0.0, -_FurGravity, 0.0);
    #if defined(LIL_FUR_HQ)
    float3 wind1 = _FurWindMove1.xyz * sin(LIL_TIME * _FurWindFreq1.xyz + positionWS * _FurWindMove1.w);
    float3 wind2 = _FurWindMove2.xyz * sin(LIL_TIME * _FurWindFreq2.xyz + positionWS * _FurWindMove2.w);
        motionWS += wind1 + wind2;
        float softness = _FurSoftness * color.w;
        float motionStrength = pow(abs(uv4.y),1.0/max(softness,0.001)) * softness * furLength;
    #else
        float motionStrength = _FurSoftness * color.w * furLength;
    #endif

    // Touch
    #if defined(LIL_FUR_HQ) && defined(VERTEXLIGHT_ON)
        float3 vectorWS2 = normalize(vectorWS + motionWS * motionStrength) * (furLength * _FurLength * uv4.y);
        float3 positionWS2 = uv4.y > -0.5 ? positionWS + vectorWS2 : positionWS;
        float4 toLightX = unity_4LightPosX0 - positionWS2.x;
        float4 toLightY = unity_4LightPosY0 - positionWS2.y;
        float4 toLightZ = unity_4LightPosZ0 - positionWS2.z;
        float4 lengthSq = toLightX * toLightX + 0.000001;
        lengthSq += toLightY * toLightY;
        lengthSq += toLightZ * toLightZ;
        float4 atten = saturate(1.0 - lengthSq * unity_4LightAtten0 / 25.0) * _FurTouchStrength;
        motionWS = abs(unity_LightColor[0].a - 0.055) < 0.001 ? motionWS - float3(toLightX[0], toLightY[0], toLightZ[0]) * rsqrt(lengthSq[0]) * atten[0] : motionWS;
        motionWS = abs(unity_LightColor[1].a - 0.055) < 0.001 ? motionWS - float3(toLightX[1], toLightY[1], toLightZ[1]) * rsqrt(lengthSq[1]) * atten[1] : motionWS;
        motionWS = abs(unity_LightColor[2].a - 0.055) < 0.001 ? motionWS - float3(toLightX[2], toLightY[2], toLightZ[2]) * rsqrt(lengthSq[2]) * atten[2] : motionWS;
        motionWS = abs(unity_LightColor[3].a - 0.055) < 0.001 ? motionWS - float3(toLightX[3], toLightY[3], toLightZ[3]) * rsqrt(lengthSq[3]) * atten[3] : motionWS;
    #endif

    // Blend
    vectorWS = normalize(vectorWS + motionWS * motionStrength) * (furLength * _FurLength * uv4.y);

    positionWS = uv4.y > -0.5 ? positionWS + vectorWS : positionWS;
}

#endif