using TpLab.Flux.FX.Editor.Compilation.Contracts;
using TpLab.Flux.FX.Editor.Compilation.Definitions;
using TpLab.Flux.FX.Editor.Contracts;
using TpLab.Flux.FX.Udon;
using UnityEngine;
using ContractPolicy = TpLab.Flux.FX.Editor.Contracts.FluxParticleParameterPolicy;

namespace TpLab.Flux.FX.Editor.Authoring
{
    /// <summary>
    /// Main・ShapeのParameterに構成に応じた入力契約を設定します。
    /// </summary>
    internal static class FluxParticleMainShapeContracts
    {
        public static FluxParticleParameterDefinition Apply(FluxParticleParameterDefinition parameter, FluxParticleRenderMode mode)
        {
            var id = parameter.Id;
            var policy = parameter.Policy == Compilation.Definitions.FluxParticleParameterPolicy.Static
                ? ContractPolicy.Static : ContractPolicy.Live;
            var apiName = id switch
            {
                "lifetime.range" => "StartLifetimeRange",
                "spawn.speed" => "StartSpeed",
                "spawn.speed-range" => "StartSpeedRange",
                "render.size" => "StartSize",
                "render.size-range" => "StartSizeRange",
                "rotation.start" => "StartRotation",
                "rotation.start-min" => "StartRotationMin",
                "rotation.start-max" => "StartRotationMax",
                "color.start" => "StartColor",
                "color.start-min" => "StartColorMin",
                "color.start-max" => "StartColorMax",
                "spawn.position" => "Position",
                "shape.radius" => "Radius",
                "shape.angle" => "Angle",
                "shape.size" => "Size",
                _ => null
            };
            FluxParticleParameterContract contract;
            switch (id)
            {
                case "render.intensity":
                    contract = FluxParticleRendererContract.Intensity;
                    break;
                case "lifetime.range":
                case "spawn.speed-range":
                case "render.size-range":
                    contract = FluxParticleParameter.Vector2(id).ApiName(apiName).Policy(policy).Minimum(Vector2.zero).OrderedComponents().Build();
                    break;
                case "spawn.speed":
                case "render.size":
                case "shape.radius":
                    contract = FluxParticleParameter.Float(id).ApiName(apiName).Policy(policy).Minimum(0).Build();
                    break;
                case "shape.angle":
                    contract = FluxParticleParameter.Float(id).ApiName(apiName).Policy(policy).Range(0, 90, true, false).Build();
                    break;
                case "shape.size":
                    contract = FluxParticleParameter.Vector3(id).ApiName(apiName).Policy(policy).Minimum(Vector3.zero).Build();
                    break;
                case "rotation.start":
                case "rotation.start-min":
                case "rotation.start-max":
                    var rotation = FluxParticleParameter.Vector3(id).ApiName(apiName).Policy(policy);
                    if (mode == FluxParticleRenderMode.Billboard)
                    {
                        rotation.Range(new Vector3(0, 0, -float.MaxValue), new Vector3(0, 0, float.MaxValue));
                    }
                    contract = rotation.Build();
                    break;
                case "spawn.position":
                    contract = FluxParticleParameter.Vector3(id).ApiName(apiName).Policy(policy).Build();
                    break;
                case "color.start":
                case "color.start-min":
                case "color.start-max":
                    contract = FluxParticleParameter.Color(id).ApiName(apiName).Policy(policy).Build();
                    break;
                default:
                    return parameter;
            }
            var result = FluxParticleParameterContractCompiler.Compile(contract);
            if (result.Type != parameter.Type)
            {
                throw new System.InvalidOperationException($"Main/Shape Parameter '{id}' has an incompatible storage type.");
            }
            return result;
        }
    }
}
