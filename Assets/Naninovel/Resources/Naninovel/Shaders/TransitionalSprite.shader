// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

Shader "Naninovel/TransitionalSprite"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "black" {}
        _TransitionTex("Transition Texture", 2D) = "black" {}
        _DissolveTex("Dissolve Texture", 2D) = "black" {}
        _TransitionProgress("Transition Progress", Float) = 0
        _TransitionParams("Transition Parameters", Vector) = (1,1,1,1)
        _TintColor("Tint Color", Color) = (1,1,1,1)
        _Flip("Flip", Vector) = (1,1,1,1)
        _DepthAlphaCutoff("Alpha Cutoff", Range(0,1)) = 0.5
    }

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "NaninovelCG.cginc"
    #include "TransitionEffects.cginc"

    #pragma target 2.0
    #pragma multi_compile _ NANINOVEL_TRANSITION_BANDEDSWIRL NANINOVEL_TRANSITION_BLINDS NANINOVEL_TRANSITION_CIRCLEREVEAL NANINOVEL_TRANSITION_CIRCLESTRETCH NANINOVEL_TRANSITION_CLOUDREVEAL NANINOVEL_TRANSITION_CRUMBLE NANINOVEL_TRANSITION_DISSOLVE NANINOVEL_TRANSITION_DROPFADE NANINOVEL_TRANSITION_LINEREVEAL NANINOVEL_TRANSITION_PIXELATE NANINOVEL_TRANSITION_RADIALBLUR NANINOVEL_TRANSITION_RADIALWIGGLE NANINOVEL_TRANSITION_RANDOMCIRCLEREVEAL NANINOVEL_TRANSITION_RIPPLE NANINOVEL_TRANSITION_ROTATECRUMBLE NANINOVEL_TRANSITION_SATURATE NANINOVEL_TRANSITION_SHRINK NANINOVEL_TRANSITION_SLIDEIN NANINOVEL_TRANSITION_SWIRLGRID NANINOVEL_TRANSITION_SWIRL NANINOVEL_TRANSITION_WATER NANINOVEL_TRANSITION_WATERFALL NANINOVEL_TRANSITION_WAVE NANINOVEL_TRANSITION_CUSTOM

    sampler2D _MainTex, _TransitionTex, _DissolveTex, _CloudsTex;
    float _TransitionProgress, _DepthAlphaCutoff;
    float2 _RandomSeed;
    fixed4 _TintColor;
    float4 _TransitionParams, _Flip;

    struct VertexInput
    {
        float4 Vertex : POSITION;
        float4 Color : COLOR;
        float2 MainTexCoord : TEXCOORD0;
        float2 TransitionTexCoord : TEXCOORD1;
    };

    struct VertexOutput
    {
        float4 Vertex : SV_POSITION;
        fixed4 Color : COLOR;
        float2 MainTexCoord : TEXCOORD0;
        float2 TransitionTexCoord : TEXCOORD1;
    };

    VertexOutput ComputeVertex(VertexInput vertexInput)
    {
        VertexOutput vertexOutput;

        vertexInput.Vertex.xy *= _Flip.xy;
        vertexOutput.Vertex = UnityObjectToClipPos(vertexInput.Vertex);
        vertexOutput.MainTexCoord = vertexInput.MainTexCoord;
        vertexOutput.TransitionTexCoord = vertexInput.TransitionTexCoord;
        vertexOutput.Color = vertexInput.Color * _TintColor;

        return vertexOutput;
    }

    ENDCG

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off

        Pass
        {
            Name "Transparent"

            ZWrite Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            fixed4 ComputeFragment(VertexOutput vertexOutput) : SV_Target
            {
                fixed4 color = ApplyTransitionEffect(_MainTex, vertexOutput.MainTexCoord, _TransitionTex, vertexOutput.TransitionTexCoord, _TransitionProgress, _TransitionParams, _RandomSeed, _CloudsTex, _DissolveTex);
                color *= vertexOutput.Color;
                color.rgb *= color.a;
                return color;
            }

            ENDCG
        }

        Pass
        {
            Name "DepthMask"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0

            CGPROGRAM

            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment

            fixed4 ComputeFragment(VertexOutput vertexOutput) : SV_Target
            {
                fixed4 color = ApplyTransitionEffect(_MainTex, vertexOutput.MainTexCoord, _TransitionTex, vertexOutput.TransitionTexCoord, _TransitionProgress, _TransitionParams, _RandomSeed, _CloudsTex, _DissolveTex);
                color *= vertexOutput.Color;
                clip(color.a - _DepthAlphaCutoff);
                return color;
            }

            ENDCG
        }
    }
}
