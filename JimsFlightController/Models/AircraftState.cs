namespace JimsFlightController.Models;

public class AircraftState
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AltitudeFeet { get; set; }
    public double GroundSpeed { get; set; }
    public double Heading { get; set; }
    public bool ApMaster { get; set; }
    public double SelectedAltitude { get; set; }
    public double VerticalSpeed { get; set; }
    public int SelectedHeading { get; set; }
    public bool HeadingMode { get; set; }
    public bool AltitudeMode { get; set; }
    public bool VerticalSpeedMode { get; set; }
    public bool NavMode { get; set; }
}
