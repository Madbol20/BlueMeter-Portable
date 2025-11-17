using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace BlueMeter.WPF.Converters;

/// <summary>
/// JSON converter for KeyBinding that properly handles Key.None to avoid confusion with Key.D0
/// </summary>
public class KeyBindingJsonConverter : JsonConverter<Models.KeyBinding>
{
    public override Models.KeyBinding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        Key key = Key.None;
        ModifierKeys modifiers = ModifierKeys.None;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new Models.KeyBinding(key, modifiers);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            string propertyName = reader.GetString()!;
            reader.Read(); // Move to the value

            switch (propertyName.ToLowerInvariant())
            {
                case "key":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var keyString = reader.GetString();
                        if (!string.IsNullOrEmpty(keyString) && Enum.TryParse<Key>(keyString, out var parsedKey))
                        {
                            key = parsedKey;
                        }
                        else
                        {
                            key = Key.None;
                        }
                    }
                    else if (reader.TokenType == JsonTokenType.Number)
                    {
                        var keyValue = reader.GetInt32();
                        // Explicitly handle 0 as Key.None to avoid D0 confusion
                        key = keyValue == 0 ? Key.None : (Key)keyValue;
                    }
                    break;

                case "modifiers":
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var modifiersString = reader.GetString();
                        if (!string.IsNullOrEmpty(modifiersString) && Enum.TryParse<ModifierKeys>(modifiersString, out var parsedModifiers))
                        {
                            modifiers = parsedModifiers;
                        }
                        else
                        {
                            modifiers = ModifierKeys.None;
                        }
                    }
                    else if (reader.TokenType == JsonTokenType.Number)
                    {
                        modifiers = (ModifierKeys)reader.GetInt32();
                    }
                    break;
            }
        }

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, Models.KeyBinding value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write Key as string to avoid confusion between Key.None (0) and Key.D0
        writer.WriteString("key", value.Key.ToString());

        // Write ModifierKeys as string for consistency
        writer.WriteString("modifiers", value.Modifiers.ToString());

        writer.WriteEndObject();
    }
}
