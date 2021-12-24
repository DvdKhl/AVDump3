using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AVDump3GUI.Controls.Settings;
/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl {
	public SettingsView() {
		InitializeComponent();
	}






	public IEnumerable<ISettingGroupItem> SettingGroups { get => (IEnumerable<ISettingGroupItem>)GetValue(SettingGroupsProperty); set => SetValue(SettingGroupsProperty, value); }
	public static readonly DependencyProperty SettingGroupsProperty = DependencyProperty.Register("SettingGroups", typeof(IEnumerable<ISettingGroupItem>), typeof(SettingsView), new PropertyMetadata(Array.Empty<ISettingGroupItem>()));



	public SettingValueTemplateSelector SettingValueTemplateSelector { get => (SettingValueTemplateSelector)GetValue(SettingValueTemplateSelectorProperty); set => SetValue(SettingValueTemplateSelectorProperty, value); }
	public static readonly DependencyProperty SettingValueTemplateSelectorProperty = DependencyProperty.Register("SettingValueTemplateSelector", typeof(SettingValueTemplateSelector), typeof(SettingsView), new PropertyMetadata(null));



}
