void FluxFX_Motion(inout FluxMotionContext ctx)
{
    ctx.v_n_particle_002e_position += ctx.v_n_particle_002e_velocity * ctx.v_n_step_002e_delta_002d_time;
    ctx.v_n_particle_002e_age += ctx.v_n_step_002e_delta_002d_time;
}
