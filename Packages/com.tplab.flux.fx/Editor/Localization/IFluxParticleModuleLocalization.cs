namespace TpLab.Flux.FX.Editor.Localization
{
    /// <summary>
    /// Module Definitionが任意で実装するInspectorの翻訳です。
    /// 見出し、自動描画する項目名・Tooltip・enum選択肢をModule単位で解決します。
    /// 独自PropertyDrawerの内部表示はDrawer側で扱います。
    /// </summary>
    public interface IFluxParticleModuleLocalization
    {
        /// <summary>
        /// 指定言語の表示文言を返します。翻訳を保持する場合も言語ごとに解決してください。
        /// </summary>
        /// <param name="text">翻訳前の表示文言。見出しはModuleクラス名をNicifyした文字列</param>
        /// <param name="language">Inspectorで選択中の言語</param>
        /// <returns>翻訳文。nullまたは空文字の場合はFluxFX標準の翻訳、未定義なら元の文言</returns>
        string Translate(string text, Language language);
    }
}
