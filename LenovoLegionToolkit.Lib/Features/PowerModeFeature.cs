using System;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Features.IdeaPadPowerMode;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Features;

public class PowerModeUnavailableWithoutACException : Exception
{
    public PowerModeState PowerMode { get; }

    public PowerModeUnavailableWithoutACException(PowerModeState powerMode)
    {
        PowerMode = powerMode;
    }
}

public class PowerModeFeature : IFeature<PowerModeState>
{
    private readonly GodModeController _godModeController;
    private readonly PowerPlanController _powerPlanController;
    private readonly ThermalModeListener _thermalModeListener;
    private readonly PowerModeListener _powerModeListener;
    private readonly IdeaPadPowerModeFeature _ideapadPowerModeFeature;

    public bool AllowAllPowerModesOnBattery { get; set; }

    public PowerModeFeature(GodModeController godModeController, PowerPlanController powerPlanController, ThermalModeListener thermalModeListener, PowerModeListener powerModeListener)
        //: base(WMI.LenovoGameZoneData.GetSmartFanModeAsync, WMI.LenovoGameZoneData.SetSmartFanModeAsync, WMI.LenovoGameZoneData.IsSupportSmartFanAsync, 1)
    {
        _godModeController = godModeController ?? throw new ArgumentNullException(nameof(godModeController));
        _powerPlanController = powerPlanController ?? throw new ArgumentNullException(nameof(powerPlanController));
        _thermalModeListener = thermalModeListener ?? throw new ArgumentNullException(nameof(thermalModeListener));
        _powerModeListener = powerModeListener ?? throw new ArgumentNullException(nameof(powerModeListener));
        _ideapadPowerModeFeature = new IdeaPadPowerModeFeature();
    }

    public async Task<PowerModeState[]> GetAllStatesAsync()
    {
        //var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        return await _ideapadPowerModeFeature.GetAllStatesAsync();
        //return mi.Properties.SupportsGodMode
        //    ? new[] { PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance, PowerModeState.GodMode }
        //    : new[] { PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance };
    }

    public async Task SetStateAsync(PowerModeState state)
    {
        await _ideapadPowerModeFeature.SetStateAsync(state);
        await _powerModeListener.NotifyAsync(state).ConfigureAwait(false);
    }

    public async Task<PowerModeState> GetStateAsync()
    {
        var res = await _ideapadPowerModeFeature.GetStateAsync();
        return res;
    }

    public async Task<bool> IsSupportedAsync()
    {
        var res = await _ideapadPowerModeFeature.IsSupportedAsync();
        return res;
    }

    //public override async Task SetStateAsync(PowerModeState state)
    //{
    //    var allStates = await GetAllStatesAsync().ConfigureAwait(false);
    //    if (!allStates.Contains(state))
    //        throw new InvalidOperationException($"Unsupported power mode {state}.");

    //    if (state is PowerModeState.Performance or PowerModeState.GodMode
    //        && !AllowAllPowerModesOnBattery
    //        && await Power.IsPowerAdapterConnectedAsync().ConfigureAwait(false) is PowerAdapterStatus.Disconnected)
    //        throw new PowerModeUnavailableWithoutACException(state);

    //    var currentState = await GetStateAsync().ConfigureAwait(false);

    //    var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

    //    if (mi.Properties.HasQuietToPerformanceModeSwitchingBug && currentState == PowerModeState.Quiet && state == PowerModeState.Performance)
    //    {
    //        _thermalModeListener.SuppressNext();
    //        await base.SetStateAsync(PowerModeState.Balance).ConfigureAwait(false);
    //        await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
    //    }

    //    if (mi.Properties.HasGodModeToOtherModeSwitchingBug && currentState == PowerModeState.GodMode && state != PowerModeState.GodMode)
    //    {
    //        _thermalModeListener.SuppressNext();

    //        switch (state)
    //        {
    //            case PowerModeState.Quiet:
    //                await base.SetStateAsync(PowerModeState.Performance).ConfigureAwait(false);
    //                break;
    //            case PowerModeState.Balance:
    //                await base.SetStateAsync(PowerModeState.Quiet).ConfigureAwait(false);
    //                break;
    //            case PowerModeState.Performance:
    //                await base.SetStateAsync(PowerModeState.Balance).ConfigureAwait(false);
    //                break;
    //        }

    //        await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

    //    }

    //    _thermalModeListener.SuppressNext();
    //    await base.SetStateAsync(state).ConfigureAwait(false);

    //    if (state == PowerModeState.GodMode)
    //        await _godModeController.ApplyStateAsync().ConfigureAwait(false);

    //    await _powerModeListener.NotifyAsync(state).ConfigureAwait(false);
    //}

    public async Task EnsureCorrectPowerPlanIsSetAsync()
    {
        var state = await GetStateAsync().ConfigureAwait(false);
        await _powerPlanController.ActivatePowerPlanAsync(state, true).ConfigureAwait(false);
    }

    public async Task EnsureGodModeStateIsAppliedAsync()
    {
        var state = await GetStateAsync().ConfigureAwait(false);
        if (state != PowerModeState.GodMode)
            return;

        await _godModeController.ApplyStateAsync().ConfigureAwait(false);
    }
}
