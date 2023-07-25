Shader "Custom/Terrain"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        float minHeight;
        float maxHeight;

        struct Input
        {
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = float3(0,1,0);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
