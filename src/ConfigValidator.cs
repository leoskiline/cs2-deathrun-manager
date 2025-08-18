namespace DeathrunManager
{
    internal class ConfigValidator
    {
        public void ValidateAndThrow(PluginConfig config)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.DrPrefix))
                errors.Add("DrPrefix não pode ser nulo ou vazio");

            if (config.DrEnabled is not (0 or 1))
                errors.Add("DrEnabled deve ser 0 ou 1");

            if (config.DrAllowCTGoSpec is not (0 or 1))
                errors.Add("DrAllowCTGoSpec deve ser 0 ou 1");

            if (config.DrOnlyDeathrunMaps is not (0 or 1))
                errors.Add("DrOnlyDeathrunMaps deve ser 0 ou 1");

            if (config.DrEnableBunnyhop is not (0 or 1))
                errors.Add("DrEnableBunnyhop deve ser 0 ou 1");

            if (config.DrVelocityMultiplierTR <= 0)
                errors.Add("DrVelocityMultiplierTR deve ser maior que 0");

            if (errors.Count > 0)
                throw new Exception($"Erros de configuração: {string.Join(", ", errors)}");
        }
    }
}
