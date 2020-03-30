﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ReactiveUI;

namespace AMQSongProcessor.UI.ViewModels
{
	[DataContract]
	public class MainViewModel : ReactiveObject, IScreen
	{
		private RoutingState _Router = new RoutingState();

		public ReactiveCommand<Unit, Unit> Add { get; }
		public ReactiveCommand<Unit, Unit> GoBack { get; }
		public ReactiveCommand<Unit, Unit> Load { get; }

		[DataMember]
		public RoutingState Router
		{
			get => _Router;
			set => this.RaiseAndSetIfChanged(ref _Router, value);
		}

		public MainViewModel()
		{
			Load = ReactiveCommand.Create(() =>
			{
				Router.Navigate.Execute(new SongViewModel());
			}, CanNavigateTo<SongViewModel>());

			Add = ReactiveCommand.Create(() =>
			{
				Router.Navigate.Execute(new AddViewModel());
			}, CanNavigateTo<AddViewModel>());

			GoBack = ReactiveCommand.Create(() =>
			{
				Router.NavigateBack.Execute();
			}, CanGoBack());
		}

		private IObservable<bool> CanGoBack()
			=> CanNavigate().CombineLatest(Router.NavigateBack.CanExecute, (x, y) => x && y);

		private IObservable<bool> CanNavigate()
		{
			return this
				.WhenAnyObservable(x => x.Router.CurrentViewModel)
				.SelectMany(x =>
				{
					if (x is INavigationController controller)
					{
						return controller.CanNavigate;
					}
					return Observable.Never<bool>().StartWith(true);
				});
		}

		private IObservable<bool> CanNavigateTo<T>() where T : IRoutableViewModel
		{
			var isDifferent = this
				.WhenAnyObservable(x => x.Router.CurrentViewModel)
				.Select(x => !(x is T));
			return CanNavigate().CombineLatest(isDifferent, (x, y) => x && y);
		}
	}
}