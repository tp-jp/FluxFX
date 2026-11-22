using System;
using System.Collections.Generic;
using System.Linq;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 生成物の削除予定・保持理由・実行結果です。削除の権限としては使用しません。
    /// </summary>
    [Serializable]
    public sealed class FluxParticleGenerationCleanupReport
    {
        /// <summary>
        /// 生成物1件の識別情報、容量、整理結果と保持理由。
        /// </summary>
        [Serializable]
        public sealed class Item
        {
            public string path;
            public string guid;
            public string kind;
            public long bytes;
            /// <summary>
            /// Candidate／Keep／Missing／Deleted／Failedのいずれか
            /// </summary>
            public string status;
            public string reason;
            public string reference;
        }

        public string root;
        public string createdUtc;
        /// <summary>
        /// 全Assetと使用中オブジェクトの参照走査を開始したか
        /// </summary>
        public bool fullScan;
        public bool blocked;
        /// <summary>
        /// 分析・管理記録処理の失敗。個々の削除失敗はItem.statusに記録
        /// </summary>
        public bool failed;
        public string logPath;
        public List<string> diagnostics = new List<string>();
        public List<Item> items = new List<Item>();

        /// <summary>
        /// 現在の削除候補数
        /// </summary>
        public int CandidateCount => items.Count(item => item.status == "Candidate");

        /// <summary>
        /// 削除候補の合計サイズ（metaを含む）
        /// </summary>
        public long CandidateBytes => items.Where(item => item.status == "Candidate").Sum(item => item.bytes);

        /// <summary>
        /// 削除に成功したAsset数
        /// </summary>
        public int DeletedCount => items.Count(item => item.status == "Deleted");
    }
}
