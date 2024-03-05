using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using maproute_simulation_SignalR_1.Hubs;

namespace maproute_simulation_SignalR_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GPXController : ControllerBase
    {
        private readonly IHubContext<MapHub> _hubContext;

        public GPXController(IHubContext<MapHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("UploadGPXFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadGPXFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("GPX file is required.");

            var filename = await WriteFile(file);
            Console.WriteLine($"GPX file uploaded: {filename}");

            using (var stream = file.OpenReadStream())
            {
                // Parse GPX file
                var gpxData = await ParseGPXFile(stream);
                var totalDuration = 10; //Min
                Console.WriteLine("GPX file parsed successfully.");

                // Print the first 5 coordinates
                PrintFirstFiveCoordinates(gpxData);

                // Send location data to connected clients
                //await SendLocationDataToClients(gpxData);
                await SendLocationDataToClients(gpxData, totalDuration);
                Console.WriteLine("SignalR started sending data to clients.");
            }

            return Ok(filename);
        }

        private void PrintFirstFiveCoordinates(GpxData gpxData)
        {
            if (gpxData == null || gpxData.TrackPoints == null || gpxData.TrackPoints.Count == 0)
            {
                Console.WriteLine("No track points found in GPX data.");
                return;
            }

            Console.WriteLine("First 5 coordinates:");
            for (int i = 0; i < Math.Min(5, gpxData.TrackPoints.Count); i++)
            {
                var trackPoint = gpxData.TrackPoints[i];
                Console.WriteLine($"Coordinate {i + 1}: Latitude = {trackPoint.Latitude}, Longitude = {trackPoint.Longitude}");
            }
        }


        private async Task<string> WriteFile(IFormFile file)
        {
            string filename = "";
            try
            {
                var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
                filename = DateTime.Now.Ticks.ToString() + extension;

                var filepath = Path.Combine(Directory.GetCurrentDirectory(), "Upload", "Files");

                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }

                var exactpath = Path.Combine(filepath, filename);
                using (var stream = new FileStream(exactpath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error occurred while uploading file: {ex.Message}");
            }
            return filename;
        }

        private async Task<GpxData> ParseGPXFile0(Stream stream)
        {
            // Parse GPX file
            var gpxData = new GpxData();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(stream);

            // Parse track points from GPX file
            XmlNodeList trackPoints = xmlDoc.SelectNodes("//trkpt");
            foreach (XmlNode trackPoint in trackPoints)
            {
                var latitude = double.Parse(trackPoint.Attributes["lat"].Value);
                var longitude = double.Parse(trackPoint.Attributes["lon"].Value);
                gpxData.TrackPoints.Add(new TrackPoint(latitude, longitude));
            }

            return gpxData;
        }

        private async Task<GpxData> ParseGPXFile(Stream stream)
        {
            // Parse GPX file
            var gpxData = new GpxData();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(stream);

            // Create a namespace manager to handle the namespaces in the XML document
            var nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("gpx", "http://www.topografix.com/GPX/1/1");

            // Parse track points from GPX file
            XmlNodeList trackPoints = xmlDoc.SelectNodes("//gpx:trkpt", nsMgr);
            foreach (XmlNode trackPoint in trackPoints)
            {
                var latitude = double.Parse(trackPoint.Attributes["lat"].Value);
                var longitude = double.Parse(trackPoint.Attributes["lon"].Value);
                gpxData.TrackPoints.Add(new TrackPoint(latitude, longitude));
            }

            return gpxData;
        }


        private async Task<List<(double, double)>> ParseGPXFileFive(Stream stream)
        {
            var coordinates = new List<(double, double)>();

            // Load the XML document from the stream
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(stream);

            // Create a namespace manager to handle the namespaces in the XML document
            var nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("gpx", "http://www.topografix.com/GPX/1/1");

            // Select all <trkpt> elements in the document
            var trackPoints = xmlDoc.SelectNodes("//gpx:trkpt", nsMgr);

            if (trackPoints != null)
            {
                // Iterate over the <trkpt> elements
                foreach (XmlNode trackPoint in trackPoints)
                {
                    // Extract the latitude and longitude attributes
                    var latitude = double.Parse(trackPoint.Attributes["lat"].Value);
                    var longitude = double.Parse(trackPoint.Attributes["lon"].Value);

                    // Add the coordinates to the list
                    coordinates.Add((latitude, longitude));
                }
            }

            return coordinates;
        }


        private async Task SendLocationDataToClients0(GpxData gpxData)
        {
            // Ensure there are track points in the GPX data
            if (gpxData == null || gpxData.TrackPoints == null || gpxData.TrackPoints.Count == 0)
            {
                Console.WriteLine("No track points found in GPX data.");
                return;
            }

            // Calculate the time interval between each point (assuming uniform time intervals)
            double timeIntervalSeconds = 2;

            // Calculate the number of points to send and the time interval between each point
            int numPoints = gpxData.TrackPoints.Count;
            double totalTime = (numPoints - 1) * timeIntervalSeconds;

            // Iterate through the track points and send them to clients
            for (int i = 0; i < numPoints; i++)
            {
                double timeOffsetSeconds = i * timeIntervalSeconds;
                int nextIndex = i + 1;

                if (nextIndex >= numPoints)
                    break;

                var currentPoint = gpxData.TrackPoints[i];
                var nextPoint = gpxData.TrackPoints[nextIndex];

                double interpolatedLatitude = Interpolate(currentPoint.Latitude, nextPoint.Latitude, timeOffsetSeconds, totalTime);
                double interpolatedLongitude = Interpolate(currentPoint.Longitude, nextPoint.Longitude, timeOffsetSeconds, totalTime);

                Console.WriteLine($"Interpolated Location: Latitude = {interpolatedLatitude}, Longitude = {interpolatedLongitude}");

                await _hubContext.Clients.All.SendAsync("ReceiveLocationData", interpolatedLatitude, interpolatedLongitude);

                await Task.Delay(TimeSpan.FromSeconds(timeIntervalSeconds));
            }
        }

        private async Task SendLocationDataToClients(GpxData gpxData, int totalDurationMinutes)
        {
            // Ensure there are track points in the GPX data
            if (gpxData == null || gpxData.TrackPoints == null || gpxData.TrackPoints.Count == 0)
            {
                Console.WriteLine("No track points found in GPX data.");
                return;
            }

            // Calculate the time interval between each point
            double timeIntervalSeconds = 2;

            // Calculate the total number of points to send
            int numPoints = (int)(totalDurationMinutes * 60 / timeIntervalSeconds);

            // Ensure we have at least two points to interpolate between
            if (numPoints < 2)
            {
                Console.WriteLine("Insufficient duration to interpolate points.");
                return;
            }

            // Calculate the total time spanned by the GPX data
            double totalTime = (gpxData.TrackPoints.Count - 1) * timeIntervalSeconds;

            // Calculate the time interval between each GPX data point
            double gpxTimeInterval = totalTime / (gpxData.TrackPoints.Count - 1);

            // Interpolation factor
            double interpolationFactor = totalTime / (numPoints - 1);

            // Iterate through the desired number of points to send
            for (int i = 0; i < numPoints; i++)
            {
                // Calculate the target time for the current interpolated point
                double targetTime = i * interpolationFactor;

                // Find the appropriate track points to interpolate between
                int startIndex = (int)(targetTime / gpxTimeInterval);
                int endIndex = startIndex + 1;

                if (endIndex >= gpxData.TrackPoints.Count)
                    endIndex = gpxData.TrackPoints.Count - 1;

                var currentPoint = gpxData.TrackPoints[startIndex];
                var nextPoint = gpxData.TrackPoints[endIndex];

                // Calculate the interpolated coordinates
                double currentTime = targetTime - (startIndex * gpxTimeInterval);
                double interpolatedLatitude = Interpolate(currentPoint.Latitude, nextPoint.Latitude, currentTime, gpxTimeInterval);
                double interpolatedLongitude = Interpolate(currentPoint.Longitude, nextPoint.Longitude, currentTime, gpxTimeInterval);

                Console.WriteLine($"Interpolated Location: Latitude = {interpolatedLatitude}, Longitude = {interpolatedLongitude}");

                // Send the interpolated coordinates to clients
                await _hubContext.Clients.All.SendAsync("ReceiveLocationData", interpolatedLatitude, interpolatedLongitude);

                // Delay for the specified time interval
                await Task.Delay(TimeSpan.FromSeconds(timeIntervalSeconds));
            }
        }


        private double Interpolate(double startValue, double endValue, double currentTime, double totalTime)
        {
            return startValue + (endValue - startValue) * (currentTime / totalTime);
        }

        public class GpxData
        {
            public List<TrackPoint> TrackPoints { get; set; } = new List<TrackPoint>();
        }

        public class TrackPoint
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }

            public TrackPoint(double latitude, double longitude)
            {
                Latitude = latitude;
                Longitude = longitude;
            }
        }
    }
}


//namespace maproute_simulation_SignalR_1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class GPXController : ControllerBase
//    {
//        private readonly IHubContext<MapHub> _hubContext;

//        public GPXController(IHubContext<MapHub> hubContext)
//        {
//            _hubContext = hubContext;
//        }

//        [HttpPost("UploadGPXFile")]
//        public async Task<IActionResult> UploadGPXFile([FromForm] IFormFile file)
//        {
//            if (file == null || file.Length == 0)
//                return BadRequest("GPX file is required.");

//            Console.WriteLine("GPX file uploaded.");

//            using (var stream = file.OpenReadStream())
//            {
//                // Parse GPX file
//                var gpxData = await ParseGPXFile(stream);
//                Console.WriteLine("GPX file parsed successfully.");

//                // Send location data to connected clients
//                await SendLocationDataToClients(gpxData);
//                Console.WriteLine("SignalR started sending data to clients.");
//            }

//            return Ok();
//        }

//        [HttpPost("UploadImage")]
//        public ActionResult UploadImage()
//        {
//            IFormFile file = HttpContext.Request.Form.Files[0];
//            //_logger.LogInformation(file.FileName);
//            // we can put rest of upload logic here.
//            return Ok();
//        }

//        private async Task<GpxData> ParseGPXFile(Stream stream)
//        {
//            // Parse GPX file
//            var gpxData = new GpxData();
//            var xmlDoc = new XmlDocument();
//            xmlDoc.Load(stream);

//            // Parse track points from GPX file
//            XmlNodeList trackPoints = xmlDoc.SelectNodes("//trkpt");
//            foreach (XmlNode trackPoint in trackPoints)
//            {
//                var latitude = double.Parse(trackPoint.Attributes["lat"].Value);
//                var longitude = double.Parse(trackPoint.Attributes["lon"].Value);
//                gpxData.TrackPoints.Add(new TrackPoint(latitude, longitude));
//            }

//            return gpxData;
//        }

//        private async Task SendLocationDataToClients(GpxData gpxData)
//        {
//            // Ensure there are track points in the GPX data
//            if (gpxData == null || gpxData.TrackPoints == null || gpxData.TrackPoints.Count == 0)
//            {
//                Console.WriteLine("No track points found in GPX data.");
//                return;
//            }

//            // Calculate the time interval between each point (assuming uniform time intervals)
//            double timeIntervalSeconds = 2;

//            // Calculate the number of points to send and the time interval between each point
//            int numPoints = gpxData.TrackPoints.Count;
//            double totalTime = (numPoints - 1) * timeIntervalSeconds;

//            // Iterate through the track points and send them to clients
//            for (int i = 0; i < numPoints; i++)
//            {
//                double timeOffsetSeconds = i * timeIntervalSeconds;
//                int nextIndex = i + 1;

//                if (nextIndex >= numPoints)
//                    break;

//                var currentPoint = gpxData.TrackPoints[i];
//                var nextPoint = gpxData.TrackPoints[nextIndex];

//                double interpolatedLatitude = Interpolate(currentPoint.Latitude, nextPoint.Latitude, timeOffsetSeconds, totalTime);
//                double interpolatedLongitude = Interpolate(currentPoint.Longitude, nextPoint.Longitude, timeOffsetSeconds, totalTime);

//                Console.WriteLine($"Interpolated Location: Latitude = {interpolatedLatitude}, Longitude = {interpolatedLongitude}");

//                await _hubContext.Clients.All.SendAsync("ReceiveLocationData", interpolatedLatitude, interpolatedLongitude);

//                await Task.Delay(TimeSpan.FromSeconds(timeIntervalSeconds));
//            }
//        }

//        private double Interpolate(double startValue, double endValue, double currentTime, double totalTime)
//        {
//            return startValue + (endValue - startValue) * (currentTime / totalTime);
//        }

//        public class GpxData
//        {
//            public List<TrackPoint> TrackPoints { get; set; } = new List<TrackPoint>();
//        }

//        public class TrackPoint
//        {
//            public double Latitude { get; set; }
//            public double Longitude { get; set; }

//            public TrackPoint(double latitude, double longitude)
//            {
//                Latitude = latitude;
//                Longitude = longitude;
//            }
//        }
//    }
//}


//namespace maproute_simulation_SignalR_1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class GPXController : ControllerBase
//    {
//        private readonly IHubContext<MapHub> _hubContext;

//        public GPXController(IHubContext<MapHub> hubContext)
//        {
//            _hubContext = hubContext;
//        }

//        //[HttpPost]
//        //public async Task<IActionResult> UploadGPXFile([FromForm] Microsoft.AspNetCore.Http.IFormFile file)
//        //{
//        //    if (file == null || file.Length == 0)
//        //        return BadRequest("GPX file is required.");

//        //    Console.WriteLine("GPX file uploaded.");

//        //    using (var stream = file.OpenReadStream())
//        //    {
//        //        // Parse GPX file
//        //        var gpxData = await ParseGPXFile(stream);
//        //        Console.WriteLine("GPX file parsed successfully.");

//        //        // Send location data to connected clients
//        //        await SendLocationDataToClients(gpxData);
//        //        Console.WriteLine("SignalR started sending data to clients.");
//        //    }

//        //    return Ok();
//        //}

//        [HttpPost]
//        public async Task<IActionResult> UploadGPXFile([FromForm] IFormFile file)
//        {
//            if (file == null || file.Length == 0)
//                return BadRequest("GPX file is required.");

//            using (var stream = file.OpenReadStream())
//            {
//                // Parse GPX file
//                var gpxData = await ParseGPXFile(stream);

//                // Send location data to connected clients
//                await SendLocationDataToClients(gpxData);
//            }

//            return Ok();
//        }


//        private async Task<GpxData> ParseGPXFile(Stream stream)
//        {
//            // Parse GPX file
//            var gpxData = new GpxData();
//            var xmlDoc = new XmlDocument();
//            xmlDoc.Load(stream);

//            // Parse track points from GPX file
//            XmlNodeList trackPoints = xmlDoc.SelectNodes("//trkpt");
//            foreach (XmlNode trackPoint in trackPoints)
//            {
//                var latitude = double.Parse(trackPoint.Attributes["lat"].Value);
//                var longitude = double.Parse(trackPoint.Attributes["lon"].Value);
//                gpxData.TrackPoints.Add(new TrackPoint(latitude, longitude));
//            }

//            return gpxData;
//        }

//        private async Task SendLocationDataToClients(GpxData gpxData)
//        {
//            // Ensure there are track points in the GPX data
//            if (gpxData == null || gpxData.TrackPoints == null || gpxData.TrackPoints.Count == 0)
//            {
//                Console.WriteLine("No track points found in GPX data.");
//                return;
//            }

//            // Calculate the time interval between each point (assuming uniform time intervals)
//            double timeIntervalSeconds = 2;

//            // Calculate the number of points to send and the time interval between each point
//            int numPoints = gpxData.TrackPoints.Count;
//            double totalTime = (numPoints - 1) * timeIntervalSeconds;

//            // Iterate through the track points and send them to clients
//            for (int i = 0; i < numPoints; i++)
//            {
//                double timeOffsetSeconds = i * timeIntervalSeconds;
//                int nextIndex = i + 1;

//                if (nextIndex >= numPoints)
//                    break;

//                var currentPoint = gpxData.TrackPoints[i];
//                var nextPoint = gpxData.TrackPoints[nextIndex];

//                double interpolatedLatitude = Interpolate(currentPoint.Latitude, nextPoint.Latitude, timeOffsetSeconds, totalTime);
//                double interpolatedLongitude = Interpolate(currentPoint.Longitude, nextPoint.Longitude, timeOffsetSeconds, totalTime);

//                Console.WriteLine($"Interpolated Location: Latitude = {interpolatedLatitude}, Longitude = {interpolatedLongitude}");

//                await _hubContext.Clients.All.SendAsync("ReceiveLocationData", interpolatedLatitude, interpolatedLongitude);

//                await Task.Delay(TimeSpan.FromSeconds(timeIntervalSeconds));
//            }
//        }

//        private double Interpolate(double startValue, double endValue, double currentTime, double totalTime)
//        {
//            return startValue + (endValue - startValue) * (currentTime / totalTime);
//        }
//    }

//    public class GpxData
//    {
//        public List<TrackPoint> TrackPoints { get; set; } = new List<TrackPoint>();
//    }

//    public class TrackPoint
//    {
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }

//        public TrackPoint(double latitude, double longitude)
//        {
//            Latitude = latitude;
//            Longitude = longitude;
//        }
//    }
//}



//namespace maproute_simulation_SignalR_1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class GPXController : ControllerBase
//    {
//        private readonly IHubContext<MapHub> _hubContext;

//        public GPXController(IHubContext<MapHub> hubContext)
//        {
//            _hubContext = hubContext;
//        }

//        [HttpPost]
//        public async Task<IActionResult> UploadGPXFile([FromForm] Microsoft.AspNetCore.Http.IFormFile file)
//        {
//            if (file == null || file.Length == 0)
//                return BadRequest("GPX file is required.");

//            using (var stream = file.OpenReadStream())
//            {
//                // Parse GPX file
//                var gpxData = await ParseGPXFile(stream);

//                // Send location data to connected clients
//                await SendLocationDataToClients(gpxData);
//            }

//            return Ok();
//        }

//        private async Task<GpxData> ParseGPXFile(Stream stream)
//        {
//            // Parse GPX file
//            var gpxData = new GpxData();
//            var xmlDoc = new XmlDocument();
//            xmlDoc.Load(stream);

//            // Parse track points from GPX file
//            XmlNodeList trackPoints = xmlDoc.SelectNodes("//trkpt");
//            foreach (XmlNode trackPoint in trackPoints)
//            {
//                var latitude = double.Parse(trackPoint.Attributes["lat"].Value);
//                var longitude = double.Parse(trackPoint.Attributes["lon"].Value);
//                gpxData.TrackPoints.Add(new TrackPoint(latitude, longitude));
//            }

//            return gpxData;
//        }

//        //private async Task SendLocationDataToClients(GpxData gpxData)
//        //{
//        //    // Calculate points based on time duration
//        //    // (Implementation omitted for brevity)

//        //    // Send location data to connected clients via SignalR
//        //    foreach (var point in calculatedPoints)
//        //    {
//        //        await _hubContext.Clients.All.SendAsync("ReceiveLocationData", point.Latitude, point.Longitude);
//        //        await Task.Delay(2000); // Adjust delay based on your requirements
//        //    }
//        //}

//        private async Task SendLocationDataToClients(GpxData gpxData)
//        {
//            // Ensure there are track points in the GPX data
//            if (gpxData == null || gpxData.TrackPoints == null || gpxData.TrackPoints.Count == 0)
//            {
//                // Log or handle the case where there are no track points
//                return;
//            }

//            // Calculate the time interval between each point (assuming uniform time intervals)
//            // Example: If you want to send points every 2 seconds
//            double timeIntervalSeconds = 2;

//            // Calculate the number of points to send and the time interval between each point
//            int numPoints = gpxData.TrackPoints.Count;
//            double totalTime = (numPoints - 1) * timeIntervalSeconds;

//            // Iterate through the track points and send them to clients
//            for (int i = 0; i < numPoints; i++)
//            {
//                // Calculate the time offset for each point
//                double timeOffsetSeconds = i * timeIntervalSeconds;

//                // Calculate the index of the next point
//                int nextIndex = i + 1;

//                // Ensure the next index is within bounds
//                if (nextIndex >= numPoints)
//                {
//                    // Log or handle the case where the next index is out of bounds
//                    break;
//                }

//                // Get the current and next track points
//                var currentPoint = gpxData.TrackPoints[i];
//                var nextPoint = gpxData.TrackPoints[nextIndex];

//                // Interpolate the latitude and longitude based on the time offset
//                double interpolatedLatitude = Interpolate(currentPoint.Latitude, nextPoint.Latitude, timeOffsetSeconds, totalTime);
//                double interpolatedLongitude = Interpolate(currentPoint.Longitude, nextPoint.Longitude, timeOffsetSeconds, totalTime);

//                Console.WriteLine($"Interpolated Location: Latitude = {interpolatedLatitude}, Longitude = {interpolatedLongitude}");

//                // Send the interpolated location to clients via SignalR
//                await _hubContext.Clients.All.SendAsync("ReceiveLocationData", interpolatedLatitude, interpolatedLongitude);

//                // Delay before sending the next point
//                await Task.Delay(TimeSpan.FromSeconds(timeIntervalSeconds));
//            }
//        }

//        // Helper method to interpolate between two values based on time
//        private double Interpolate(double startValue, double endValue, double currentTime, double totalTime)
//        {
//            // Perform linear interpolation
//            return startValue + (endValue - startValue) * (currentTime / totalTime);
//        }



//    }

//    public class GpxData
//    {
//        public List<TrackPoint> TrackPoints { get; set; } = new List<TrackPoint>();
//    }

//    public class TrackPoint
//    {
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }

//        public TrackPoint(double latitude, double longitude)
//        {
//            Latitude = latitude;
//            Longitude = longitude;
//        }
//    }
//    //public class MapRouteSimulationCon : Controller
//    //{
//    //    public IActionResult Index()
//    //    {
//    //        return View();
//    //    }
//    //}
//}
