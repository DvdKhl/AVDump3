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

namespace AVDump3GUI.Views {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();

			var vm = (MainWindowViewModel)DataContext;
			vm.ConsoleWrite += Vm_ConsoleWrite;
		}

		private void Vm_ConsoleWrite(string obj) {
			Dispatcher.Invoke(() => {
				ConsoleTextBox.AppendText(obj + "\n");
			});
		}

		private void FilesView_FilesDrop(object sender, string[] e) {
			var vm = (MainWindowViewModel)DataContext;
			vm.AddPaths(e);

		}
	}
}
