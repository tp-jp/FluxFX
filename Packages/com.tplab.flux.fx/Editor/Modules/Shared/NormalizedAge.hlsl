float FluxFX_NormalizedAge(float age, float lifetime)
{
    return saturate(age / max(lifetime, 1e-6));
}
