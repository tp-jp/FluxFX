#include "Packages/com.tplab.flux.fx/Runtime/Shaders/Includes/ParticleCommon.hlsl"

void FluxFX_SpawnPosition(inout FluxSpawnPositionContext ctx)
{
    uint key = ctx.v_n_spawn_002e_key;
    float3 direction = FluxFXCreateSpawnDirection(key * 31u + 17u);
    float distance = pow(FluxFXRandom01(key * 37u + 23u), 1.0 / 3.0) * ctx.p_n_shape_002e_radius;

    ctx.v_n_particle_002e_position = ctx.p_n_spawn_002e_position + direction * distance;
    ctx.v_n_particle_002e_age = 0.0;
}
