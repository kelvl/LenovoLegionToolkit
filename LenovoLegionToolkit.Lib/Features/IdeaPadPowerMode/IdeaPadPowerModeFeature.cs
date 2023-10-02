using LenovoLegionToolkit.Lib.Utils;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static LenovoLegionToolkit.Lib.Features.IdeaPadPowerMode.IdeaPadPowerModeFeature;

namespace LenovoLegionToolkit.Lib.Features.IdeaPadPowerMode
{
    public class IdeaPadPowerModeFeature : IFeature<PowerModeState>
    {
        private enum PowerPlan
        {
            IntelligentCooling = 1,
            ExtremePerformance = 3,
            EfficiencyMode = 2
        }

        [NativeCppClass]
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct CIntelligentCoolingS
        {
            private long value;
        }

        [DllImport("PowerBattery.dll", EntryPoint = "?SetITSMode@CIntelligentCooling@PowerBattery@@QEAAHAEAW4ITSMode@12@@Z", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int SetITSMode(ref CIntelligentCoolingS var1, ref PowerPlan var2);

        [DllImport("PowerBattery.dll", EntryPoint = "??0CIntelligentCooling@PowerBattery@@QEAA@XZ", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern CIntelligentCoolingS CIntelligentCooling(ref CIntelligentCoolingS var1);


        [DllImport("PowerBattery.dll", EntryPoint = "?GetITSMode@CIntelligentCooling@PowerBattery@@QEAAHAEAHAEAW4ITSMode@12@@Z", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int GetITSMode(ref CIntelligentCoolingS var1, ref int var2, ref PowerPlan var3);

        private static PowerPlan GetPowerModeDll()
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Getting IdeaPad PowerMode via DLL");
            CIntelligentCoolingS instance = new();
            instance = CIntelligentCooling(ref instance);
            PowerPlan newmode = new();
            int trash = 0;
            _ = GetITSMode(ref instance, ref trash, ref newmode);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Retrieved IdeaPad PowerMode. [powermode={newmode}]");
            return newmode;
        }

        private static void SetPowerModeDll(PowerPlan plan)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Set IdeaPad Powermode. [newmode={plan}]");
            CIntelligentCoolingS instance = new();
            instance = CIntelligentCooling(ref instance);
            _ = SetITSMode(ref instance, ref plan);
        }

        private static PowerModeState FromInternal(PowerPlan plan)
        {
            switch (plan)
            {
                case PowerPlan.EfficiencyMode:
                    return PowerModeState.Quiet;
                case PowerPlan.IntelligentCooling:
                    return PowerModeState.Balance;
                case PowerPlan.ExtremePerformance:
                    return PowerModeState.Performance;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unknown mapping for ${plan}");
            return PowerModeState.Balance;
        }

        private static PowerPlan ToInternal(PowerModeState state)
        {
            switch (state)
            {
                case PowerModeState.Quiet:
                    return PowerPlan.EfficiencyMode;
                case PowerModeState.Balance:
                    return PowerPlan.IntelligentCooling;
                case PowerModeState.Performance:
                    return PowerPlan.ExtremePerformance;
                default:
                    return PowerPlan.IntelligentCooling;
            }
        }


        public Task<PowerModeState[]> GetAllStatesAsync()
        {
            return Task.FromResult(new[] { PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance });
        }

        public Task<PowerModeState> GetStateAsync()
        {
            return Task.Run(() =>
            {
                return FromInternal(GetPowerModeDll());
            });
        }

        public Task<bool> IsSupportedAsync()
        {
            return Task.FromResult(true);
        }

        public Task SetStateAsync(PowerModeState state)
        {
            return Task.Run(() =>
            {
                SetPowerModeDll(ToInternal(state));
            });
        }
    }
}
