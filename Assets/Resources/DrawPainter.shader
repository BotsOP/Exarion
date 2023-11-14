Shader "BotsOP/TexturePainter"{   



    SubShader{
        Cull Off ZWrite Off ZTest Off

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			sampler2D _IDTex;
            float4 _IDTex_ST;
            
            float _BrushSize;
            float _TimeColor;
            float _PreviousTimeColor;
            float3 _LastCursorPos;
            float3 _CursorPos;
            int _FirstStroke;
            float _StrokeID;

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
            
            float sdCapsule( float3 p, float3 a, float3 b, float r )
            {
              float3 pa = p - a;
              float3 ba = b - a;
              float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
              return length( pa - ba*h ) - r;
            }

            float3 ClosestPointOnLine(const float3 lineStart, const float3 lineEnd, const float3 position)
            {
                const float3 lineDirection = normalize(lineEnd - lineStart);
                const float3 positionRelativeToStart = position - lineStart;
                const float projectionDistance = dot(positionRelativeToStart, lineDirection);
                return lineStart + projectionDistance * lineDirection;
            }

            float cubicBezierCircle(float t)
            {
                return sqrt(1 - pow(t - 0.9, 2));
            }

            float CalculatePaintColor(float3 paintPos, float3 startPos, float3 endPos)
            {
                const float3 paintPosOnLine = ClosestPointOnLine(startPos, endPos, paintPos);

                float3 directionPos = paintPos - startPos;
                float3 directionLine = endPos - startPos;
                float dotDirection = dot(directionPos, directionLine);
                dotDirection = ceil(dotDirection);

                float distLine = distance(startPos, endPos) + _BrushSize * dotDirection;
                float distanceToPointA = (distance(startPos, paintPosOnLine) + _BrushSize) / distLine;
                float paintColor = lerp(_PreviousTimeColor, _TimeColor, distanceToPointA);

                float distToLine = distance(paintPosOnLine, paintPos);
                distToLine = 1 - distToLine / _BrushSize;
                float paintColorOutside = lerp(0, _TimeColor - _PreviousTimeColor, cubicBezierCircle(distToLine));
                
                paintColor -= paintColorOutside;
                
                return paintColor;
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

            float frag (v2f i) : SV_Target {
                // if(tex2D(_IDTex, i.uv).x >= 0)
                // {
                //      return 0;
                // }

                float paintColor = LineSegment3DSDF(i.worldPos, _LastCursorPos, _CursorPos);

                if(paintColor > _BrushSize)
                {
                    return 0;
                }

                if(_FirstStroke == 1)
                {
                    return _PreviousTimeColor;
                }

                paintColor = CalculatePaintColor(i.worldPos, _LastCursorPos, _CursorPos);

                return paintColor;
            }
            ENDCG
        }
    }
}