﻿using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia.Data.Converters;

namespace AMQSongProcessor.UI.Converters
{
	public abstract class BaseConverter : IMultiValueConverter
	{
		public abstract int ExpectedValueCount { get; }

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Count != ExpectedValueCount)
			{
				throw InvalidCount();
			}
			return Convert(new ValueCollection(values), targetType, parameter, culture);
		}

		protected abstract object Convert(ValueCollection values, Type targetType, object parameter, CultureInfo culture);

		protected ArgumentException InvalidCount()
			=> new ArgumentException($"Invalid value count. Expected {ExpectedValueCount}.");
	}
}