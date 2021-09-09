﻿using SongProcessor.UI.ViewModels;

using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace SongProcessor.UI.Views
{
	public sealed class AddView : ReactiveUserControl<AddViewModel>
	{
		public AddView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
			=> AvaloniaXamlLoader.Load(this);
	}
}