Shader "BotsOP/SimplePainter"{   


    SubShader{
        Cull Off ZWrite Off ZTest Off

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float _BrushSize;
            float _TimeColor;
            float3 _LastCursorPos;
            float3 _CursorPos;
            bool _FirstStroke;

            struct appdata{
                float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };
            
            float LineSegment3DSDF(const float3 p, const float3 a, const float3 b)
            {
                const float3 ba = b - a;
                const float3 pa = p - a;
                const float k = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * k);
            }

            v2f vert (appdata v){
                v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
				float4 uv = float4(0, 0, 0, 1);
                uv.xy = float2(1, _ProjectionParams.x) * (v.uv.xy * float2( 2, 2) - float2(1, 1));
				o.vertex = uv; 
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                const float paintColor = LineSegment3DSDF(i.worldPos, _LastCursorPos, _CursorPos);

                if(paintColor > _BrushSize) { return float4(0, 0, 0, 0); }
                
                return float4(_TimeColor, 0, 0, 0); 
            }
            ENDCG
        }
    }
}