// Sprites/Default plus a team-coloured outline drawn on the sprite's own edge pixels.
//
// The outline is drawn inward, over the outermost opaque pixels, not outside them. Almost every
// unit sprite under Assets/Resources/Game/sprites is trimmed to its alpha bounds (102 of 115 have
// under 3 px of transparent padding), so the quad has no room to draw anything outside the body.
//
// _OutlineWidth is measured in screen pixels through fwidth(uv), so a 256 px sprite and a 64 px
// sprite get the same visible thickness. _OutlineColor defaults to alpha 0: a renderer that copies
// this material without the property block (after-images, death fragments) draws no outline and
// looks exactly like Sprites/Default.
Shader "ArcaneCasters/SpriteTeamOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [PerRendererData] _OutlineColor ("Outline Color", Color) = (0,0,0,0)
        _OutlineWidth ("Outline Width (screen pixels)", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment OutlineFrag
            // fwidth needs derivatives, which shader model 2.0 does not guarantee.
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            // Outside the texture counts as transparent, so a sprite trimmed to its edge still
            // gets an outline along the texture border instead of the clamped edge texel.
            float AlphaAt(float2 uv)
            {
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                return SampleSpriteTexture(uv).a * inside;
            }

            fixed4 OutlineFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                float2 d = fwidth(IN.texcoord) * _OutlineWidth;
                float2 dd = d * 0.7071;
                float neighbour = 1.0;
                neighbour = min(neighbour, AlphaAt(IN.texcoord + float2(d.x, 0)));
                neighbour = min(neighbour, AlphaAt(IN.texcoord - float2(d.x, 0)));
                neighbour = min(neighbour, AlphaAt(IN.texcoord + float2(0, d.y)));
                neighbour = min(neighbour, AlphaAt(IN.texcoord - float2(0, d.y)));
                neighbour = min(neighbour, AlphaAt(IN.texcoord + dd));
                neighbour = min(neighbour, AlphaAt(IN.texcoord - dd));
                neighbour = min(neighbour, AlphaAt(IN.texcoord + float2(dd.x, -dd.y)));
                neighbour = min(neighbour, AlphaAt(IN.texcoord + float2(-dd.x, dd.y)));

                float self = SampleSpriteTexture(IN.texcoord).a;
                float edge = smoothstep(0.3, 0.7, self) * (1.0 - smoothstep(0.3, 0.7, neighbour));
                c.rgb = lerp(c.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);

                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
