﻿Shader "BotsOP/TexturePainter"{   

    Properties{
        _PainterColor ("Painter Color", Color) = (0, 0, 0, 0)
    }

    SubShader{
        Cull Off ZWrite Off ZTest Off

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			sampler2D _IDTex;
            float4 _IDTex_ST;
            
            float3 _PainterPosition;
            float _Radius;
            float _Hardness;
            float _Strength;
            float4 _PainterColor;
            float _PrepareUV;
            float _BrushSize;
            float _TimeColor;
            float _PreviousTimeColor;
            float3 _LastCursorPos;
            float _CursorPos;
            bool _FirstStroke;
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

            float2 ClosestPointOnLine(const float2 lineStart, const float2 lineEnd, const float2 position)
            {
                const float2 lineDirection = normalize(lineEnd - lineStart);
                const float2 positionRelativeToStart = position - lineStart;
                const float projectionDistance = dot(positionRelativeToStart, lineDirection);
                return lineStart + projectionDistance * lineDirection;
            }

            float cubicBezierCircle(float t)
            {
                return t * t * t;
            }

            float CalculatePaintColor(float2 paintPos, float2 startPos, float2 endPos)
            {
                const float2 paintPosOnLine = ClosestPointOnLine(startPos, endPos, paintPos);

                float distLine = distance(startPos, endPos);
                float distanceToPointA = distance(startPos, paintPosOnLine) / distLine;
                float paintColor = lerp(_PreviousTimeColor, _TimeColor, distanceToPointA);
                //paintColor = clamp(paintColor, _PreviousTimeColor, _TimeColor);
                
                float distToLine = distance(paintPosOnLine, paintPos);
                distToLine = 1 - saturate((cubicBezierCircle(distToLine / _BrushSize) * _BrushSize) / distLine);
                float paintColorOutside = (_TimeColor - _PreviousTimeColor) * distToLine;
                //paintColorOutside = clamp(paintColorOutside, 0, _TimeColor - _PreviousTimeColor);
                
                paintColor -= paintColorOutside;
                
                return paintColor;
            }

            float mask(float3 position, float3 center, float radius, float hardness){
                float m = distance(center, position);
                return 1 - smoothstep(radius * hardness, radius, m);    
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

            float4 frag (v2f i) : SV_Target{
                if(tex2D(_IDTex, i.uv).x == _StrokeID)
                {
                     return float4(-1, 0, 0, 0);
                }
                
                if(distance(i.worldPos, _LastCursorPos) < _BrushSize + 1)
                {
                    float2 AtoB = _CursorPos - _LastCursorPos;
                    float2 paintPosToA = _LastCursorPos - i.worldPos;
                    
                    if(dot(AtoB, paintPosToA) > 0.0 && !_FirstStroke)
                    {
                        return float4(-1, 0, 0, 0);
                    }
                }

                float paintColor = LineSegment3DSDF(i.worldPos, _LastCursorPos, _CursorPos);
                paintColor = 1 - smoothstep(_BrushSize, _BrushSize, paintColor);

                if(paintColor <= 0) { return float4(-1, 0, 0, 0); }

                return float4(_StrokeID, 0, 0, 0);
                
                // if(_PrepareUV > 0 ){
                //     return float4(0, 0, 1, 1);
                // }
                //
                // float4 col = tex2D(_MainTex, i.uv);
                // float f = mask(i.worldPos, _PainterPosition, _Radius, _Hardness);
                // float edge = f * _Strength;
                // return lerp(col, _PainterColor, edge);
            }
            ENDCG
        }
    }
}