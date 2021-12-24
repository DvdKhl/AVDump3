using Prism.Mvvm;
using System.IO;

namespace AVDump3GUI.ViewModels;

public class AVDFile: BindableBase {
	public FileInfo Info { get; set; }

	public bool Completed { get => completed; set => SetProperty(ref completed, value); }
	private bool completed;
}
