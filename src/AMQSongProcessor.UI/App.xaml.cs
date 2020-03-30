﻿using AMQSongProcessor.UI.ViewModels;
using AMQSongProcessor.UI.Views;

using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using ReactiveUI;

using Splat;

namespace AMQSongProcessor.UI
{
	public class App : Application
	{
		public override void Initialize()
			=> AvaloniaXamlLoader.Load(this);

		public override void OnFrameworkInitializationCompleted()
		{
			Locator.CurrentMutable.RegisterConstant(new SongLoader
			{
				RemoveIgnoredSongs = false,
			});
			Locator.CurrentMutable.Register<ISongProcessor>(() => new SongProcessor());

			var suspension = new AutoSuspendHelper(ApplicationLifetime);
			var driver = new NewtonsoftJsonSuspensionDriver("appstate.json");
			RxApp.SuspensionHost.CreateNewAppState = () => new MainViewModel();
			RxApp.SuspensionHost.SetupDefaultSuspendResume(driver);
			suspension.OnFrameworkInitializationCompleted();

			var state = RxApp.SuspensionHost.GetAppState<MainViewModel>();
			Locator.CurrentMutable.RegisterConstant<IScreen>(state);
			Locator.CurrentMutable.Register<IViewFor<SongViewModel>>(() => new SongView());
			Locator.CurrentMutable.Register<IViewFor<AddViewModel>>(() => new AddView());
			Locator.CurrentMutable.Register<IViewFor<EditViewModel>>(() => new EditView());

			var window = new MainWindow
			{
				DataContext = Locator.Current.GetService<IScreen>()
			};
			Locator.CurrentMutable.RegisterConstant<IMessageBoxManager>(new MessageBoxManager(window));

			window.Show();
			base.OnFrameworkInitializationCompleted();
		}
	}
}