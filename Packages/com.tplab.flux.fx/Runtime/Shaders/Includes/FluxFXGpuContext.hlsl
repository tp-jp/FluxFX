#ifndef FLUXFX_GPU_CONTEXT_INCLUDED
#define FLUXFX_GPU_CONTEXT_INCLUDED

struct FluxFXGpuContext
{
    float3 position;
    float3 velocity;

    float age;
    float lifetime;

    float deltaTime;
    float simulationTime;
};

#endif