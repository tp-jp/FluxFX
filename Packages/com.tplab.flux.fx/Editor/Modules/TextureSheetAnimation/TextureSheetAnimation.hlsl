void FluxFX_TextureSheetAnimation(inout FluxRenderTextureSheetAnimationContext ctx)
{
    float age = saturate(ctx.valueParticleAge / max(ctx.valueParticleLifetime, 1e-6));
    float2 tiles = max(1, floor(ctx.parameterTextureSheetTiles));
    float frameCount = tiles.x * tiles.y;
    float frame = min(floor(frac(age * max(0, ctx.parameterTextureSheetCycles)) * frameCount), frameCount - 1);
    float2 tileSize = 1.0 / tiles;
    float2 tileOffset = float2(fmod(frame, tiles.x), tiles.y - floor(frame / tiles.x) - 1) * tileSize;
    ctx.valueRenderUvTransform = float4(ctx.valueRenderUvTransform.xy * tileSize,
        ctx.valueRenderUvTransform.zw * tileSize + tileOffset);
}