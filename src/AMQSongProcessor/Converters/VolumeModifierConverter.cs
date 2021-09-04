﻿using System.Text.Json;
using System.Text.Json.Serialization;

using AMQSongProcessor.Models;

namespace AMQSongProcessor.Converters
{
	internal sealed class VolumeModifierConverter : JsonConverter<VolumeModifer?>
	{
		public override VolumeModifer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var s = reader.GetString();
			if (s is null)
			{
				return default;
			}
			return VolumeModifer.Parse(s);
		}

		public override void Write(Utf8JsonWriter writer, VolumeModifer? value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value?.ToString());
	}
}