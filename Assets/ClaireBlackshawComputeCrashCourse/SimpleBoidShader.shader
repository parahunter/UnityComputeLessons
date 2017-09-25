// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/SimpleVertColour" {
  Properties {
  }
    
  SubShader {
    Pass {

      Cull Off
      CGPROGRAM

      #pragma target 3.0
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct v2f
      {
        float4 pos : SV_POSITION;
        float4 col : COLOR; 
      };

      v2f vert(float4 vertex : POSITION, float4 col : COLOR) {
        v2f o;
        o.pos = UnityObjectToClipPos(vertex);
        o.col = col;
        return o;
      }
      
      fixed4 frag(v2f i) : SV_Target 
      {
        fixed4 c = i.col;
        return c;
      }
    ENDCG
  }
  }
}