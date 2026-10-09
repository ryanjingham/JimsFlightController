using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.FlightSimulator.SimConnect;
using JimsFlightController.Models;
using JimsFlightController.Services.Contracts;

namespace JimsFlightController.Services;

public class SimConnectService : ISimService, IDisposable
{
    private const int WM_USER_SIMCONNECT = 0x0402;
    private const string APP_NAME = "Jims Flight Controller";

    private enum NotificationGroup
    {
        Group0
    }

    private SimConnect? _simConnect;
    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _messageTimer;
    private System.Windows.Interop.HwndSource? _hwndSource;
    private bool _disposed;

    public AircraftState CurrentState { get; private set; } = new();

    public bool IsConnected => _simConnect != null;

    public event EventHandler<AircraftState>? StateUpdated;
    public event EventHandler<string>? ConnectionStatusChanged;

    private enum DataRequest
    {
        AircraftData
    }

    private enum DataDefinition
    {
        AircraftData
    }

    private enum SimEvent
    {
        ApMaster,
        HeadingBugSet,
        ApHdgHold,
        ApAltHold,
        AltVarSet,
        ApVsHold,
        VsVarSet,
        ApNavHold
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    private struct AircraftDataStruct
    {
        public double Latitude;
        public double Longitude;
        public double AltitudeFeet;
        public double GroundSpeed;
        public double Heading;
        public double ApMaster;
        public double SelectedAltitude;
        public double VerticalSpeed;
        public double SelectedHeading;
        public double HeadingMode;
        public double AltitudeMode;
        public double VsMode;
        public double NavMode;
    }

    public SimConnectService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task ConnectAsync()
    {
        await _dispatcher.InvokeAsync(() =>
        {
            try
            {
                if (_hwndSource == null)
                {
                    var parameters = new System.Windows.Interop.HwndSourceParameters(APP_NAME)
                    {
                        Width = 0,
                        Height = 0,
                        WindowStyle = 0
                    };
                    _hwndSource = new System.Windows.Interop.HwndSource(parameters);
                    _hwndSource.AddHook(WndProc);
                }

                _simConnect = new SimConnect(APP_NAME, _hwndSource.Handle, WM_USER_SIMCONNECT, null, 0);

                _simConnect.OnRecvOpen += OnRecvOpen;
                _simConnect.OnRecvQuit += OnRecvQuit;
                _simConnect.OnRecvException += OnRecvException;
                _simConnect.OnRecvSimobjectDataBytype += OnRecvSimObjectDataByType;

                RegisterDataDefinitions();
                RegisterEvents();

                StartMessageProcessing();
            }
            catch (Exception ex)
            {
                _simConnect = null;
                ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            }
        });
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_USER_SIMCONNECT && _simConnect != null)
        {
            try
            {
                _simConnect.ReceiveMessage();
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"SimConnect message error: {ex.Message}");
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void StartMessageProcessing()
    {
        // Fallback polling in case WM_USER_SIMCONNECT isn't delivered reliably.
        _messageTimer?.Stop();
        _messageTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _messageTimer.Tick += (_, _) =>
        {
            try
            {
                _simConnect?.ReceiveMessage();
            }
            catch
            {
                // Ignore transient receive errors; connection failures are surfaced via OnRecvQuit/Exception.
            }
        };
        _messageTimer.Start();
    }

    private void RegisterDataDefinitions()
    {
        if (_simConnect == null) return;

        AddDef("PLANE LATITUDE", "degrees");
        AddDef("PLANE LONGITUDE", "degrees");
        AddDef("PLANE ALTITUDE", "feet");
        AddDef("GROUND VELOCITY", "knots");
        AddDef("PLANE HEADING DEGREES TRUE", "degrees");
        AddDef("AUTOPILOT MASTER", "bool");
        AddDef("AUTOPILOT ALTITUDE LOCK VAR", "feet");
        AddDef("VERTICAL SPEED", "feet per minute");
        AddDef("AUTOPILOT HEADING LOCK DIR", "degrees");
        AddDef("AUTOPILOT HEADING LOCK", "bool");
        AddDef("AUTOPILOT ALTITUDE LOCK", "bool");
        AddDef("AUTOPILOT VERTICAL HOLD", "bool");
        AddDef("AUTOPILOT NAV1 LOCK", "bool");

        _simConnect.RegisterDataDefineStruct<AircraftDataStruct>(DataDefinition.AircraftData);
    }

    private void AddDef(string simVar, string units)
    {
        _simConnect!.AddToDataDefinition(
            DataDefinition.AircraftData,
            simVar,
            units,
            SIMCONNECT_DATATYPE.FLOAT64,
            0.0f,
            SimConnect.SIMCONNECT_UNUSED);
    }

    private void RegisterEvents()
    {
        if (_simConnect == null) return;

        _simConnect.MapClientEventToSimEvent(SimEvent.ApMaster, "AP_MASTER");
        _simConnect.MapClientEventToSimEvent(SimEvent.HeadingBugSet, "HEADING_BUG_SET");
        _simConnect.MapClientEventToSimEvent(SimEvent.ApHdgHold, "AP_HDG_HOLD");
        _simConnect.MapClientEventToSimEvent(SimEvent.ApAltHold, "AP_ALT_HOLD");
        _simConnect.MapClientEventToSimEvent(SimEvent.AltVarSet, "AP_ALT_VAR_SET_ENGLISH");
        _simConnect.MapClientEventToSimEvent(SimEvent.ApVsHold, "AP_VS_HOLD");
        _simConnect.MapClientEventToSimEvent(SimEvent.VsVarSet, "AP_VS_VAR_SET_ENGLISH");
        _simConnect.MapClientEventToSimEvent(SimEvent.ApNavHold, "AP_NAV1_HOLD");

        _simConnect.RequestDataOnSimObjectType(
            DataRequest.AircraftData,
            DataDefinition.AircraftData,
            0,
            SIMCONNECT_SIMOBJECT_TYPE.USER);
    }

    private void OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
    {
        _dispatcher.Invoke(() => ConnectionStatusChanged?.Invoke(this, "Connected"));

        // Kick off periodic updates now that the connection is confirmed.
        _simConnect?.RequestDataOnSimObjectType(
            DataRequest.AircraftData,
            DataDefinition.AircraftData,
            0,
            SIMCONNECT_SIMOBJECT_TYPE.USER);
    }

    private void OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
    {
        _dispatcher.Invoke(() =>
        {
            ConnectionStatusChanged?.Invoke(this, "Simulator closed connection");
        });
        Disconnect();
    }

    private void OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
    {
        _dispatcher.Invoke(() =>
        {
            ConnectionStatusChanged?.Invoke(this, $"SimConnect exception: {(SIMCONNECT_EXCEPTION)data.dwException}");
        });
    }

    private void OnRecvSimObjectDataByType(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
    {
        if (data.dwRequestID != (uint)DataRequest.AircraftData) return;
        if (data.dwData.Length == 0) return;

        var raw = (AircraftDataStruct)data.dwData[0];

        CurrentState = new AircraftState
        {
            Latitude = raw.Latitude,
            Longitude = raw.Longitude,
            AltitudeFeet = raw.AltitudeFeet,
            GroundSpeed = raw.GroundSpeed,
            Heading = raw.Heading,
            ApMaster = raw.ApMaster > 0,
            SelectedAltitude = raw.SelectedAltitude,
            VerticalSpeed = raw.VerticalSpeed,
            SelectedHeading = (int)raw.SelectedHeading,
            HeadingMode = raw.HeadingMode > 0,
            AltitudeMode = raw.AltitudeMode > 0,
            VerticalSpeedMode = raw.VsMode > 0,
            NavMode = raw.NavMode > 0
        };

        _dispatcher.Invoke(() => StateUpdated?.Invoke(this, CurrentState));
    }

    public void Disconnect()
    {
        _messageTimer?.Stop();
        _messageTimer = null;

        if (_simConnect != null)
        {
            _simConnect.Dispose();
            _simConnect = null;
            ConnectionStatusChanged?.Invoke(this, "Disconnected");
        }

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }

    private void Transmit(SimEvent evt, uint data) => _simConnect?.TransmitClientEvent(
            SimConnect.SIMCONNECT_OBJECT_ID_USER,
            evt,
            data,
            NotificationGroup.Group0,
            SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);

    public void SetHeading(int heading)
    {
        if (!IsConnected) return;
        Transmit(SimEvent.HeadingBugSet, (uint)((heading % 360 + 360) % 360));
        CurrentState.SelectedHeading = heading;
    }

    public void SetAltitude(int altitude)
    {
        if (!IsConnected) return;
        Transmit(SimEvent.AltVarSet, unchecked((uint)altitude));
        CurrentState.SelectedAltitude = altitude;
    }

    public void SetVerticalSpeed(int vs)
    {
        if (!IsConnected) return;
        Transmit(SimEvent.VsVarSet, unchecked((uint)vs));
        CurrentState.VerticalSpeed = vs;
    }

    public void EnableHeadingMode()
    {
        if (!IsConnected) return;
        Transmit(SimEvent.ApHdgHold, 0);
        CurrentState.HeadingMode = true;
    }

    public void EnableAltitudeMode()
    {
        if (!IsConnected) return;
        Transmit(SimEvent.ApAltHold, 0);
        CurrentState.AltitudeMode = true;
    }

    public void EnableVerticalSpeedMode()
    {
        if (!IsConnected) return;
        Transmit(SimEvent.ApVsHold, 0);
        CurrentState.VerticalSpeedMode = true;
    }

    public void EnableNavMode()
    {
        if (!IsConnected) return;
        Transmit(SimEvent.ApNavHold, 0);
        CurrentState.NavMode = true;
    }

    public void ToggleAutopilotMaster()
    {
        if (!IsConnected) return;
        Transmit(SimEvent.ApMaster, 0);
        CurrentState.ApMaster = !CurrentState.ApMaster;
    }

    public void Dispose()
    {
        if (_disposed) return;

        Disconnect();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
