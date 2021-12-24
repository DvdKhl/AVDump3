using AVDump3Lib.Settings.Core;
using AVDump3GUI.Controls.Settings;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Immutable;
using System.Resources;
using Prism.Mvvm;
using Prism.Commands;

namespace AVDump3GUI.ViewModels;

public class SettingsPropertyViewModel : BindableBase, ISettingPropertyItem {
	public ResourceManager ResourceManager { get; }
	public ISettingStore SettingStore { get; }
	public ISettingProperty Base { get; }

	public SettingsPropertyViewModel(ISettingProperty settingProperty, ISettingStore settingStore, ResourceManager resourceManager) {
		Base = settingProperty ?? throw new ArgumentNullException(nameof(settingProperty));
		SettingStore = settingStore ?? throw new ArgumentNullException(nameof(settingStore));

		ResourceManager = resourceManager;

		StoredValue = SettingStore.GetRawPropertyValue(Base);
	}

	public string Description => ResourceManager.GetInvString($"{Base.Group.FullName}.{Base.Name}.Description") ?? "";
	public string Example => ResourceManager.GetInvString($"{Base.Group.FullName}.{Base.Name}.Example") ?? "";

	public void UpdateStoredValue() {
		StoredValue = SettingStore.GetRawPropertyValue(Base);
		UpdateBindings();
	}

	public DelegateCommand SetDefaultValueCommand => setDefaultValueCommand ??= new DelegateCommand(SetDefaultValueCommandExecute);
	private DelegateCommand setDefaultValueCommand = null!;

	private void SetDefaultValueCommandExecute() {
		Value = DefaultValue;
	}


	public DelegateCommand UnsetValueCommand => unsetValueCommand ??= new DelegateCommand(UnsetValueCommandExecute);
	private DelegateCommand unsetValueCommand = null!;

	private void UnsetValueCommandExecute() {
		Value = ISettingStore.Unset;
	}


	public DelegateCommand ResetToStoredValueCommand => resetToStoredValueCommand ??= new DelegateCommand(ResetToStoredValueCommandExecute);
	private DelegateCommand resetToStoredValueCommand = null!;

	private void ResetToStoredValueCommandExecute() {
		Value = StoredValue;
	}


	public ImmutableArray<string> AlternativeNames => Base.AlternativeNames;
	public string Name => Base.Name;
	public Type ValueType => Base.ValueType;
	public string ValueTypeKey => Base.ValueType.Name;

	public object DefaultValue => Base.DefaultValue;
	public object StoredValue { get; private set; }
	public object ValueRaw { get => SettingStore.GetRawPropertyValue(Base); set { SettingStore.SetPropertyValue(Base, value); UpdateBindings(); } }
	public object Value { get => SettingStore.GetPropertyValue(Base); set { SettingStore.SetPropertyValue(Base, value); UpdateBindings(); } }
	public bool IsSet => ValueRaw != ISettingStore.Unset;
	public bool IsSetAndUnchanged => IsSet && !IsChanged;
	public bool IsChanged {
		get {
			var value = SettingStore.GetRawPropertyValue(Base);
			if(value == ISettingStore.Unset ^ StoredValue == ISettingStore.Unset) {
				return true;
			} else if(value == ISettingStore.Unset) {
				return false;
			} else {
				var v1 = Base.ToString(ValueRaw);
				var v2 = Base.ToString(StoredValue);

				return !v1?.Equals(v2) ?? (v2 == null);
			}
		}
	}

	public object? ToObject(string? stringValue) => Base.ToObject(stringValue);
	public string? ToString(object? objectValue) => Base.ToString(objectValue);

	private void UpdateBindings() {
		RaisePropertyChanged("IsSet");
		RaisePropertyChanged("IsSetAndUnchanged");
		RaisePropertyChanged("IsChanged");
		RaisePropertyChanged("ValueRaw");
		RaisePropertyChanged("Value");
	}
}
