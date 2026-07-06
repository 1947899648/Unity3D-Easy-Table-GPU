Shader "WPZ0325/TableFont"
{
    Properties
    {
        _FontTex ("Font Texture", 2D) = "white" {}
        _ClipRect ("Clip Rect (x=minX, y=minY, z=maxX, w=maxY)", Vector) = (0,0,0,0)
        _SDF_Threshold ("SDF Threshold", Range(0, 1)) = 0.5
        _SDF_Spread ("SDF Spread", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float2 uv2    : TEXCOORD1;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float2 uv2      : TEXCOORD1;
                fixed4 color    : COLOR;
                float3 localPos : TEXCOORD2;
            };

            sampler2D _FontTex;
            float4 _ClipRect;
            float _SDF_Threshold;
            float _SDF_Spread;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv2 = v.uv2;
                o.color = v.color;
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (i.localPos.x < _ClipRect.x || i.localPos.x > _ClipRect.z ||
                    i.localPos.y < _ClipRect.y || i.localPos.y > _ClipRect.w)
                    discard;

                if (i.uv2.x < 0.5)
                    return i.color;
                else
                {
                float distance = tex2D(_FontTex, i.uv).a;
                float spread = fwidth(distance) + _SDF_Spread;
                float a = smoothstep(_SDF_Threshold - spread, _SDF_Threshold + spread, distance);
                return fixed4(i.color.rgb, i.color.a * a);
                }
            }
            ENDCG
        }
    }
}
