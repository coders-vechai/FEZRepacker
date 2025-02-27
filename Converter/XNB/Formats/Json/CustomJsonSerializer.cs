﻿using System.Text.Json;
using System.Text.Json.Serialization;

using FEZRepacker.Converter.XNB.Formats.Json.CustomConverters;

namespace FEZRepacker.Converter.XNB.Formats.Json
{
    internal static class CustomJsonSerializer
    {
        private static JsonSerializerOptions? _serializerOptions;

        private static void PrepareOptions()
        {
            if (_serializerOptions != null) return;
            _serializerOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = {
                    new JsonStringEnumConverter(),
                    new Vector2JsonConverter(),
                    new Vector3JsonConverter(),
                    new QuaternionJsonConverter(),
                    new ColorJsonConverter(),
                    new TimeSpanJsonConverter(),
                    new TrileEmplacementJsonConverter(),
                    new TrileEmplacementListJsonConverter(),
                    new ScriptTriggerJsonConverter(),
                    new ScriptConditionJsonConverter(),
                    new ScriptActionJsonConverter(),
                }
            };
        }

        public static string Serialize<T>(T serializable)
        {
            PrepareOptions();
            return JsonSerializer.Serialize(serializable, _serializerOptions);
        }

        public static T Deserialize<T>(string json)
        {
            PrepareOptions();
            return JsonSerializer.Deserialize<T>(json, _serializerOptions)!;
        }

    }
}
