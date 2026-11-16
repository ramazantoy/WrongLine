#ifndef WRONGLINE_CURVE_WORLD_INCLUDED
#define WRONGLINE_CURVE_WORLD_INCLUDED

// The world bend. The same formula lives in C# as WrongLine.World.CurveBend.Drop: change one, change the other.
// Set once per frame by CurveWorldDriver, never per material:
//   _WL_CurveOrigin.xyz  the camera's position in scene space
//   _WL_CurveParams.x    distance from the camera where the bend starts (metres)
//   _WL_CurveParams.y    metres of sinking per metre-squared beyond the start (0 = no bend)
float4 _WL_CurveOrigin;
float4 _WL_CurveParams;

// Sinks a world-space position by its ground distance from the camera. Everything that draws or casts a shadow with a
// curved material must bend the same way, or shadows and depth drift off the picture.
float3 WL_BendWorldPosition(float3 positionWS)
{
    float2 offset = positionWS.xz - _WL_CurveOrigin.xz;
    float beyond = max(0.0, length(offset) - _WL_CurveParams.x);
    positionWS.y -= _WL_CurveParams.y * beyond * beyond;
    return positionWS;
}

#endif
