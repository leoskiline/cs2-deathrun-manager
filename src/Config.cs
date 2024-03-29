﻿using System;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("DeathrunEnabled")] public int DrEnabled { get; set; } = 1;

    [JsonPropertyName("DeathrunPrefix")] public string DrPrefix { get; set; } = "[DR Manager]";
}
