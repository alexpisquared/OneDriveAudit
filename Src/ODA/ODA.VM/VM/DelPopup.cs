using MVVM.Common;

namespace ODA.VM.VM
{
  public partial class DelPopup : BindableBaseViewModel
	{
		bool? _OkToDelete = null;   /**/ public bool? OkToDelete { get { return _OkToDelete; } set { Set(ref _OkToDelete, value); } }
	}
}
