#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnVelocity(inout FluxSpawnVelocityContext ctx)
{
    ctx.v_n_particle_002e_velocity = FluxFXCreateSpawnDirection(ctx.v_n_spawn_002e_key) * ctx.p_n_spawn_002e_speed;
    ctx.v_n_particle_002e_spawn_002d_key = ctx.v_n_spawn_002e_key;
}
