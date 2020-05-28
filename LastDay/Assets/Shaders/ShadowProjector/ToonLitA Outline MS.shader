﻿Shader "ME/Toon/LitA Outline MS"
{
    Properties
    {
        [Header(Light Settings)]
        _Color("Color", Color) = (0.6,0.6,0.6,1.0)
        _HColor("Highlight Color", Color) = (1.0,1.0,1.0,1.0)
        _SColor("Shadow Color", Color) = (0.2,0.2,0.2,1.0)

		[Toggle(TOON_RAMP_TEX)] _RampTex("Ramp Texture?", Float) = 0
		_Ramp("Ramp Tex", 2D) = "gray" {}
        _RampThreshold("Ramp Threshold", Range(0,1)) = 0.5
        _RampSmooth("Ramp Smoothing", Range(0.01,1)) = 0.1		

        [Header(Texture Settings)]
        _MainTex("Main Texture (RGB)", 2D) = "white" {}
        [Toggle(SET_GRAYSCALE)] _Grayscale("Grayscale?", Float) = 0
        _AlphaTex("Alpha (A)", 2D) = "white" {}
        _Cutoff("Alpha Cut", Range(0,1)) = 0.5
		_SkinTex("Skin (RGB)", 2D) = "black" {}
		_SkinCut("Skin Cut", Range(0,1)) = 0.1

        [Header(Outline Settings)]
        _OutlineColor("Outline Color", Color) = (0.2, 0.2, 0.2, 1.0)
        _Outline("Outline Width", Float) = 1
        [Toggle(CONST_WIDTH)] _ConstWidth("ConstWidth?", Float) = 0
        
        _ZSmooth("Z Correction", Range(-3.0,3.0)) = 0

        //Z Offset
        _Offset1("Z Offset 1", Float) = 0
        _Offset2("Z Offset 2", Float) = 0

		_ShadowAlpha ("Shadow Alpha", Range(0,1)) = 0.5
		_GroundY ("GroundY", float) = 0
    }

    SubShader
    {
        LOD 200
        
        UsePass "ME/Toon/LitA/BASE"
        UsePass "ME/Toon/Outline(Shader Model 2)/OUTLINE"
		UsePass "Hidden/Toon/MESH SHADOW"
    }

    //Fallback "Diffuse"
}