using System.ComponentModel;
using System.Configuration.Install;

namespace PowerManagementService
{
    [RunInstaller(true)]
    public partial class PowerManagementServiceInstaller : Installer
    {
        public PowerManagementServiceInstaller()
        {
            InitializeComponent();
        }
    }
}
