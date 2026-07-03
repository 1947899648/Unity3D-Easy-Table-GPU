Shader "WPZ0325/TableFont"
{
    Properties
    {
        _FontTex ("Font Texture", 2D) = "white" {}
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
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float2 uv2    : TEXCOORD1;
                fixed4 color  : COLOR;
            };

            sampler2D _FontTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv2 = v.uv2;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (i.uv2.x < 0.5)
                    return i.color;
                else
                {
                    fixed a = tex2D(_FontTex, i.uv).a;
                    return fixed4(i.color.rgb, i.color.a * a);
                }
            }
            ENDCG
        }
    }
}
