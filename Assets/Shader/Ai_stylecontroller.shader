Shader "Custom/Ai_stylecontroller"
{
    Properties
    {
        _Color("Color Tint",Color)=(1,1,1,1)
        _MainTex("Main Tex",2D)="white"{}

        _Stylization("Stylization",Range(0,1))=0
        _Darkness("Darkness",Range(0,1))=0
        _ColorTint("Color Tint 2",Color)=(1,1,1,1)

        _Specular("Specular",Color)=(1,1,1,1)
        _Gloss("Gloss",Range(8.0,256))=20
        _RampTex("Ramp Tex",2D)="white"{}

        _SpecStep("Spec Step",Range(0,1))=0.5

        _RampV("Ramp Position", Range(0,1)) = 0.5  
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 posWS       : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);

            float4 _Color;
            float _Stylization;
            float _Darkness;
            float4 _ColorTint;
            float4 _Specular;
            float _Gloss;
            float _SpecStep;
            float _RampV;   

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 normal = normalize(i.normalWS);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);

                float3 viewDir = normalize(GetCameraPositionWS() - i.posWS);
                float3 halfDir = normalize(lightDir + viewDir);

                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Color.rgb;

                // ===== 漫反射 =====
                float NdotL = saturate(dot(normal, lightDir));

                float halfLambert = 0.5 * NdotL + 0.5;
                float diff = lerp(NdotL, halfLambert, _Stylization);

                //  用上 _RampV（否则这个参数没意义）
                float2 rampUV = float2(diff, _RampV);
                float3 ramp = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV).rgb;

                float3 diffuse = mainLight.color * albedo * ramp;

                // ===== 高光 =====
                float specReal = pow(max(0, dot(normal, halfDir)), _Gloss);
                float specToon = step(_SpecStep, specReal);
                float spec = lerp(specReal, specToon, _Stylization);

                float3 specular = mainLight.color * _Specular.rgb * spec;

                // ===== 环境光 =====
                float3 ambient = SampleSH(normal) * albedo;

                float3 color = ambient + diffuse + specular;

                // ===== Darkness =====
                color = lerp(color, color * float3(0.5,0.65,0.75), _Darkness);
                color = pow(color, lerp(1.0,1.4,_Darkness));

                color *= _ColorTint.rgb;

                 // ===== Additional Lights =====
                int lightCount = 0;
                lightCount = GetAdditionalLightsCount();

                for (int li = 0; li < lightCount; li++)
                {
                    Light light = GetAdditionalLight(li, i.posWS);

                    float3 lightDir = normalize(light.direction);

                    float NdotL = saturate(dot(normal, lightDir));

                    float3 diffuseAdd = light.color * albedo * NdotL;

                    float3 halfDir = normalize(lightDir + viewDir);

                    float specReal = pow(max(0, dot(normal, halfDir)), _Gloss);
                    float specToon = step(_SpecStep, specReal);
                    float spec = lerp(specReal, specToon, _Stylization);

                    float3 specAdd = light.color * _Specular.rgb * spec;

                   float atten = light.distanceAttenuation * light.shadowAttenuation;
                    color += (diffuseAdd + specAdd) * atten;
                }
            
                return float4(color,1);
            }
           
            ENDHLSL
        }
    }
}