﻿using SongProcessor.UI.ViewModels;

using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace SongProcessor.UI.Views;

public sealed class EditView : ReactiveUserControl<EditViewModel>
{
	public EditView()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
		=> AvaloniaXamlLoader.Load(this);
}
