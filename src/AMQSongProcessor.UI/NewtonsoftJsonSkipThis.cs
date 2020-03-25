﻿using System;

using Newtonsoft.Json;

namespace AMQSongProcessor.UI
{
	public sealed class NewtonsoftJsonSkipThis : JsonConverter
	{
		public override bool CanConvert(Type objectType)
			=> true;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
			=> throw new NotImplementedException();

#pragma warning restore RCS1079 // Throwing of new NotImplementedException.

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
		}
	}
}