using System;
using System.Security.Cryptography;
using System.Text;

namespace TpLab.Flux.FX.Editor.GpuModules
{
    public static class FluxParticleGpuModuleEnabled
    {
        public static string GetParameterName(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId)) throw new ArgumentException("Module ID cannot be empty.", nameof(moduleId));

            var bytes = Encoding.UTF8.GetBytes(moduleId);

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                var name = new StringBuilder("_FluxFXEnabled_");

                for (var i = 0; i < 16; i++)
                {
                    name.Append(hash[i].ToString("x2"));
                }

                return name.ToString();
            }
        }
    }
}