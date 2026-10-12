void FluxFX_Drag(inout FluxDragContext ctx)
{
    ctx.v_n_particle_002e_velocity *= exp(-max(ctx.p_n_drag, 0.0) * ctx.v_n_step_002e_delta_002d_time);
}
