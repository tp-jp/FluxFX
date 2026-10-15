namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Parameterの変更と反映の契約
    /// </summary>
    public enum FluxParticleParameterPolicy
    {
        /// <summary>
        /// 実行開始後は変更できません。
        /// </summary>
        Static,

        /// <summary>
        /// 変更後の値を既存粒子にも反映します。
        /// </summary>
        Live,

        /// <summary>
        /// 変更後の値を次回以降のSpawnに使用し、生成済み粒子の評価結果を保持します。
        /// </summary>
        SpawnLocked
    }
}
