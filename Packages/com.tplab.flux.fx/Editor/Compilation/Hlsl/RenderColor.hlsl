void FluxFX_RenderColor(inout FluxContext_n_render_002e_color ctx)
{
    float normalizedAge = saturate(ctx.v_n_particle_002e_age / max(ctx.v_n_particle_002e_lifetime, 1e-6));
    ctx.v_n_render_002e_color = lerp(ctx.v_n_render_002e_color, ctx.p_n_color_002e_end, normalizedAge);
}
