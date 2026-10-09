namespace JimsFlightController.Services.Contracts;

using JimsFlightController.Models;

public interface ISimService
{
    AircraftState CurrentState { get; }

    bool IsConnected { get; }

    event EventHandler<AircraftState>? StateUpdated;

    event EventHandler<string>? ConnectionStatusChanged;

    Task ConnectAsync();

    void Disconnect();

    void SetHeading(int heading);

    void SetAltitude(int altitude);

    void SetVerticalSpeed(int vs);

    void EnableHeadingMode();

    void EnableAltitudeMode();

    void EnableVerticalSpeedMode();

    void EnableNavMode();

    void ToggleAutopilotMaster();
}
