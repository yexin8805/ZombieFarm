﻿Shader "ME/Toon/LitA Outline"
{
    Properties
    {
        [HideInInspector]_Color("Color", Color) = (0.6,0.6,0.6,1.0)
        [HideInInspector]_HColor("Highlight Color", Color) = (1.0,1.0,1.0,1.0)
        [HideInInspector]_SColor("Shadow Color", Color) = (0.2,0.2,0.2,1.0)

		[HideInInspector]_Ramp("Ramp Tex", 2D) = "gray" {}
        [HideInInspector]_RampThreshold("Ramp Threshold", Range(0,1)) = 0.5
        [HideInInspector]_RampSmooth("Ramp Smoothing", Range(0.01,1)) = 0.1

        [HideInInspector]_MainTex("Main (RGBA)", 2D) = "white" {}

        [HideInInspector]_AlphaTex("Alpha (A)", 2D) = "white" {}
        [HideInInspector]_Cutoff("Alpha Cut", Range(0,1)) = 0.5

		[HideInInspector]_SkinTex("Skin (RGB)", 2D) = "black" {}
		[HideInInspector]_SkinCut("Skin Cut", Range(0,1)) = 0.1

        [HideInInspector]_HairUV("Hair UV", Vector) = (0, 0, 1, 1)
        [HideInInspector]_HairColor("Hair Color", Color) = (1, 1, 1, 1)
        [HideInInspector]_HairSpecular("Hair Specular", Range(0, 10)) = 1
        [HideInInspector]_HairShininess("Hair Shininess", Range(0.01, 1)) = 0.5
        
        [HideInInspector]_AtlasUV("UV in Atlas", Vector) = (0, 0, 1, 1)
        
        [HideInInspector]_OutlineColor("Outline Color", Color) = (0.2, 0.2, 0.2, 1.0)
        [HideInInspector]_Outline("Outline Width", Float) = 1

        [HideInInspector]_ZSmooth("Z Correction", Range(-3.0,3.0)) = 0
        [HideInInspector]_Offset1("Z Offset 1", Float) = 0
        [HideInInspector]_Offset2("Z Offset 2", Float) = 0
        
        [HideInInspector]_AlphaGridTex ("Alpha Grid", 2D) = "white" {}
        
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Int) = 1
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Int) = 0

        _StencilRef ("Stencil Ref", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPassOp ("Stencil Pass Op", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFailOp ("Stencil ZFail Op", Int) = 0
    }

    SubShader
    {
        LOD 200

        UsePass "ME/Toon/LitA/BASE"
        UsePass "ME/Toon/Outline(Shader Model 2)/OUTLINE"
		UsePass "Hidden/Toon/SHADOWCASTER"
    }

    //Fallback "Diffuse"
    CustomEditor "METoonShaderEditor"
}