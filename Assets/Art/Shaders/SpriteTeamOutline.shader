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

            // 12 directions on 3 rings (1/3, 2/3 and all of _OutlineWidth). One ring of 8, as this
            // shader first had, leaves about 2.3 screen pixels between samples at width 3, so gaps
            // between them went unseen and the outline came out thicker in some directions than
            // others. Even rings are turned by half a step so no two rings sample the same angle.
            // 16 directions on 4 rings rendered the same offline at twice the cost.
            #define OUTLINE_DIRECTIONS 12
            #define OUTLINE_RINGS 3

            fixed4 OutlineFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                float2 pixel = fwidth(IN.texcoord);
                float gap = 0.0;
                [unroll]
                for (int ring = 1; ring <= OUTLINE_RINGS; ring++)
                {
                    float radius = _OutlineWidth * ring / OUTLINE_RINGS;
                    // Transparency found at the outermost ring counts half, which anti-aliases the
                    // inner edge of the outline instead of cutting it at a whole pixel.
                    float weight = saturate(_OutlineWidth + 0.5 - radius);
                    float phase = 0.5 - frac(ring * 0.5);
                    [unroll]
                    for (int i = 0; i < OUTLINE_DIRECTIONS; i++)
                    {
                        float angle = (i + phase) * (6.2831853 / OUTLINE_DIRECTIONS);
                        float2 offset = float2(cos(angle), sin(angle)) * pixel * radius;
                        gap = max(gap, (1.0 - AlphaAt(IN.texcoord + offset)) * weight);
                    }
                }
                float edge = smoothstep(0.2, 0.8, gap) * _OutlineColor.a;
                // No threshold on the pixel's own alpha: the half-transparent rim of the sprite
                // takes the outline colour too, instead of leaving a fringe of body colour outside it.
                c.rgb = lerp(c.rgb, _OutlineColor.rgb, edge);

                // The sprites have no mipmaps and are drawn smaller than their texture, so one
                // bilinear sample per pixel leaves a stair-stepped silhouette. Four samples inside
                // the pixel give its coverage. Without an outline colour the alpha stays the
                // single sample, so after-images still look exactly like Sprites/Default.
                float coverage = 0.25 * (AlphaAt(IN.texcoord + pixel * float2(0.125, 0.375))
                    + AlphaAt(IN.texcoord + pixel * float2(-0.375, 0.125))
                    + AlphaAt(IN.texcoord + pixel * float2(-0.125, -0.375))
                    + AlphaAt(IN.texcoord + pixel * float2(0.375, -0.125)));
                c.a = lerp(c.a, coverage * IN.color.a, _OutlineColor.a);

                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
