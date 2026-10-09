using System.Windows.Input;
using JimsFlightController.Models;
using JimsFlightController.Services.Contracts;

namespace JimsFlightController.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ISimService _simService;
    private string _connectionStatus = "Disconnected";
    private AircraftState _aircraftState = new();
    private int _targetHeading;
    private int _targetAltitude;
    private int _targetVerticalSpeed;

    public MainViewModel(ISimService simService)
    {
        _simService = simService;
        _simService.StateUpdated += OnStateUpdated;
        _simService.ConnectionStatusChanged += OnConnectionStatusChanged;

        ConnectCommand = new RelayCommand(async () => await ConnectAsync(), () => !_simService.IsConnected);
        DisconnectCommand = new RelayCommand(Disconnect, () => _simService.IsConnected);
        ToggleAutopilotCommand = new RelayCommand(ToggleAutopilot, () => _simService.IsConnected);
        SetHeadingCommand = new RelayCommand(SetHeading, () => _simService.IsConnected);
        EnableHeadingModeCommand = new RelayCommand(EnableHeadingMode, () => _simService.IsConnected);
        SetAltitudeCommand = new RelayCommand(SetAltitude, () => _simService.IsConnected);
        EnableAltitudeModeCommand = new RelayCommand(EnableAltitudeMode, () => _simService.IsConnected);
        SetVerticalSpeedCommand = new RelayCommand(SetVerticalSpeed, () => _simService.IsConnected);
        EnableVerticalSpeedModeCommand = new RelayCommand(EnableVerticalSpeedMode, () => _simService.IsConnected);
        EnableNavModeCommand = new RelayCommand(EnableNavMode, () => _simService.IsConnected);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public double Latitude
    {
        get => _aircraftState.Latitude;
        set
        {
            if (_aircraftState.Latitude != value)
            {
                _aircraftState.Latitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LatitudeDisplay));
            }
        }
    }

    public double Longitude
    {
        get => _aircraftState.Longitude;
        set
        {
            if (_aircraftState.Longitude != value)
            {
                _aircraftState.Longitude = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LongitudeDisplay));
            }
        }
    }

    public double AltitudeFeet
    {
        get => _aircraftState.AltitudeFeet;
        set
        {
            if (_aircraftState.AltitudeFeet != value)
            {
                _aircraftState.AltitudeFeet = value;
                OnPropertyChanged();
            }
        }
    }

    public double GroundSpeed
    {
        get => _aircraftState.GroundSpeed;
        set
        {
            if (_aircraftState.GroundSpeed != value)
            {
                _aircraftState.GroundSpeed = value;
                OnPropertyChanged();
            }
        }
    }

    public double Heading
    {
        get => _aircraftState.Heading;
        set
        {
            if (_aircraftState.Heading != value)
            {
                _aircraftState.Heading = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ApMaster
    {
        get => _aircraftState.ApMaster;
        set
        {
            if (_aircraftState.ApMaster != value)
            {
                _aircraftState.ApMaster = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ApStatusDisplay));
            }
        }
    }

    public double SelectedAltitude
    {
        get => _aircraftState.SelectedAltitude;
        set
        {
            if (_aircraftState.SelectedAltitude != value)
            {
                _aircraftState.SelectedAltitude = value;
                OnPropertyChanged();
            }
        }
    }

    public double VerticalSpeed
    {
        get => _aircraftState.VerticalSpeed;
        set
        {
            if (_aircraftState.VerticalSpeed != value)
            {
                _aircraftState.VerticalSpeed = value;
                OnPropertyChanged();
            }
        }
    }

    public int SelectedHeading
    {
        get => _aircraftState.SelectedHeading;
        set
        {
            if (_aircraftState.SelectedHeading != value)
            {
                _aircraftState.SelectedHeading = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HeadingMode
    {
        get => _aircraftState.HeadingMode;
        set
        {
            if (_aircraftState.HeadingMode != value)
            {
                _aircraftState.HeadingMode = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AltitudeMode
    {
        get => _aircraftState.AltitudeMode;
        set
        {
            if (_aircraftState.AltitudeMode != value)
            {
                _aircraftState.AltitudeMode = value;
                OnPropertyChanged();
            }
        }
    }

    public bool VerticalSpeedMode
    {
        get => _aircraftState.VerticalSpeedMode;
        set
        {
            if (_aircraftState.VerticalSpeedMode != value)
            {
                _aircraftState.VerticalSpeedMode = value;
                OnPropertyChanged();
            }
        }
    }

    public bool NavMode
    {
        get => _aircraftState.NavMode;
        set
        {
            if (_aircraftState.NavMode != value)
            {
                _aircraftState.NavMode = value;
                OnPropertyChanged();
            }
        }
    }

    public int TargetHeading
    {
        get => _targetHeading;
        set => SetProperty(ref _targetHeading, value);
    }

    public int TargetAltitude
    {
        get => _targetAltitude;
        set => SetProperty(ref _targetAltitude, value);
    }

    public int TargetVerticalSpeed
    {
        get => _targetVerticalSpeed;
        set => SetProperty(ref _targetVerticalSpeed, value);
    }

    public string LatitudeDisplay => $"{Math.Abs(Latitude):F6}° {(Latitude >= 0 ? "N" : "S")}";
    public string LongitudeDisplay => $"{Math.Abs(Longitude):F6}° {(Longitude >= 0 ? "E" : "W")}";
    public string ApStatusDisplay => ApMaster ? "ON" : "OFF";

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ToggleAutopilotCommand { get; }
    public ICommand SetHeadingCommand { get; }
    public ICommand EnableHeadingModeCommand { get; }
    public ICommand SetAltitudeCommand { get; }
    public ICommand EnableAltitudeModeCommand { get; }
    public ICommand SetVerticalSpeedCommand { get; }
    public ICommand EnableVerticalSpeedModeCommand { get; }
    public ICommand EnableNavModeCommand { get; }

    private async Task ConnectAsync()
    {
        ConnectionStatus = "Connecting...";
        await _simService.ConnectAsync();
        CommandManager.InvalidateRequerySuggested();
    }

    private void Disconnect()
    {
        _simService.Disconnect();
        ConnectionStatus = "Disconnected";
        CommandManager.InvalidateRequerySuggested();
    }

    private void ToggleAutopilot()
    {
        _simService.ToggleAutopilotMaster();
    }

    private void SetHeading()
    {
        _simService.SetHeading(TargetHeading);
    }

    private void EnableHeadingMode()
    {
        _simService.EnableHeadingMode();
    }

    private void SetAltitude()
    {
        _simService.SetAltitude(TargetAltitude);
    }

    private void EnableAltitudeMode()
    {
        _simService.EnableAltitudeMode();
    }

    private void SetVerticalSpeed()
    {
        _simService.SetVerticalSpeed(TargetVerticalSpeed);
    }

    private void EnableVerticalSpeedMode()
    {
        _simService.EnableVerticalSpeedMode();
    }

    private void EnableNavMode()
    {
        _simService.EnableNavMode();
    }

    private void OnStateUpdated(object? sender, AircraftState state)
    {
        Latitude = state.Latitude;
        Longitude = state.Longitude;
        AltitudeFeet = state.AltitudeFeet;
        GroundSpeed = state.GroundSpeed;
        Heading = state.Heading;
        ApMaster = state.ApMaster;
        SelectedAltitude = state.SelectedAltitude;
        VerticalSpeed = state.VerticalSpeed;
        SelectedHeading = state.SelectedHeading;
        HeadingMode = state.HeadingMode;
        AltitudeMode = state.AltitudeMode;
        VerticalSpeedMode = state.VerticalSpeedMode;
        NavMode = state.NavMode;
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        ConnectionStatus = status;
        CommandManager.InvalidateRequerySuggested();
    }

    public void Dispose()
    {
        if (_simService is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
