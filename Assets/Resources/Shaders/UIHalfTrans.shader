Shader "Custom/SpriteHalfTrans"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TopAlpha ("上半透明度", Range(0,1)) = 1
        _BottomAlpha ("下半透明度", Range(0,1)) = 0.2
        _MidY ("分界Y", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float _TopAlpha, _BottomAlpha, _MidY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float a = lerp(_BottomAlpha, _TopAlpha, step(_MidY, i.uv.y));
                c.a *= a;
                return c;
            }
            ENDCG
        }
    }
}