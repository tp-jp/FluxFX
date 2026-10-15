using System;
using System.Collections.Generic;

namespace TpLab.Flux.FX.Editor.Contracts
{
    /// <summary>
    /// Moduleの識別、公開API、Parameter一覧の変更不可の契約
    /// </summary>
    public sealed class FluxParticleModuleContract
    {
        /// <summary>
        /// Moduleの識別子
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// ハンドルのクラス名
        /// </summary>
        public string HandleName { get; }

        /// <summary>
        /// ハンドル取得APIの名前
        /// </summary>
        public string AccessorName { get; }

        /// <summary>
        /// Enabledの契約。対応しない場合はnull
        /// </summary>
        public FluxParticleParameterContract<bool> Enabled { get; }

        /// <summary>
        /// 宣言順のParameter一覧。Enabledを除く
        /// </summary>
        public IReadOnlyList<FluxParticleParameterContract> Parameters { get; }

        internal FluxParticleModuleContract(string id, string handleName, string accessorName,
            FluxParticleParameterContract<bool> enabled, IEnumerable<FluxParticleParameterContract> parameters)
        {
            Id = id;
            HandleName = handleName;
            AccessorName = accessorName;
            Enabled = enabled;
            Parameters = new List<FluxParticleParameterContract>(parameters).AsReadOnly();
        }
    }
}
