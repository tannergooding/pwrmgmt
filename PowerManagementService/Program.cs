using System.ServiceProcess;

namespace PowerManagementService
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var service = new PowerManagementService();
            ServiceBase.Run(service);
        }
    }
}
