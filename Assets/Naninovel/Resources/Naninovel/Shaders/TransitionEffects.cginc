// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

#ifndef NANINOVEL_TRANSITION_EFFECTS_INCLUDED
#define NANINOVEL_TRANSITION_EFFECTS_INCLUDED

#include "NaninovelCG.cginc"

inline fixed4 Crossfade(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress)
{
    const fixed4 CLIP_COLOR = fixed4(0, 0, 0, 0);
    fixed4 mainColor = Tex2DClip01(mainTex, mainUV, CLIP_COLOR);
    fixed4 transitionColor = Tex2DClip01(transitionTex, transitionUV, CLIP_COLOR);
    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 BandedSwirl(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float twistAmount, in float frequency)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = atan2(normToUV.y, normToUV.x);
	
    angle += sin(distanceFromCenter * frequency) * twistAmount * progress;
    float2 newUV;
    sincos(angle, newUV.y, newUV.x);
    newUV = newUV * distanceFromCenter + center;
	
    float4 mainColor = tex2D(mainTex, frac(newUV));
    float4 transitionColor = tex2D(transitionTex, transitionUV);

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 Blinds(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float count)
{
    if (frac(mainUV.y * count) < progress) return tex2D(transitionTex, transitionUV);
    else return tex2D(mainTex, mainUV);
}

inline fixed4 CircleReveal(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float fuzzyAmount)
{
    float radius = -fuzzyAmount + progress * (0.70710678 + 2.0 * fuzzyAmount);
    float fromCenter = length(mainUV - float2(0.5, 0.5));
    float distFromCircle = fromCenter - radius;
	
    float4 mainColor = tex2D(mainTex, mainUV); 
    float4 transitionColor = tex2D(transitionTex, transitionUV);
    
    float p = saturate((distFromCircle + fuzzyAmount) / (2.0 * fuzzyAmount));
    return lerp(transitionColor, mainColor, p);
}

inline fixed4 CircleStretch(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress)
{
    float2 center = float2(0.5, 0.5);
    float radius = progress * 0.70710678;
    float2 toUV = mainUV - center;
    float len = length(toUV);
    float2 normToUV = toUV / len;
	
    if (len < radius)
    {
        float distFromCenterToEdge = DistanceFromCenterToSquareEdge(normToUV) / 2.0;
        float2 edgePoint = center + distFromCenterToEdge * normToUV;
	
        float minRadius = min(radius, distFromCenterToEdge);
        float percentFromCenterToRadius = len / minRadius;
		
        float2 newUV = lerp(center, edgePoint, percentFromCenterToRadius);
        return tex2D(transitionTex, newUV);
    }
    else
    {
        float distFromCenterToEdge = DistanceFromCenterToSquareEdge(normToUV);
        float2 edgePoint = center + distFromCenterToEdge * normToUV;
        float distFromRadiusToEdge = distFromCenterToEdge - radius;
		
        float2 radiusPoint = center + radius * normToUV;
        float2 radiusToUV = mainUV - radiusPoint;
		
        float percentFromRadiusToEdge = length(radiusToUV) / distFromRadiusToEdge;
		
        float2 newUV = lerp(center, edgePoint, percentFromRadiusToEdge);
        return tex2D(mainTex, newUV);
    }
}

inline fixed4 CloudReveal(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in sampler2D cloudsTex)
{
    float cloud = tex2D(cloudsTex, mainUV).r;
    float4 mainColor = tex2D(mainTex, mainUV);
    float4 transitionColor = tex2D(transitionTex, transitionUV);
	
    float a;
	
    if (progress < 0.5) a = lerp(0.0, cloud, progress / 0.5);
    else a = lerp(cloud, 1.0, (progress - 0.5) / 0.5);
	
    return (a < 0.5) ? mainColor : transitionColor;
}

inline fixed4 Crumble(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float randomSeed, in sampler2D cloudsTex)
{
    float2 offset = tex2D(cloudsTex, float2(mainUV.x / 5, frac(mainUV.y / 5 + min(0.9, randomSeed)))).xy * 2.0 - 1.0;
    float p = progress * 2;
    if (p > 1.0) p = 1.0 - (p - 1.0);

    float4 mainColor = tex2D(mainTex, frac(mainUV + offset * p));
    float4 transitionColor = tex2D(transitionTex, frac(transitionUV + offset * p));

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 Dissolve(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float step)
{
    const fixed4 CLIP_COLOR = fixed4(0, 0, 0, 0);
    float noise = (PerlinNoise(mainUV * step) + 1.0) / 2.0 ;
	if (noise > progress) return Tex2DClip01(mainTex, mainUV, CLIP_COLOR);
    else return Tex2DClip01(transitionTex, transitionUV, CLIP_COLOR);
}

inline fixed4 DropFade(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float randomSeed, in sampler2D cloudsTex)
{
    const fixed4 CLIP_COLOR = fixed4(0, 0, 0, 0);
    float offset = tex2D(cloudsTex, float2(mainUV.x / 5, randomSeed)).x;
    float4 mainColor = Tex2DClip01(mainTex, float2(mainUV.x, mainUV.y + offset * progress), CLIP_COLOR);
    float4 transitionColor = tex2D(transitionTex, transitionUV);

    if (mainColor.a <= 0.0) return transitionColor;
    else return lerp(mainColor, transitionColor, progress);
}

inline fixed4 LineReveal(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float fuzzyAmount, in float2 lineNormal)
{
    float2 lineOrigin = float2(0.0, 0.0);
    float2 lineOffset = float2(1.0, 1.0);

    float2 currentLineOrigin = lerp(lineOrigin, lineOffset, progress);
    float2 normLineNormal = normalize(lineNormal);
    float4 mainColor = tex2D(mainTex, mainUV);
    float4 transitionColor = tex2D(transitionTex, transitionUV);
    
    float distFromLine = dot(normLineNormal, mainUV - currentLineOrigin);
    float p = saturate((distFromLine + fuzzyAmount) / (2.0 * fuzzyAmount));
    return lerp(transitionColor, mainColor, p);
}

inline fixed4 Pixelate(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress)
{
    float pixels;
    float segmentProgress;

    if (progress < 0.5) segmentProgress = 1 - progress * 2;
    else segmentProgress = (progress - 0.5) * 2;
    
    pixels = 5 + 1000 * segmentProgress * segmentProgress;
    float2 newMainUV = round(mainUV * pixels) / pixels;
    float2 newTransitionUV = round(transitionUV * pixels) / pixels;
	
    fixed4 mainColor = tex2D(mainTex, newMainUV);
    fixed4 transitionColor = tex2D(transitionTex, newTransitionUV);

    float lerpProgress = saturate((progress - 0.4) / 0.2);
    return lerp(mainColor, transitionColor, lerpProgress);	
}

inline fixed4 RadialBlur(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float2 normToUV = toUV;
	
    float4 mainColor = float4(0, 0, 0, 0);
    int count = 24;
    float s = progress * 0.02;
	
    for (int i = 0; i < count; i++)
    {
        mainColor += tex2D(mainTex, mainUV - normToUV * s * i); 
    }
	
    mainColor /= count;
    float4 transitionColor = tex2D(transitionTex, transitionUV);

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 RadialWiggle(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float randomSeed, in sampler2D cloudsTex)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(mainUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = (atan2(normToUV.y, normToUV.x) + 3.141592) / (2.0 * 3.141592);
    float offset1 = tex2D(cloudsTex, float2(angle, frac(progress / 3 + distanceFromCenter / 5 + randomSeed))).x * 2.0 - 1.0;
    float offset2 = offset1 * 2.0 * min(0.3, (1 - progress)) * distanceFromCenter;
    offset1 = offset1 * 2.0 * min(0.3, progress) * distanceFromCenter;
	
    float4 mainColor = tex2D(mainTex, frac(center + normToUV * (distanceFromCenter + offset1))); 
    float4 transitionColor = tex2D(transitionTex, frac(center + normToUV * (distanceFromCenter + offset2)));

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 RandomCircleReveal(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float randomSeed, in sampler2D cloudsTex)
{
    float radius = progress * 0.70710678;
    float2 fromCenter = mainUV - float2(0.5, 0.5);
    float len = length(fromCenter);
	
    float2 toUV = normalize(fromCenter);
    float angle = (atan2(toUV.y, toUV.x) + 3.141592) / (2.0 * 3.141592);
    radius += progress * tex2D(cloudsTex, float2(angle, frac(randomSeed + progress / 5.0))).r;
	
    if (len < radius) return tex2D(transitionTex, transitionUV);
    else return tex2D(mainTex, mainUV);
}

inline fixed4 Ripple(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float frequency, in float speed, in float amplitude)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;

    float wave = cos(frequency * distanceFromCenter - speed * progress);
    float offset1 = progress * wave * amplitude;
    float offset2 = (1.0 - progress) * wave * amplitude;
	
    float2 newUV1 = center + normToUV * (distanceFromCenter + offset1);
    float2 newUV2 = center + normToUV * (distanceFromCenter + offset2);
	
    float4 mainColor = tex2D(mainTex, newUV1); 
    float4 transitionColor = tex2D(transitionTex, newUV2);

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 RotateCrumble(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float randomSeed, in sampler2D cloudsTex)
{
    float2 offset = (tex2D(cloudsTex, float2(mainUV.x / 10, frac(mainUV.y / 10 + min(0.9, randomSeed)))).xy * 2.0 - 1.0);
    float2 center = mainUV + offset / 10.0;
    float2 toUV = mainUV - center;
    float len = length(toUV);
    float2 normToUV = toUV / len;
    float angle = atan2(normToUV.y, normToUV.x);
	
    angle += 3.141592 * 2.0 * progress;
    float2 newOffset;
    sincos(angle, newOffset.y, newOffset.x); 
    newOffset *= len;
	
    float4 mainColor = tex2D(mainTex, frac(center + newOffset));
    float4 transitionColor = tex2D(transitionTex, frac(center + newOffset));

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 Saturate(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress)
{
    float4 mainColor = tex2D(mainTex, mainUV); 
    mainColor = saturate(mainColor * (2 * progress + 1));
    float4 transitionColor = tex2D(transitionTex, transitionUV);

    if (progress > 0.8)
    {
        float p = (progress - 0.8) * 5.0;
        return lerp(mainColor, transitionColor, p);
    }
    else return mainColor;
}

inline fixed4 Shrink(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float speed)
{
    const fixed4 CLIP_COLOR = fixed4(0, 0, 0, 0);
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;
	
    float2 newUV = center + normToUV * (distanceFromCenter * (progress * speed + 1));
    float4 mainColor = Tex2DClip01(mainTex, newUV, CLIP_COLOR); 
    if (mainColor.a <= 0) mainColor = tex2D(transitionTex, transitionUV);
	
    return mainColor;
}

inline fixed4 SlideIn(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float2 slideAmount)
{
    mainUV += slideAmount * progress;
    if (any(saturate(mainUV) - mainUV))
    {	
        mainUV = frac(mainUV);
        return tex2D(transitionTex, mainUV);
    }
    else return tex2D(mainTex, mainUV);
}

inline fixed4 SwirlGrid(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float twistAmount, in float cellCount)
{
    float cellSize = 1.0 / cellCount;
	
    float2 cell = floor(mainUV * cellCount);
    float2 oddeven = fmod(cell, 2.0);
    float cellTwistAmount = twistAmount;
    if (oddeven.x < 1.0) cellTwistAmount *= -1;
    if (oddeven.y < 1.0) cellTwistAmount *= -1;
	
    float2 newUV = frac(mainUV * cellCount);
	
    float2 center = float2(0.5, 0.5);
    float2 toUV = newUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = atan2(normToUV.y, normToUV.x);
	
    angle += max(0, 0.5 - distanceFromCenter) * cellTwistAmount * progress;
    float2 newUV2;
    sincos(angle, newUV2.y, newUV2.x);
    newUV2 *= distanceFromCenter;
    newUV2 += center;
	
    newUV2 *= cellSize;
    newUV2 += cell * cellSize;
	
    float4 mainColor = tex2D(mainTex, newUV2);
    float4 transitionColor = tex2D(transitionTex, transitionUV);

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 Swirl(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float twistAmount)
{
    const fixed4 CLIP_COLOR = fixed4(0, 0, 0, 0);
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = atan2(normToUV.y, normToUV.x);
	
    angle += distanceFromCenter * distanceFromCenter * twistAmount * progress;
    float2 newUV;
    sincos(angle, newUV.y, newUV.x);
    newUV *= distanceFromCenter;
    newUV += center;
	
    float4 mainColor = Tex2DClip01(mainTex, newUV, CLIP_COLOR);
    float4 transitionColor = Tex2DClip01(transitionTex, transitionUV, CLIP_COLOR);

    return lerp(mainColor, transitionColor, progress);
}

inline fixed4 Water(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float randomSeed, in sampler2D cloudsTex)
{
    float2 offset = tex2D(cloudsTex, float2(mainUV.x / 10, frac(mainUV.y / 10 + min(0.9, randomSeed)))).xy * 2.0 - 1.0;
    float4 mainColor = tex2D(mainTex, frac(mainUV + offset * progress));
    float4 transitionColor = tex2D(transitionTex, transitionUV);

    if (mainColor.a <= 0.0) return transitionColor;
    else return lerp(mainColor, transitionColor, progress);
}

inline fixed4 Waterfall(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float randomSeed, in sampler2D cloudsTex)
{
    float offset = 1 - min(progress + progress * tex2D(cloudsTex, float2(mainUV.x, randomSeed)).r, 1.0);
    mainUV.y -= offset;
    transitionUV.y -= offset;
	
    if (mainUV.y > 0.0) return tex2D(transitionTex, transitionUV);
    else return tex2D(mainTex, frac(mainUV));
}

inline fixed4 Wave(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float magnitude, in float phase, in float frequency)
{
    const fixed4 CLIP_COLOR = fixed4(0, 0, 0, 0);
	float2 newUV = mainUV + float2(magnitude * progress * sin(frequency * mainUV.y + phase * progress), 0);
	
	float4 mainColor = Tex2DClip01(mainTex, newUV, CLIP_COLOR);
    float4 transitionColor = Tex2DClip01(transitionTex, transitionUV, CLIP_COLOR);

    return lerp(mainColor, transitionColor, progress);
}

// Executes transition effect based on enabled keyword.
// Returns resulting color of the transition at the given texture coordinates.
inline fixed4 ApplyTransitionEffect(in sampler2D mainTex, in float2 mainUV, in sampler2D transitionTex, in float2 transitionUV, in float progress, in float4 params, in float2 randomSeed, in sampler2D cloudsTex, in sampler2D customTex)
{
    #ifdef NANINOVEL_TRANSITION_BANDEDSWIRL
    return BandedSwirl(mainTex, mainUV, transitionTex, transitionUV, progress, params.x, params.y);
    #endif

    #ifdef NANINOVEL_TRANSITION_BLINDS
    return Blinds(mainTex, mainUV, transitionTex, transitionUV, progress, params.x);
    #endif
    
    #ifdef NANINOVEL_TRANSITION_CIRCLEREVEAL
    return CircleReveal(mainTex, mainUV, transitionTex, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_CIRCLESTRETCH
    return CircleStretch(mainTex, mainUV, transitionTex, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_CLOUDREVEAL
    return CloudReveal(mainTex, mainUV, transitionTex, transitionUV, progress, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_CRUMBLE
    return Crumble(mainTex, mainUV, transitionTex, transitionUV, progress, randomSeed.x, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_DISSOLVE
    return Dissolve(mainTex, mainUV, transitionTex, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_DROPFADE
    return DropFade(mainTex, mainUV, transitionTex, transitionUV, progress, randomSeed.x, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_LINEREVEAL
    return LineReveal(mainTex, mainUV, transitionTex, transitionUV, progress, params.x, params.yz);
    #endif

    #ifdef NANINOVEL_TRANSITION_PIXELATE
    return Pixelate(mainTex, mainUV, transitionTex, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_RADIALBLUR
    return RadialBlur(mainTex, mainUV, transitionTex, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_RADIALWIGGLE
    return RadialWiggle(mainTex, mainUV, transitionTex, transitionUV, progress, randomSeed.x, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_RANDOMCIRCLEREVEAL
    return RandomCircleReveal(mainTex, mainUV, transitionTex, transitionUV, progress, randomSeed.x, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_RIPPLE
    return Ripple(mainTex, mainUV, transitionTex, transitionUV, progress, params.x, params.y, params.z);
    #endif

    #ifdef NANINOVEL_TRANSITION_ROTATECRUMBLE
    return RotateCrumble(mainTex, mainUV, transitionTex, transitionUV, progress, randomSeed.x, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_SATURATE
    return Saturate(mainTex, mainUV, transitionTex, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_SHRINK
    return Shrink(mainTex, mainUV, transitionTex, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_SLIDEIN
    return SlideIn(mainTex, mainUV, transitionTex, transitionUV, progress, params.xy);
    #endif

    #ifdef NANINOVEL_TRANSITION_SWIRLGRID
    return SwirlGrid(mainTex, mainUV, transitionTex, transitionUV, progress, params.x, params.y);
    #endif

    #ifdef NANINOVEL_TRANSITION_SWIRL
    return Swirl(mainTex, mainUV, transitionTex, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_WATER
    return Water(mainTex, mainUV, transitionTex, transitionUV, progress, randomSeed.x, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_WATERFALL
    return Waterfall(mainTex, mainUV, transitionTex, transitionUV, progress, randomSeed.x, cloudsTex);
    #endif

    #ifdef NANINOVEL_TRANSITION_WAVE
    return Wave(mainTex, mainUV, transitionTex, transitionUV, progress, params.x, params.y, params.z);
    #endif

    #ifdef NANINOVEL_TRANSITION_CUSTOM
    return progress >= tex2D(customTex, transitionUV).r ? tex2D(transitionTex, transitionUV) : tex2D(mainTex, mainUV);
    #endif

    // When no transition keywords enabled default to crossfade.
    return Crossfade(mainTex, mainUV, transitionTex, transitionUV, progress);
}

#endif // NANINOVEL_TRANSITION_EFFECTS_INCLUDED
