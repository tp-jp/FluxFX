#ifndef FLUXFX_LIMIT_VELOCITY_INCLUDED
#define FLUXFX_LIMIT_VELOCITY_INCLUDED

void FluxFX_LimitVelocity(inout FluxFXGpuContext ctx)
{
    if (_MaxSpeed <= 0)
    {
        return;
    }

    float speed = length(ctx.velocity);

    if (speed <= _MaxSpeed)
    {
        return;
    }

    ctx.velocity *= _MaxSpeed / speed;
}

#endif