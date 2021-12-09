﻿using System.Text.Json;
using System.Text.Json.Serialization;

using SongProcessor.Models;

namespace SongProcessor.Converters;

public sealed class VolumeModifierJsonConverter : JsonConverter<VolumeModifer>
{
	public override VolumeModifer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> VolumeModifer.Parse(reader.GetString()!);

	public override void Write(Utf8JsonWriter writer, VolumeModifer value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.ToString());
}
