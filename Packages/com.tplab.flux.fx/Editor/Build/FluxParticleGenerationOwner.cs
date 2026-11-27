using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor.Build
{
    /// <summary>
    /// 元のAuthoringのScene／Prefab内の同一性を識別します。生成されたExecutorのInstance IDには依存しません。
    /// </summary>
    internal static class FluxParticleGenerationOwner
    {
        sealed class TemporaryIdentity
        {
            internal readonly string Id = Guid.NewGuid().ToString("N");
        }

        static readonly ConditionalWeakTable<Object, TemporaryIdentity> _temporary = new ConditionalWeakTable<Object, TemporaryIdentity>();

        internal static string GetId(Object authoring, FluxParticleGenerationSession session)
        {
            if (authoring == null) throw new ArgumentNullException(nameof(authoring));
            if (session == null) throw new ArgumentNullException(nameof(session));
            var id = GlobalObjectId.GetGlobalObjectIdSlow(authoring);
            // 保存済みScene内の未保存追加にも有効なIDが割り当てられる。保存の有無の判定には使わない。
            if (id.identifierType != 0 && id.targetObjectId != 0 &&
                !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(id.assetGUID.ToString())))
                return id.ToString();
            // 破棄後のInstance ID再利用やDomain Reloadでも、別の一時オブジェクトへ所有を引き継がない。
            return "Temporary:" + session.Id + ":" + _temporary.GetValue(authoring, _ => new TemporaryIdentity()).Id;
        }
    }
}
