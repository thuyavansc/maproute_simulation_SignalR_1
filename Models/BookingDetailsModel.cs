namespace maproute_simulation_SignalR_1.Models
{
    public class BookingDetailsModel
    {
        public GeoCoordinates PickupLocation { get; set; }
        public GeoCoordinates DropoffLocation { get; set; }
        public string Username { get; set; }
        public int BookingId { get; set; }
    }

    public class GeoCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class HereReverseGeocodeResponse
    {
        public HereReverseGeocodeResponseView[] View { get; set; }
    }

    public class HereReverseGeocodeResponseView
    {
        public HereReverseGeocodeResponseResult[] Result { get; set; }
    }

    public class HereReverseGeocodeResponseResult
    {
        public HereReverseGeocodeResponseLocation Location { get; set; }
    }

    public class HereReverseGeocodeResponseLocation
    {
        public GeoCoordinates DisplayPosition { get; set; }
    }

    public class HereRouteResponse
    {
        public HereRoute[] Route { get; set; }
    }

    public class HereRoute
    {
        public string Shape { get; set; }
    }
}
