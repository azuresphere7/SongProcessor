﻿using AMQSongProcessor.UI.ViewModels;

using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace AMQSongProcessor.UI.Views
{
	public class EditView : ReactiveUserControl<EditViewModel>
	{
		public EditView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
			=> AvaloniaXamlLoader.Load(this);
	}
}