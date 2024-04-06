Shader "Hidden/VolFx/Dither"
{    
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        ZClip false
            
        Pass    // 0
        {
            Name "Dither"
            
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_local PIXELATE _
            #pragma multi_compile_local DITHER NOISE
            //#pragma multi_compile_local _LUT_SIZE_X16 _LUT_SIZE_X32 _LUT_SIZE_X64
            
            #define LUT_SIZE 16.
            #define LUT_SIZE_MINUS (16. - 1.)
            
/*#if defined(_LUT_SIZE_X16)
            #define LUT_SIZE 16.
            #define LUT_SIZE_MINUS (16. - 1.)
#endif
#if defined(_LUT_SIZE_X32)
            #define LUT_SIZE 32.
            #define LUT_SIZE_MINUS (32. - 1.)
#endif
#if defined(_LUT_SIZE_X64)
            #define LUT_SIZE 64.
            #define LUT_SIZE_MINUS (64. - 1.)
#endif*/

            sampler2D _MainTex;
	        sampler2D _DitherTex;
            
	        sampler2D _PaletteTex;
	        sampler2D _QuantTex;
	        sampler2D _MeasureTex;
            
            uniform float  _Dither;
            uniform float  _Weight;
            
            uniform float4 _DitherMad;
            uniform float4 _PatternData;

            struct vert_in
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct frag_in
            {
                float2 uv  : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            frag_in vert(vert_in v)
            {
                frag_in o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }
                        
            half3 GetLinearToSRGB(half3 c)
            {
            #if _USE_FAST_SRGB_LINEAR_CONVERSION
                return FastLinearToSRGB(c);
            #else
                return LinearToSRGB(c);
            #endif
            }

            real3 GetSRGBToLinear(real3 c)
            {
            #if _USE_FAST_SRGB_LINEAR_CONVERSION
                return FastSRGBToLinear(c);
            #else
                return SRGBToLinear(c);
            #endif
            }
            
            float4 lut_sample(in float3 uvw, const sampler2D tex)
            {
                float2 uv;
                
                // get replacement color from the lut set
                uv.y = uvw.y * (LUT_SIZE_MINUS / LUT_SIZE) + .5 * (1. / LUT_SIZE);
                uv.x = uvw.x * (LUT_SIZE_MINUS / (LUT_SIZE * LUT_SIZE)) + .5 * (1. / (LUT_SIZE * LUT_SIZE)) + floor(uvw.z * LUT_SIZE) / LUT_SIZE;    

                float4 lutColor = tex2D(tex, uv);
                
#if !defined(UNITY_COLORSPACE_GAMMA)
                lutColor = float4(GetSRGBToLinear(lutColor.xyz), lutColor.w);
#endif

                return lutColor;
            }
            
            float4 grad_sample(in float2 uv, in float val, const sampler2D tex)
            {
                // xy - pix * aspect, z - sample scale, w - sample count
                uv.x *= _PatternData.z;
                uv.x += floor(val / _PatternData.z) * _PatternData.z;
                
                return tex2D(tex, uv);
            }
            
            half4 frag(frag_in i) : COLOR
            {
#if PIXELATE
                // pixelate screen dither related 
                float2 pix = float2(_PatternData.x, _PatternData.y);
                half4 col = tex2D(_MainTex, float2(floor((i.uv.x) * pix.x) / pix.x, floor((i.uv.y) * pix.y) / pix.y));
#else
                half4 col = tex2D(_MainTex, i.uv);
#endif

#if !defined(UNITY_COLORSPACE_GAMMA)
                float3 uvw = GetLinearToSRGB(col);
#else
                float3 uvw = col;
#endif
                
#if DITHER
                half4 plette  = lut_sample(uvw, _PaletteTex);
                float measure = lut_sample(uvw, _MeasureTex);
                float grade   = 1 - saturate(pow(1 - measure / _Dither, 4) + .001); // can be customized and remaped via curve (dither pattern sample)
                float noise   = grad_sample(frac(mad(i.uv, _DitherMad.xy, _DitherMad.zw)), grade, _DitherTex).r;

                
                half4 result = lerp(lut_sample(uvw, _PaletteTex), lut_sample(uvw, _QuantTex), step(measure, noise * _Dither));
                result.a *= col.a;
                
                return lerp(col, result, _Weight);
#endif
                          
#if NOISE
                float measure = lut_sample(uvw, _MeasureTex);
                float noise   = tex2D(_DitherTex, frac(mad(i.uv, _DitherMad.xy, _DitherMad.zw))).r;
                
                half4 result = lerp(lut_sample(uvw, _PaletteTex), lut_sample(uvw, _QuantTex), step(measure, noise * _Dither));
                result.a *= col.a;
                
                return lerp(col, result, _Weight);
                // return half4(lerp(col, lerp(lut_sample(uvw, _PaletteTex), lut_sample(uvw, _QuantTex), step(measure, _Dither) * noise), _Weight).rgb, col.a);
#endif
            }
            ENDHLSL
        }
    }
}