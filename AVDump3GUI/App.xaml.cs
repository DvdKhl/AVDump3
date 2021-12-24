using AVDump3GUI.Views;
using Prism.Ioc;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AVDump3GUI {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : PrismApplication {
		protected override Window CreateShell() {
			var w = Container.Resolve<MainWindow>();
			return w;
		}

		protected override void RegisterTypes(IContainerRegistry containerRegistry) {

		}
	}
}
