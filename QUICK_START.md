# Quick Start Guide

## What You Have Now

A complete WPF application structure with:

✅ **MVVM Architecture**
- `MainViewModel` - Handles all UI logic and state
- `AircraftState` - Clean data model for telemetry
- `ISimService` - Service interface for dependency injection

✅ **Professional UI**
- Dark theme matching Visual Studio
- Telemetry display panel
- Autopilot control panel with:
  - AP Master toggle
  - Heading control
  - Altitude control
  - Vertical Speed control
  - NAV mode

✅ **Project Structure**
- Models, Services, ViewModels, Converters properly separated
- Commands for all user actions
- Data binding throughout

## What You Need to Do Next

### Step 1: Get SimConnect DLL (5 minutes)

The MSFS SDK should be installed with MSFS 2024. Common locations:

```
C:\MSFS SDK\SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll
%LOCALAPPDATA%\Packages\Microsoft.FlightSimulator_...\LocalCache\Packages\fs-base-simconnect\SimConnect.dll
```

Or install from MSFS Developer Mode:
1. Launch MSFS 2024
2. Enable Developer Mode (Options → General → Developers)
3. Top menu bar → SDK Installer
4. Install SimConnect

### Step 2: Add DLL Reference (2 minutes)

Find the current project file:

```bash
MSFS2024Companion/JimsFlightController.csproj
```

It currently looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>
```

Add this section before the closing `</Project>` tag:

```xml
  <ItemGroup>
    <Reference Include="Microsoft.FlightSimulator.SimConnect">
      <HintPath>C:\Program Files (x86)\Steam\steamapps\common\MSFS2024\SimConnect_internal.dll</HintPath>
    </Reference>
  </ItemGroup>
```

Update the path to match where you found the DLL.

### Step 3: Complete SimConnectService.cs (30-60 minutes)

Open `MSFS2024Companion/Services/SimConnectService.cs`

The file currently has placeholders marked with `// TODO:`

Follow the guide in `SIMCONNECT_GUIDE.md` to:
1. Add the `using Microsoft.FlightSimulator.SimConnect;` statement
2. Replace all the TODO sections with actual SimConnect API calls
3. Add event handlers for receiving data

### Step 4: Test It! (5 minutes)

1. Start MSFS 2024 and load into any aircraft
2. In Visual Studio, press **F5** to debug
3. Click **Connect**
4. Watch the telemetry update!
5. Try the autopilot controls

## Expected Results

Once SimConnect is wired up, you should see:

**Telemetry Panel:**
```
Position: 47.123456° N  122.123456° W
Altitude: 5430 ft
Ground Speed: 124 kts
Heading: 271°
Vertical Speed: 0 fpm
Autopilot: OFF
```

**After engaging AP:**
1. Click "TOGGLE AP MASTER" → Autopilot: ON (green)
2. Enter heading (e.g., 270)
3. Click "SET HDG" → Sets heading bug
4. Click "HDG" → Enables heading mode, button shows "HDG [ON]"

## Development Tips

### Debugging
- Set breakpoints in `OnRecvSimObjectData` to see incoming data
- Check the `ConnectionStatus` text for errors
- Watch the Output window for exceptions

### Iteration Speed
Unlike WASM packages, you can:
- Make a code change
- Press F5
- Immediately see results
- No sim restart needed!

### Testing Without SimConnect
If you want to test UI changes without MSFS running:
1. Add a "Mock" mode that generates fake telemetry
2. Create a `MockSimService : ISimService`
3. Swap it in `MainWindow.xaml.cs` constructor

Example mock service:

```csharp
public class MockSimService : ISimService
{
    private Timer _timer;
    private Random _random = new Random();
    
    public AircraftState CurrentState { get; private set; } = new();
    public bool IsConnected { get; private set; }
    
    public event EventHandler<AircraftState>? StateUpdated;
    public event EventHandler<string>? ConnectionStatusChanged;
    
    public async Task ConnectAsync()
    {
        await Task.Delay(500); // Simulate connection time
        IsConnected = true;
        ConnectionStatusChanged?.Invoke(this, "Connected (Mock)");
        
        _timer = new Timer(_ => UpdateMockData(), null, 0, 100);
    }
    
    private void UpdateMockData()
    {
        CurrentState = new AircraftState
        {
            Latitude = 47.5 + _random.NextDouble() * 0.01,
            Longitude = -122.3 + _random.NextDouble() * 0.01,
            AltitudeFeet = 5000 + _random.Next(-50, 50),
            GroundSpeed = 120 + _random.Next(-5, 5),
            Heading = 270 + _random.Next(-2, 2),
            VerticalSpeed = _random.Next(-100, 100),
            ApMaster = true
        };
        
        StateUpdated?.Invoke(this, CurrentState);
    }
    
    // Implement other interface methods as no-ops or simple state changes
}
```

## Future Enhancements

Once the basic version works, consider:

### Weekend 2-3
- Add more telemetry (IAS, pitch, bank, trim)
- Add AP mode indicators (APR, LNAV, VNAV)
- Add flight director controls
- Add throttle/prop/mixture display

### Weekend 4+
- **Flight Recording**: Save telemetry to SQLite or CSV
- **Playback**: Review recorded flights
- **Profiles**: Save AP settings per aircraft
- **Multiple Windows**: Separate panels for different data
- **Glass Cockpit**: Custom G1000-style display
- **Charts Integration**: Load approach plates
- **Performance Calculator**: W&B, takeoff/landing distance

### Advanced
- **WebSocket Server**: Allow external apps to connect
- **Mobile Companion**: Control from tablet
- **Voice Commands**: "Set heading 270"
- **Checklist Manager**: Interactive checklists
- **Failure Simulation**: Random systems failures for practice

## Troubleshooting

### "Assembly not found" on first run
- Make sure the SimConnect DLL is in the project or output folder
- Try copying it to `bin/Debug/net8.0-windows/`

### UI not updating
- Check that `StateUpdated` event is being invoked
- Verify `Dispatcher.Invoke` is being used for cross-thread updates
- Look for binding errors in Output window

### Autopilot commands don't work
- Verify the aircraft has an autopilot
- Some AP modes require certain conditions (e.g., ALT needs positive climb rate first)
- Try simpler aircraft like C172 G1000 for initial testing

### High CPU usage
- Change data request frequency from `SIM_FRAME` to `SECOND`
- Reduce message processing frequency in timer

## Resources

- **SimConnect SDK Docs**: In MSFS SDK folder, `Documentation/` subfolder
- **SimVar Reference**: https://docs.flightsimulator.com/
- **Key Events List**: Same SDK documentation
- **Community Forums**: MSFS Developer Forum, Reddit r/flightsim

## Support

If you get stuck, check:
1. Output window in Visual Studio for exceptions
2. `ConnectionStatus` text in the app
3. MSFS Developer Mode → SimConnect window shows connections
4. Windows Event Viewer → Application logs

Good luck, and happy flying! ✈️
