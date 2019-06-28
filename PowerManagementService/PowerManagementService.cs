using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using static PowerManagementService.POWER_DATA_ACCESSOR;

namespace PowerManagementService
{
    public sealed unsafe partial class PowerManagementService : ServiceBase
    {
        private const uint ERROR_SUCCESS = 0;

        private Guid* _cachedPolicyScheme;
        private Guid _lockedPowerScheme;
        private Guid _unlockedPowerScheme;

        public PowerManagementService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            fixed (Guid** cachedPolicyScheme = &_cachedPolicyScheme)
            {
                var errorCode = PowerGetActiveScheme(UserRootPowerKey: IntPtr.Zero, cachedPolicyScheme);

                if (errorCode != ERROR_SUCCESS)
                {
                    throw new ExternalException("Failed to get the active power scheme.", (int)errorCode);
                }
            }

            fixed (Guid* lockedPowerScheme = &_lockedPowerScheme)
            {
                if ((TryFindPowerScheme("Power saver", lockedPowerScheme) == false) && (TryFindPowerScheme("Balanced", lockedPowerScheme) == false))
                {
                    throw new Exception("Failed to find the 'Power saver' or 'Balanced' power scheme.");
                }
            }

            fixed (Guid* unlockedPowerScheme = &_unlockedPowerScheme)
            {
                if ((TryFindPowerScheme("Ultimate Performance", unlockedPowerScheme) == false) && (TryFindPowerScheme("High performance", unlockedPowerScheme) == false))
                {
                    throw new Exception("Failed to find the 'Ultimate Performance' or 'High performance' power scheme.");
                }
            }
        }

        protected override void OnStop()
        {
            var errorCode = PowerSetActiveScheme(UserRootPowerKey: IntPtr.Zero, _cachedPolicyScheme);

            if (errorCode != ERROR_SUCCESS)
            {
                throw new ExternalException("Failed to restore the active power scheme.", (int)errorCode);
            }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLock:
                {
                    fixed (Guid* lockedPowerScheme = &_lockedPowerScheme)
                    {
                        var errorCode = PowerSetActiveScheme(UserRootPowerKey: IntPtr.Zero, lockedPowerScheme);

                        if (errorCode != ERROR_SUCCESS)
                        {
                            throw new ExternalException("Failed to set the locked power scheme.", (int)errorCode);
                        }
                        EventLog.WriteEntry("Session locked. Power scheme changed.");
                    }
                    break;
                }

                case SessionChangeReason.SessionUnlock:
                {
                    fixed (Guid* unlockedPowerScheme = &_unlockedPowerScheme)
                    {
                        var errorCode = PowerSetActiveScheme(UserRootPowerKey: IntPtr.Zero, unlockedPowerScheme);

                        if (errorCode != ERROR_SUCCESS)
                        {
                            throw new ExternalException("Failed to set the unlocked power scheme.", (int)errorCode);
                        }
                        EventLog.WriteEntry("Session unlocked. Power scheme changed.");
                    }
                    break;
                }
            }

            base.OnSessionChange(changeDescription);
        }

        private bool TryFindPowerScheme(string expectedFriendlyName, Guid* powerScheme)
        {
            bool found = false;
            char* friendlyNameBuffer = stackalloc char[256];

            for (uint index = 0; found != true; index++)
            {
                uint powerSchemeSize = (uint)sizeof(Guid);
                uint errorCode = PowerEnumerate(RootPowerKey: IntPtr.Zero, SchemeGuid: null, SubGroupOfPowerSettingsGuid: null, ACCESS_SCHEME, index, (byte*)powerScheme, &powerSchemeSize);

                if (errorCode != ERROR_SUCCESS)
                {
                    break;
                }

                uint friendlyNameBufferSize = 256 * sizeof(char);
                errorCode = PowerReadFriendlyName(RootPowerKey: IntPtr.Zero, powerScheme, SubGroupOfPowerSettingsGuid: null, PowerSettingGuid: null, (byte*)friendlyNameBuffer, &friendlyNameBufferSize);

                var actualFriendlyName = new string(friendlyNameBuffer);
                found = (errorCode == ERROR_SUCCESS) && actualFriendlyName.Equals(expectedFriendlyName);
            }

            return found;
        }

        [DllImport("PowrProf.dll")]
        private static extern uint PowerEnumerate([Optional] IntPtr RootPowerKey, [Optional] Guid* SchemeGuid, [Optional] Guid* SubGroupOfPowerSettingsGuid, POWER_DATA_ACCESSOR AccessFlags, uint Index, [Optional] byte* Buffer, uint* BufferSize);

        [DllImport("PowrProf.dll")]
        private static extern uint PowerGetActiveScheme([Optional] IntPtr UserRootPowerKey, [Optional] Guid** ActivePolicyGuid);

        [DllImport("PowrProf.dll")]
        private static extern uint PowerReadFriendlyName([Optional] IntPtr RootPowerKey, [Optional] Guid* SchemeGuid, [Optional] Guid* SubGroupOfPowerSettingsGuid, [Optional] Guid* PowerSettingGuid, [Optional] byte* Buffer, uint* BufferSize);

        [DllImport("PowrProf.dll")]
        private static extern uint PowerSetActiveScheme([Optional] IntPtr UserRootPowerKey, [Optional] Guid* SchemeGuid);
    }
}
