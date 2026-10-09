
using System;
using System.IO;
using System.Text;
using TpLab.Flux.Editor;
using TpLab.Flux.FX.Scripts;
using TpLab.Flux.FX.Scripts.Modules;
using TpLab.Flux.Udon;
using TpLab.SceneFlow.Editor.Cores;
using TpLab.SceneFlow.Editor.Passes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TpLab.Flux.FX.Editor
{
    public class FluxFXShaderD2BuildPass : PassBase
    {
        const string SourcePath = "Packages/com.tplab.flux.fx/Runtime/Shaders/ParticleVelocityUpdate.shader";
        const string GeneratedDirectory = "Assets/FluxFX/Generated/Shaders";
        const string GravityInclude = "Assets/FluxFX/Modules/Gravity.hlsl";
        const string DragInclude = "Assets/FluxFX/Modules/Drag.hlsl";

        const string SourceShaderName = "FluxFX/ParticleVelocityUpdate";
        const string GravityOperation = "velocity.xyz += _Gravity.xyz * _DeltaTime;";
        const string DragOperation = "float dragFactor = exp(-_Drag * _DeltaTime);\n                velocity.xyz *= dragFactor;";

        protected override void ConfigureDependencies(DependencyBuilder builder)
        {
            builder.Before<FluxBuildPass>();
        }

        public override void Execute(SceneFlowContext context)
        {
            var sourceShader = AssetDatabase.LoadAssetAtPath<Shader>(SourcePath);
            if (sourceShader == null)
                throw new InvalidOperationException($"Source Shader not found: {SourcePath}");

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GravityInclude) == null ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DragInclude) == null)
                throw new InvalidOperationException("D2 Module HLSL files were not found.");

            var source = File.ReadAllText(SourcePath).Replace("\r\n", "\n");
            var authorings = Object.FindObjectsOfType<FluxParticleAuthoring>(true);
            var assignedCount = 0;

            foreach (var authoring in authorings)
            {
                var plan = FluxParticleCompilePlan.Create(authoring);
                var gravity = plan.GetModule<FluxParticleGravityModule>() != null;
                var drag = plan.GetModule<FluxParticleDragModule>() != null;

                var generatedShader = GenerateShader(source, gravity, drag);
                var kernels = authoring.GetComponentsInChildren<FluxKernel>(true);

                foreach (var kernel in kernels)
                {
                    if (kernel.Shader != sourceShader) continue;

                    kernel.SetProgramVariable("shader", generatedShader);
                    assignedCount++;

                    Debug.Log($"[FluxFX D2] Assigned {generatedShader.name} to {kernel.name}", kernel);
                }
            }

            if (assignedCount == 0)
                throw new InvalidOperationException("No Velocity FluxKernel was assigned.");

            Debug.Log($"[FluxFX D2] Completed. Assigned kernels: {assignedCount}");
        }

        static Shader GenerateShader(string source, bool gravity, bool drag)
        {
            var suffix = gravity && drag ? "GD" : gravity ? "G" : drag ? "D" : "None";
            var shaderName = $"Hidden/FluxFX/Generated/Velocity_{suffix}";
            var assetPath = $"{GeneratedDirectory}/Velocity_{suffix}.shader";

            var includes = new StringBuilder();

            if (gravity)
                includes.AppendLine($"            #include \"{GravityInclude}\"");

            if (drag)
                includes.AppendLine($"            #include \"{DragInclude}\"");

            var operations = new StringBuilder();
            operations.AppendLine("            void FluxFX_ApplyGravity(inout float3 velocity, float deltaTime)");
            operations.AppendLine("            {");

            if (gravity)
                operations.AppendLine("                FluxFX_Gravity(velocity, deltaTime);");

            operations.AppendLine("            }");
            operations.AppendLine();
            operations.AppendLine("            void FluxFX_ApplyDrag(inout float3 velocity, float deltaTime)");
            operations.AppendLine("            {");

            if (drag)
                operations.AppendLine("                FluxFX_Drag(velocity, deltaTime);");

            operations.AppendLine("            }");
            operations.AppendLine();

            var generated = source;

            generated = ReplaceRequired(generated,
                $"Shader \"{SourceShaderName}\"",
                $"Shader \"{shaderName}\"");

            generated = ReplaceRequired(generated,
                "            float _SpawnCount;",
                "            float _SpawnCount;\n" + includes);

            generated = ReplaceRequired(generated,
                "            float4 frag(v2f_img i) : SV_Target",
                operations + "            float4 frag(v2f_img i) : SV_Target");

            generated = ReplaceRequired(generated,
                GravityOperation,
                "FluxFX_ApplyGravity(velocity.xyz, _DeltaTime);");

            generated = ReplaceRequired(generated,
                DragOperation,
                "FluxFX_ApplyDrag(velocity.xyz, _DeltaTime);");

            Directory.CreateDirectory(GeneratedDirectory);

            if (!File.Exists(assetPath) || File.ReadAllText(assetPath) != generated)
            {
                File.WriteAllText(assetPath, generated);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"[FluxFX D2] Generated: {assetPath}");
            }
            else
            {
                Debug.Log($"[FluxFX D2] Reused: {assetPath}");
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            if (shader == null || shader.name != shaderName || !shader.isSupported)
                throw new InvalidOperationException($"Generated Shader is invalid: {assetPath}");

            return shader;
        }

        static string ReplaceRequired(string source, string before, string after)
        {
            if (!source.Contains(before))
                throw new InvalidOperationException($"Shader template marker was not found: {before}");

            return source.Replace(before, after);
        }
    }
}
