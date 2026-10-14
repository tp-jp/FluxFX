void FluxFX_TestStateUpdate(inout FluxTestStateUpdateContext ctx)
{
    ctx.valueTestState += ctx.valueTestRate * ctx.valueStepDeltaTime;
}
