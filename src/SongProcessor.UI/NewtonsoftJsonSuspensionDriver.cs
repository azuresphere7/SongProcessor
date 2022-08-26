﻿#define USE_NAV_STACK_FIX

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using ReactiveUI;

using System.Reactive;
using System.Reactive.Linq;

namespace SongProcessor.UI;

public class NewtonsoftJsonSuspensionDriver : ISuspensionDriver
{
	private readonly string _File;
	private readonly JsonSerializerSettings _Options = new()
	{
		ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
		ContractResolver = new WritablePropertiesOnlyResolver(),
		Formatting = Formatting.Indented,
		TypeNameHandling = TypeNameHandling.All,
	};
	public bool DeleteOnInvalidState { get; set; }

	public NewtonsoftJsonSuspensionDriver(string file)
	{
		_File = file;
	}

	public IObservable<Unit> InvalidateState()
	{
		if (DeleteOnInvalidState && File.Exists(_File))
		{
			File.Delete(_File);
		}
		return Observable.Return(Unit.Default);
	}

	public IObservable<object> LoadState()
	{
		// ReactiveUI relies on this method throwing an exception
		// to determine if CreateNewAppState should be called
		var lines = File.ReadAllText(_File);
		var state = JsonConvert.DeserializeObject<object>(lines, _Options);
		return Observable.Return(state)!;
	}

	public IObservable<Unit> SaveState(object state)
	{
		var lines = JsonConvert.SerializeObject(state, _Options);
		File.WriteAllText(_File, lines);
		return Observable.Return(Unit.Default);
	}

#if USE_NAV_STACK_FIX

	private sealed class NavigationStackValueProvider : IValueProvider
	{
		private readonly IValueProvider _Original;

		public NavigationStackValueProvider(IValueProvider original)
		{
			_Original = original;
		}

		public object? GetValue(object target)
			=> _Original.GetValue(target);

		public void SetValue(object target, object? value)
		{
			var castedTarget = (RoutingState)target!;
			var castedValue = (IEnumerable<IRoutableViewModel>)value!;

			castedTarget.NavigationStack.Clear();
			foreach (var vm in castedValue)
			{
				castedTarget.NavigationStack.Add(vm);
			}
		}
	}

#endif

	private sealed class WritablePropertiesOnlyResolver : DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var props = base.CreateProperties(type, memberSerialization);
			for (var i = props.Count - 1; i >= 0; --i)
			{
				var prop = props[i];

#if USE_NAV_STACK_FIX
				if (prop.DeclaringType == typeof(RoutingState)
					&& prop.PropertyName == nameof(RoutingState.NavigationStack))
				{
					prop.Ignored = false;
					prop.Writable = true;
					prop.ValueProvider = new NavigationStackValueProvider(prop.ValueProvider!);
				}
				else
#endif
				if (!prop.Writable)
				{
					props.RemoveAt(i);
				}
			}
			return props;
		}
	}
}