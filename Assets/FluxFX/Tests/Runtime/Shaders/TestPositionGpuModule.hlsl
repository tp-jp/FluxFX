#ifndef FLUXFX_TEST_POSITION_GPU_MODULE_INCLUDED
#define FLUXFX_TEST_POSITION_GPU_MODULE_INCLUDED

void FluxFX_TestPosition(inout FluxFXGpuContext ctx)
{
    ctx.position.y += _TestPositionStrength * ctx.deltaTime;
}

#endif