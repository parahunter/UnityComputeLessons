Shader "Custom/BoidMeshShader" {
	Properties {
	}
	SubShader {
      Pass
    {
      Blend SrcAlpha OneMinusSrcAlpha
      Cull Off

        CGPROGRAM 
        #pragma target 5.0

        #pragma vertex vert
        #pragma fragment frag

        #include "UnityCG.cginc"

        struct Boid {
          float2 pos;
          float2 dir;
          float4 col;
        };

        // The buffer containing the points we want to draw.
        StructuredBuffer<Boid> BoidBuffer;
        float4 InvBounds;
        float4 Bounds;
        float4x4 My_Object2World;

        // A simple input struct for our pixel shader step containing a
        // position.
        struct ps_input {
          float4 pos : SV_POSITION;
          float4 col : COLOR0;
        };

        // Our vertex function simply fetches a point from the buffer
        // corresponding to the vertex index
        // which we transform with the view-projection matrix before passing to
        // the pixel program.
        ps_input vert(uint id : SV_VertexID, uint inst : SV_InstanceID) {
          ps_input o;

          Boid b = BoidBuffer[inst];

          float3 worldPos = float3(
            b.pos.x * InvBounds.x - Bounds.x*0.5f, 
            b.pos.y * InvBounds.y - Bounds.y*0.5f, 
            0);

          //float3 worldDir = normalize(float3(b.dir.x, b.dir.y, 0)) * 0.01f;
          float3 worldDir = float3(b.dir.x, b.dir.y, 0) * 0.002f;
          float3 upDir = float3(0, 0, -1) * 0.01f;
          float3 rightDir = normalize(cross(worldDir, float3(0, 0, 1))) * 0.01f;

          [branch] switch (id) {
          case 0: worldPos = worldPos - worldDir; break;
          case 1: worldPos = worldPos + upDir; break;
          case 2: worldPos = worldPos + rightDir; break;

          case 3: worldPos = worldPos - worldDir; break;
          case 4: worldPos = worldPos + upDir; break;
          case 5: worldPos = worldPos - rightDir; break;

          case 6: worldPos = worldPos - worldDir; break;
          case 7: worldPos = worldPos + rightDir; break;
          case 8: worldPos = worldPos - rightDir; break;

          case  9: worldPos = worldPos + upDir; break;
          case 10: worldPos = worldPos + rightDir; break;
          case 11: worldPos = worldPos - rightDir; break;
          };

          // o.pos = mul(UNITY_MATRIX_VP, mul(My_Object2World, float4(worldPos, 1.0f)) );
          o.pos = mul(UNITY_MATRIX_VP, mul(My_Object2World, float4(worldPos, 1.0f)));
          o.col = b.col;
          return o;
        }

        // Pixel function returns a solid color for each point.
        float4 frag(ps_input i) : COLOR{ return i.col; }

        ENDCG
      }
        }

        FallBack Off
}
