using AVDump3Lib.Settings.Core;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.ComponentModel;

namespace AVDump3GUI.Controls.Settings;

public class SettingValueDisplay : BindableBase {
	public SettingValueDisplay(ISettingPropertyItem settingProperty, SettingValueDisplayType type) {
		Property = settingProperty;
		Type = type;

		if(settingProperty is INotifyPropertyChanged npc) {

			npc.PropertyChanged += (s, e) => {
				if(e.PropertyName?.Equals("Value") ?? false) {
					RaisePropertyChanged(nameof(ValueAsString));
					RaisePropertyChanged(nameof(Value));
				}
			};
		}
	}

	public ISettingPropertyItem Property { get; private set; }
	public SettingValueDisplayType Type { get; private set; }

	public object? Value {
		get => Type switch {
			SettingValueDisplayType.Default => Property.DefaultValue,
			SettingValueDisplayType.Current => Property.ValueRaw == ISettingStore.Unset ? null : Property.ValueRaw,
			SettingValueDisplayType.Active => Property.Value,
			_ => null
		};
		set {
			if(Type == SettingValueDisplayType.Current) {
				Property.ValueRaw = value;
			}
		}
	}

	public DelegateCommand ResetCommand => resetCommand ??= new DelegateCommand(ResetExecute);
	private DelegateCommand? resetCommand;

	private void ResetExecute() {
		Property.ValueRaw = ISettingStore.Unset;
	}


	public bool IsReadOnly => Type != SettingValueDisplayType.Current;

	public string ValueAsString {
		get => Property.ToString(Value);
		set => Property.Value = Property.ToObject(value);
	}

}
