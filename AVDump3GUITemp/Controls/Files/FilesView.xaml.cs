using AVDump3GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AVDump3GUI.Controls.Files;
/// <summary>
/// Interaction logic for FilesView.xaml
/// </summary>
public partial class FilesView : UserControl {
	public FilesView() {
		InitializeComponent();
	}

	public event EventHandler<string[]>? FilesDrop;



	public IEnumerable<AVDFile> Files { get => (IEnumerable<AVDFile>)GetValue(FilesProperty); set => SetValue(FilesProperty, value); }
	public static readonly DependencyProperty FilesProperty = DependencyProperty.Register("Files", typeof(IEnumerable<AVDFile>), typeof(FilesView), new PropertyMetadata(Array.Empty<AVDFile>()));




	public ICommand? StartCommand { get { return (ICommand?)GetValue(StartCommandProperty); } set { SetValue(StartCommandProperty, value); } }
	public static readonly DependencyProperty StartCommandProperty = DependencyProperty.Register("StartCommand", typeof(ICommand), typeof(FilesView), new PropertyMetadata(null));

	private void ListView_DragEnter(object sender, DragEventArgs e) {
		e.Effects = DragDropEffects.Link;
	}

	private void ListView_Drop(object sender, DragEventArgs e) {
		FilesDrop?.Invoke(this, (string[])e.Data.GetData(DataFormats.FileDrop));
	}
}
